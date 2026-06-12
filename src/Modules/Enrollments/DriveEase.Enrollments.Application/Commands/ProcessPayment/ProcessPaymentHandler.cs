using DriveEase.Enrollments.Domain.Repositories;
using DriveEase.Enrollments.Application.Services;
using DriveEase.Shared.Messaging;
using DriveEase.Enrollments.Domain.Events;
using MediatR;

namespace DriveEase.Enrollments.Application.Commands.ProcessPayment;

public sealed class ProcessPaymentHandler(
    IEnrollmentRepository repository,
    IPaymentGateway paymentGateway,
    IEventBus eventBus) : IRequestHandler<ProcessPaymentCommand, bool>
{
    public async Task<bool> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var enrollment = await repository.GetByIdAsync(request.EnrollmentId, cancellationToken)
            ?? throw new InvalidOperationException($"Enrollment {request.EnrollmentId} not found.");

        var success = await paymentGateway.ChargeAsync(enrollment.StudentId, enrollment.Fee, cancellationToken);

        if (success)
        {
            enrollment.ConfirmPayment();
            await repository.UpdateAsync(enrollment, cancellationToken);

            var confirmedEvent = (EnrollmentConfirmedEvent)enrollment.DomainEvents
                .First(e => e is EnrollmentConfirmedEvent);
            await eventBus.PublishAsync(confirmedEvent, cancellationToken);
        }
        else
        {
            enrollment.FailPayment("Payment gateway declined the charge.");
            await repository.UpdateAsync(enrollment, cancellationToken);

            var failedEvent = (PaymentFailedEvent)enrollment.DomainEvents
                .First(e => e is PaymentFailedEvent);
            await eventBus.PublishAsync(failedEvent, cancellationToken);
        }

        enrollment.ClearDomainEvents();
        return success;
    }
}
