namespace DriveEase.Api.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next)
{
    // TODO: Catch unhandled exceptions from the pipeline
    // TODO: Return a consistent JSON error response to the frontend
    public Task InvokeAsync(HttpContext context) => throw new NotImplementedException();
}
