using DriveEase.Lessons.Domain.Repositories;
using DriveEase.Lessons.Infrastructure.Persistence;
using DriveEase.Lessons.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DriveEase.Lessons.Infrastructure;

public static class LessonsModule
{
    public static IServiceCollection AddLessonsModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<LessonsDbContext>(opt =>
            opt.UseSqlite(connectionString));

        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IUpcomingLessonsQuery, UpcomingLessonsQuery>();

        services.AddHostedService<LessonReminderWorker>();

        return services;
    }
}
