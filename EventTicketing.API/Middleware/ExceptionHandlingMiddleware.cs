
using EventTicketing.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                    exception,
                    "An exception occurred after the response started.");

                throw;
            }

            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var statusCode = exception switch
        {
            EventTicketing.Core.Exceptions.ValidationException =>
                StatusCodes.Status400BadRequest,

            DbUpdateConcurrencyException =>
                StatusCodes.Status409Conflict,

            ConflictException =>
                StatusCodes.Status409Conflict,

            NotFoundException =>
                StatusCodes.Status404NotFound,

            KeyNotFoundException =>
                StatusCodes.Status404NotFound,

            UnauthorizedAccessException =>
                StatusCodes.Status401Unauthorized,

            _ => StatusCodes.Status500InternalServerError
        };

        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. Path: {Path}",
                context.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                "Request failed. StatusCode: {StatusCode}, Path: {Path}, Error: {Error}",
                statusCode,
                context.Request.Path,
                exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,

            Title = statusCode switch
            {
                400 => "Bad Request",
                401 => "Unauthorized",
                404 => "Not Found",
                409 => "Conflict",
                _ => "Internal Server Error"
            },

            Detail = exception switch
            {
                DbUpdateConcurrencyException =>
                    "The requested resource was modified by another operation. Please refresh and try again.",

                _ when statusCode >= 500 =>
                    "An unexpected error occurred.",

                _ => exception.Message
            },

            Instance = context.Request.Path
        };

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(
            problem,
            cancellationToken: context.RequestAborted);
    }
}