# ADR-001: Transactional Outbox Pattern for Domain Event Publishing

**Status:** Accepted

---

## Context

DriveEase's core enrollment flow requires that when a student pays and an Enrollment moves from Pending → Active, a downstream notification must be sent — email to the student, alert to the instructor, confirmation to the school admin. This is a two-step write: update the database, then publish an event to Azure Service Bus.

If the DB write succeeds but the Service Bus publish fails (network blip, Service Bus throttling, process crash between the two steps), the enrollment is active in SQL but no notification goes out. The student thinks they enrolled — the instructor never knows. This is the classic dual-write problem: two systems must stay consistent with no distributed transaction available.

Relevant code path: EnrollStudentHandler → ProcessPaymentCommand → Enrollment.ConfirmPayment() → raises EnrollmentConfirmedEvent. That event must reliably reach the Notifications module.

---

## Decision

Implement the Transactional Outbox Pattern using an EF Core SaveChangesInterceptor.

**Mechanism:**

1. OutboxInterceptor.SavingChangesAsync() scans the EF ChangeTracker for all aggregates implementing IHasDomainEvents
2. Every IIntegrationEvent is serialized into an OutboxMessage row in the same DbContext (same schema, same transaction)
3. The aggregate write + outbox insert commit atomically — if either fails, both roll back
4. OutboxRelayWorker (a BackgroundService) polls every 10 seconds, reads up to 50 unprocessed rows, publishes each to IEventBus (Azure Service Bus in prod, InMemoryEventBus in dev), then marks ProcessedAt = UtcNow

**Files:** OutboxMessage.cs, OutboxInterceptor.cs, OutboxRelayWorker.cs, registered in EnrollmentsDbContext and LessonsDbContext.

---

## Alternatives Considered

**Option A — Direct Service Bus publish inside the handler**
Call eventBus.PublishAsync(event) immediately after dbContext.SaveChangesAsync() in the handler.
- ❌ Dual-write problem: process crash between SaveChanges and PublishAsync loses the event forever. Impossible to guarantee at-least-once delivery.

**Option B — MediatR Notifications (in-process publish)**
Use INotificationHandler\<T\> inside MediatR to dispatch domain events in-process.
- ❌ Events are lost if the handler throws. No durability across process restarts. Cannot integrate with external Service Bus topics.

**Option C — Azure Service Bus Transactions (AMQP sessions)**
Use Service Bus built-in transactions to atomically send and acknowledge within one AMQP session.
- ❌ Does not span Service Bus + SQL Server. Still leaves the DB write and message publish in separate systems. Premium tier required. Doesn't solve the two-phase problem.

---

## Pros

- Guaranteed at-least-once delivery — event cannot be lost if the DB transaction succeeds
- Process crashes are safe — OutboxRelayWorker picks up unprocessed messages on restart
- Works with any event bus via the IEventBus interface (Service Bus in prod, InMemory in dev)
- Full observability — each relay has its own OpenTelemetry span and Serilog log line
- No distributed transaction coordinator (no DTC dependency)
- OutboxInterceptor is transparent — handlers do not change, domain models stay clean

---

## Cons

- At-least-once (not exactly-once) — handlers must be idempotent
- 10-second polling delay — events are not immediate
- Reflection-based dispatch in OutboxRelayWorker (MakeGenericMethod) — fragile if event type names change
- Two outbox tables (enrollments.OutboxMessages, lessons.OutboxMessages) — schema maintenance per module
- No dead-letter handling — on first publish failure, ProcessedAt is stamped and the message is permanently lost. No retry, no alert, no recovery.

---

## Why This Decision Was Chosen

The direct publish alternatives create an unrecoverable split-brain scenario: the student pays, the enrollment goes active in SQL, but the instructor never gets notified because the publish step failed. In a driving school context, a student showing up without an assigned instructor is a real operational failure — not a tolerable edge case.

The Outbox pattern is the industry-standard solution to this specific problem. It moves the publish step into the same transaction as the domain write, eliminating the window where data and events diverge. The 10-second polling delay is acceptable for this domain — lesson reminders and enrollment confirmations are not real-time requirements.

---

## Future Consequences

- All notification handlers must implement idempotency checks before any state mutation or external call
- If polling latency becomes unacceptable, replace the timer with SQL Server change data capture
- The Error field in OutboxMessage needs a proper dead-letter queue and retry policy before Day 32
- If modules split into separate services in the future, the outbox can be extended per-service with zero architectural change — the pattern is microservice-compatible
