using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DriveEase.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        logger.LogError(exception,
            "Unhandled exception. TraceId: {TraceId} Path: {Path}",
            traceId, httpContext.Request.Path);

        var (statusCode, title) = exception switch
        {
            ValidationException       => (400, "Validation Failed"),
            InvalidOperationException => (400, "Invalid Operation"),
            KeyNotFoundException      => (404, "Not Found"),
            UnauthorizedAccessException => (403, "Forbidden"),
            _                         => (500, "Internal Server Error")
        };

        var detail = exception is ValidationException ve
            ? string.Join("; ", ve.Errors.Select(e => e.ErrorMessage))
            : exception.Message;

        var problem = new ProblemDetails
        {
            Type     = $"https://driveease.io/errors/{statusCode}",
            Title    = title,
            Status   = statusCode,
            Detail   = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
