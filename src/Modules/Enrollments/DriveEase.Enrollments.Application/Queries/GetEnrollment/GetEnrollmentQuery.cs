using DriveEase.Enrollments.Application.DTOs;
using MediatR;

namespace DriveEase.Enrollments.Application.Queries.GetEnrollment;

public sealed record GetEnrollmentQuery(Guid EnrollmentId) : IRequest<EnrollmentDto?>;
