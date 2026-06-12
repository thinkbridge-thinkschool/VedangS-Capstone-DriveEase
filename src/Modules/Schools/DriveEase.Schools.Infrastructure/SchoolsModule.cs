using DriveEase.Schools.Domain.Repositories;
using DriveEase.Schools.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DriveEase.Schools.Infrastructure;

public static class SchoolsModule
{
    public static IServiceCollection AddSchoolsModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SchoolsDbContext>(opt =>
            opt.UseSqlite(connectionString));

        services.AddScoped<IDrivingSchoolRepository, SchoolRepository>();
        services.AddScoped<IInstructorRepository, InstructorRepository>();

        return services;
    }
}
