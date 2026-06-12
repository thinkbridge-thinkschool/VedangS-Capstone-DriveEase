using MediatR;

namespace DriveEase.Students.Application.Queries.GetStudent;

public sealed record StudentDto(Guid Id, string FullName, string Email, string PhoneNumber, DateOnly DateOfBirth);

public sealed record GetStudentQuery(Guid StudentId) : IRequest<StudentDto?>;
