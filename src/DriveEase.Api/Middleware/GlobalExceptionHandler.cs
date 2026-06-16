using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DriveEase.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    // TODO: Handle exceptions globally and return a clean ProblemDetails JSON response
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken) => throw new NotImplementedException();
}
