# Day 28 — Design Review + ADR

**Author:** Vedang Shindee
**Date:** 22 June 2026
**Program:** ThinkBridge — Day 28

---

## ADR-001: Transactional Outbox Pattern for Domain Event Publishing

**Status:** Accepted

### Context

DriveEase's core enrollment flow requires that when a student pays and an Enrollment moves from Pending → Active, a downstream notification must be sent — email to the student, alert to the instructor, confirmation to the school admin. This is a two-step write: update the database, then publish an event to Azure Service Bus.

If the DB write succeeds but the Service Bus publish fails (network blip, Service Bus throttling, process crash between the two steps), the enrollment is active in SQL but no notification goes out. The student thinks they enrolled — the instructor never knows. This is the classic dual-write problem: two systems must stay consistent with no distributed transaction available.

Relevant code path: EnrollStudentHandler → ProcessPaymentCommand → Enrollment.ConfirmPayment() → raises EnrollmentConfirmedEvent. That event must reliably reach the Notifications module.

### Decision

Implement the Transactional Outbox Pattern using an EF Core SaveChangesInterceptor.

**Mechanism:**

1. OutboxInterceptor.SavingChangesAsync() scans the EF ChangeTracker for all aggregates implementing IHasDomainEvents
2. Every IIntegrationEvent is serialized into an OutboxMessage row in the same DbContext (same schema, same transaction)
3. The aggregate write + outbox insert commit atomically — if either fails, both roll back
4. OutboxRelayWorker (a BackgroundService) polls every 10 seconds, reads up to 50 unprocessed rows, publishes each to IEventBus (Azure Service Bus in prod, InMemoryEventBus in dev), then marks ProcessedAt = UtcNow

**Files:** OutboxMessage.cs, OutboxInterceptor.cs, OutboxRelayWorker.cs, registered in EnrollmentsDbContext and LessonsDbContext.

### Alternatives Considered

**Option A — Direct Service Bus publish inside the handler**
Call eventBus.PublishAsync(event) immediately after dbContext.SaveChangesAsync() in the handler.
- ❌ Dual-write problem: process crash between SaveChanges and PublishAsync loses the event forever. Impossible to guarantee at-least-once delivery.

**Option B — MediatR Notifications (in-process publish)**
Use INotificationHandler\<T\> inside MediatR to dispatch domain events in-process.
- ❌ Events are lost if the handler throws. No durability across process restarts. Cannot integrate with external Service Bus topics.

**Option C — Azure Service Bus Transactions (AMQP sessions)**
Use Service Bus built-in transactions to atomically send and acknowledge within one AMQP session.
- ❌ Does not span Service Bus + SQL Server. Still leaves the DB write and message publish in separate systems. Premium tier required. Doesn't solve the two-phase problem.

### Pros

- Guaranteed at-least-once delivery — event cannot be lost if the DB transaction succeeds
- Process crashes are safe — OutboxRelayWorker picks up unprocessed messages on restart
- Works with any event bus via the IEventBus interface (Service Bus in prod, InMemory in dev)
- Full observability — each relay has its own OpenTelemetry span and Serilog log line
- No distributed transaction coordinator (no DTC dependency)
- OutboxInterceptor is transparent — handlers do not change, domain models stay clean

### Cons

- At-least-once (not exactly-once) — handlers must be idempotent
- 10-second polling delay — events are not immediate
- Reflection-based dispatch in OutboxRelayWorker (MakeGenericMethod) — fragile if event type names change
- Two outbox tables (enrollments.OutboxMessages, lessons.OutboxMessages) — schema maintenance per module
- No dead-letter handling — on first publish failure, ProcessedAt is stamped and the message is permanently lost. No retry, no alert, no recovery.

### Why This Decision Was Chosen

The direct publish alternatives create an unrecoverable split-brain scenario: the student pays, the enrollment goes active in SQL, but the instructor never gets notified because the publish step failed. In a driving school context, a student showing up without an assigned instructor is a real operational failure — not a tolerable edge case.

The Outbox pattern is the industry-standard solution to this specific problem. It moves the publish step into the same transaction as the domain write, eliminating the window where data and events diverge. The 10-second polling delay is acceptable for this domain — lesson reminders and enrollment confirmations are not real-time requirements.

