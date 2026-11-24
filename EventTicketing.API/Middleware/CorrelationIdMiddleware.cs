namespace EventTicketing.API.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.TraceIdentifier;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-ID"] =
                correlationId;

            return Task.CompletedTask;
        });

        using var scope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            });

        _logger.LogInformation(
            "Incoming request: {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        await _next(context);
    }
}