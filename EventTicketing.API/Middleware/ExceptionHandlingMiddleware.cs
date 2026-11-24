using EventTicketing.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventTicketing.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(
                    "An exception occurred after the response started. ExceptionType: {ExceptionType}",
                    exception.GetType().Name);

                throw;
            }

            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        int statusCode;
        string title;
        string detail;

        switch (exception)
        {
            case ValidationException:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Validation Error";
                detail = exception.Message;
                break;

            case DbUpdateConcurrencyException:
                statusCode = StatusCodes.Status409Conflict;
                title = "Concurrency Conflict";
                detail = "The requested data was modified by another operation. Please try again.";
                break;

            case Exception ex when IsTransactionConflict(ex):
                statusCode = StatusCodes.Status409Conflict;
                title = "Transaction Conflict";
                detail = "The requested operation conflicts with another operation. Please try again.";
                break;

            case ConflictException:
                statusCode = StatusCodes.Status409Conflict;
                title = "Conflict";
                detail = exception.Message;
                break;

            case NotFoundException:
            case KeyNotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                title = "Not Found";
                detail = exception.Message;
                break;

            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status401Unauthorized;
                title = "Unauthorized";
                detail = exception.Message;
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                title = "Internal Server Error";
                detail = "An unexpected error occurred.";
                break;
        }

        if (statusCode == StatusCodes.Status409Conflict)
        {
            _logger.LogWarning(
                "Resource conflict. StatusCode: {StatusCode}, Path: {Path}, ExceptionType: {ExceptionType}, CorrelationId: {CorrelationId}",
                statusCode,
                context.Request.Path,
                exception.GetType().Name,
                context.TraceIdentifier);
        }
        else
        {
            _logger.LogError(
                "Request exception. StatusCode: {StatusCode}, Path: {Path}, ExceptionType: {ExceptionType}, CorrelationId: {CorrelationId}",
                statusCode,
                context.Request.Path,
                exception.GetType().Name,
                context.TraceIdentifier);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: context.RequestAborted);
    }

    private static bool IsTransactionConflict(
        Exception exception)
    {
        for (Exception? current = exception;
             current is not null;
             current = current.InnerException)
        {
            if (current is PostgresException postgresException &&
                postgresException.SqlState is
                    PostgresErrorCodes.SerializationFailure or
                    PostgresErrorCodes.DeadlockDetected)
            {
                return true;
            }
        }

        return false;
    }
}