### Future Consequences

- All notification handlers must implement idempotency checks before any state mutation or external call
- If polling latency becomes unacceptable, replace the timer with SQL Server change data capture
- The Error field in OutboxMessage needs a proper dead-letter queue and retry policy before Day 32
- If modules split into separate services in the future, the outbox can be extended per-service with zero architectural change — the pattern is microservice-compatible

---

## Top Critique + How It Changed the Design

### The Critique

> The Outbox relay has no dead-letter handling. Failed events are silently dropped after a single attempt with no retry, no alert, and no operational runbook. A transient Service Bus error permanently loses the event — the student's payment goes through in SQL but the instructor notification never arrives and is never tried again. `OutboxRelayWorker` catches exceptions, stamps `ProcessedAt = UtcNow`, and sets `Error = ex.Message` — then moves on. The message is marked as processed even though it was never delivered. There is no way to distinguish "delivered" from "failed" by looking at `ProcessedAt` alone, and no human is ever alerted.

### Original Design — Problematic

```csharp
// OutboxRelayWorker.cs — ExecuteUpdateAsync runs OUTSIDE the try/catch
// so ProcessedAt is stamped regardless of success or failure
catch (Exception ex)
{
    error = ex.Message;
    logger.LogError(ex, "Failed to relay outbox message {Id}", message.Id);
}

// Always runs — success or failure:
await context.Set<OutboxMessage>()
    .Where(m => m.Id == message.Id)
    .ExecuteUpdateAsync(s => s
        .SetProperty(m => m.ProcessedAt, DateTime.UtcNow) // stamped even on failure
        .SetProperty(m => m.Error, error),
        cancellationToken);
```

### Why It Was Problematic

The outbox becomes a silent data loss machine. A transient Service Bus throttle causes the event to be permanently dropped on first failure — `ProcessedAt` is set so the relay never touches it again. Operations has no way to distinguish a successfully delivered message from a silently failed one. There is no retry window, no counter, no alert. The system appears healthy (enrollment was saved, `ProcessedAt` is non-null) but the instructor notification was lost forever on the first failure.

### Recommended Change

Add `RetryCount` (int) and `DeadLettered` (bool) to `OutboxMessage`. On failure, increment `RetryCount` but leave `ProcessedAt = null` so the relay picks it up again on the next poll. After 5 retries, set `DeadLettered = true` and stamp `ProcessedAt = UtcNow` so the relay stops. Log an error with full context when dead-lettering — this fires an App Insights alert.

```csharp
outboxMsg.RetryCount++;
if (outboxMsg.RetryCount >= 5)
{
    outboxMsg.DeadLettered = true;
    outboxMsg.ProcessedAt = _clock.UtcNow;
    _logger.LogError(
        "Outbox message dead-lettered after {RetryCount} attempts. EventType: {EventType}, Id: {Id}",
        outboxMsg.RetryCount, outboxMsg.EventType, outboxMsg.Id);
}
// else: leave ProcessedAt = null so the next poll retries it
```

### How It Changed the Design

The outbox goes from a silent data loss machine to an **observable reliability layer**. Operations can query `WHERE DeadLettered = 1` to see stranded events. App Insights alerts fire when messages dead-letter. The retry window means a transient Service Bus blip no longer permanently loses the event. The system now has a clear contract: events are delivered at-least-once within a bounded time window, or they are surfaced to operations for manual replay.

This will be implemented on **Day 30** when adding the Dapper hot-read path alongside outbox reliability improvements.

---

## Day-by-Day Build Plan

### Day 29 — Foundation + Happy Path End-to-End

**Goal:** Student can register → login → see schools → enroll on the live Angular frontend against real Dev infrastructure.

**Key Tasks:**
- ✅ Refresh tokens implemented — `students.RefreshTokens` table, `/auth/refresh`, `/auth/logout`, rotation + reuse detection
- Commit untracked `IClock.cs` to the repository
- Seed Dev SQL with 2–3 driving schools via Swagger
- Confirm `EnrollmentConfirmedEvent` reaches outbox and Service Bus end-to-end
- Verify App Insights receives traces for the enroll request

