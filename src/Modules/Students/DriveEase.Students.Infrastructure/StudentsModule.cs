using DriveEase.Students.Domain.Repositories;
using DriveEase.Students.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DriveEase.Students.Infrastructure;

public static class StudentsModule
{
    public static IServiceCollection AddStudentsModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<StudentsDbContext>(opt =>
            opt.UseSqlite(connectionString));

        services.AddScoped<IStudentRepository, StudentRepository>();

        return services;
    }
}
