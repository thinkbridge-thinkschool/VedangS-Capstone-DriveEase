using DriveEase.Enrollments.Domain.Events;
using DriveEase.Enrollments.Domain.ValueObjects;
using DriveEase.Shared.Domain;

namespace DriveEase.Enrollments.Domain.Aggregates;

public sealed class Enrollment : AggregateRoot<Guid>
{
    public Guid StudentId { get; private set; }
    public Guid DrivingSchoolId { get; private set; }
    public Guid? InstructorId { get; private set; }
    public decimal Fee { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public DateTime EnrolledAt { get; private set; }
    public DateTime? PaymentConfirmedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private Enrollment() { }

    public static Enrollment Create(Guid studentId, Guid schoolId, decimal fee)
    {
        if (fee <= 0)
            throw new InvalidOperationException("Enrollment fee must be greater than zero.");

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            DrivingSchoolId = schoolId,
            Fee = fee,
            PaymentStatus = PaymentStatus.Pending,
            Status = EnrollmentStatus.Pending,
            EnrolledAt = DateTime.UtcNow
        };

        return enrollment;
    }

    public void ConfirmPayment()
    {
        if (PaymentStatus != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm payment in state '{PaymentStatus}'.");

        if (Status != EnrollmentStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm payment for enrollment in state '{Status}'.");

        PaymentStatus = PaymentStatus.Paid;
        Status = EnrollmentStatus.Active;
        PaymentConfirmedAt = DateTime.UtcNow;

        RaiseDomainEvent(EnrollmentConfirmedEvent.Create(Id, StudentId, DrivingSchoolId, Fee));
    }

    public void FailPayment(string reason)
    {
        if (PaymentStatus != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot fail payment in state '{PaymentStatus}'.");

        PaymentStatus = PaymentStatus.Failed;

        RaiseDomainEvent(PaymentFailedEvent.Create(Id, StudentId, reason));
    }

    public void AssignInstructor(Guid instructorId)
    {
        if (Status != EnrollmentStatus.Active)
            throw new InvalidOperationException("Instructor can only be assigned to an active enrollment.");

        InstructorId = instructorId;

        RaiseDomainEvent(InstructorAssignedEvent.Create(Id, StudentId, instructorId));
    }

    public void Complete()
    {
        if (Status != EnrollmentStatus.Active)
            throw new InvalidOperationException("Only active enrollments can be completed.");

        Status = EnrollmentStatus.Completed;
    }

    public void Cancel(string reason)
    {
        if (Status is EnrollmentStatus.Completed or EnrollmentStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel an enrollment in state '{Status}'.");

        Status = EnrollmentStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;

        RaiseDomainEvent(EnrollmentCancelledEvent.Create(Id, StudentId, reason));
    }

    public bool CanBookLesson() => Status == EnrollmentStatus.Active && PaymentStatus == PaymentStatus.Paid;
}