**Files Created/Modified (Done on Day 28):**
- `src/Modules/Students/DriveEase.Students.Infrastructure/Persistence/RefreshToken.cs` ✅
- `src/Modules/Students/DriveEase.Students.Infrastructure/Persistence/StudentsDbContext.cs` ✅ — added `DbSet<RefreshToken>`
- `src/DriveEase.Api/Auth/RefreshTokenService.cs` ✅
- `src/DriveEase.Api/Controllers/AuthController.cs` ✅ — login returns both tokens, `/refresh`, `/logout` added
- `src/DriveEase.Api/Program.cs` ✅ — `RefreshTokenService` registered, `students.RefreshTokens` table in schema init

**Refresh Token Design:**
- Raw token: 64 random bytes, base64-encoded — never stored
- Stored token: SHA-256 hash of the raw token
- Lifetime: 7 days, single-use, rotated on every `/auth/refresh` call
- `Family` field groups all rotated tokens in a chain — if a revoked token is replayed, the entire family is revoked and the student must log in again (reuse detection)
- Login response: `{ accessToken, refreshToken, studentId, fullName, email }`
- Refresh response: `{ accessToken, refreshToken }`

**Expected Outcome:** Happy path confirmed end-to-end on Dev. JWT + refresh token returned on login. Outbox relay confirms event published to Service Bus in logs.

---

### Day 30 — Feature Completeness + PR Review

**Goal:** All 6 Angular pages working. Instructor assignment, lesson booking, and My Lessons page complete. PR open for review.

**Key Tasks:**
- Add Dapper to `SchoolRepository` for `GetAllSchools` hot read path
- Add outbox dead-letter retry logic (`RetryCount`, `DeadLettered` fields, backoff)
- Wire `FakeNotificationSender` to log structured notification events
- Test full flow: enroll → assign instructor → book lesson → complete lesson
- Verify `LessonReminderWorker` fires on Dev
- Open PR with clean commit history, respond to review comments

**Files to Create/Modify:**
- `src/Modules/Schools/DriveEase.Schools.Infrastructure/Persistence/SchoolRepository.cs` — add Dapper query
- `src/DriveEase.Shared/Outbox/OutboxMessage.cs` — add `RetryCount`, `DeadLettered` columns
- `src/DriveEase.Api/Workers/OutboxRelayWorker.cs` — add retry backoff logic
- `frontend/src/app/features/lessons/` — wire Book Lesson + My Lessons to API

**Expected Outcome:** Complete user journey works on live Dev URL. Dapper query visible in App Insights dependency traces.

---

### Day 31 — Tests, Performance, Security, Green CI

**Goal:** Tests at every layer, CI green with coverage gate, p99 of enroll endpoint documented, security re-check.

**Key Tasks:**
- Create `tests/DriveEase.Api.Tests.Integration/` project with WebApplicationFactory
- Add Testcontainers for real SQL Server in integration tests
- Write 10+ integration tests: happy path enroll, 401 without token, 403 wrong role, validation errors
- Run k6 against Dev: capture p50/p99 for `POST /api/v1/enrollments`
- Verify CI runs unit + integration tests with green gate

**Files to Create:**
- `tests/DriveEase.Api.Tests.Integration/DriveEaseApiFactory.cs`
- `tests/DriveEase.Api.Tests.Integration/EnrollmentEndpointTests.cs`
- `tests/DriveEase.Api.Tests.Integration/AuthEndpointTests.cs`

**Expected Outcome:** CI runs unit + integration tests, both green. Coverage report in GitHub Actions artifacts. p99 documented with App Insights KQL query.

---

### Day 32 — Ship + Demo + Postmortem

**Goal:** Prod redeployed, live demo runs without error, postmortem written.

**Key Tasks:**
- Redeploy prod from Bicep:
  ```powershell
  $env:SQL_ADMIN_PASSWORD = "Prod@Str0ng#2025!"
  .\infra\stacks\deploy-prod.ps1
  azd deploy --environment prod
  ```
- Seed prod with 1–2 driving schools for the demo
- Record full demo: register → enroll → assign instructor → book lesson → complete lesson
- Run Lighthouse on Angular frontend (target ≥ 90)
- Write 1-page postmortem: hardest bug, what I'd do differently, proudest decision

**Expected Outcome:** Live prod URL accessible. Demo submitted. Postmortem covers hardest bug, what I'd change, and one thing I'm proudest of.
