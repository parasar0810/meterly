using Meterly.Application.Common.Exceptions;
using Meterly.Application.Common.Models;
using System.Text.Json;

namespace Meterly.Api.Middleware;

/// <summary>
/// Global exception handling middleware.
///
/// WHY A GLOBAL EXCEPTION HANDLER?
/// Without this, any unhandled exception in a controller or service would
/// either crash the request or (worse) return an HTML error page to an API
/// consumer that expects JSON. It might also leak stack traces to the outside
/// world — a significant security risk.
///
/// HOW MIDDLEWARE WORKS:
/// Middleware is a chain of handlers. When a request comes in, it flows
/// through each middleware in order. When a middleware calls `_next(context)`,
/// it hands control to the next middleware in the chain.
///
/// This particular middleware wraps the entire chain in a try/catch.
/// If anything throws, we catch it HERE and return a clean JSON error.
/// The user sees: { "succeeded": false, "message": "Resource not found." }
/// Not: NullReferenceException with a 50-line stack trace.
///
/// IMPORTANT: This middleware must be registered FIRST in Program.cs
/// so it wraps everything else.
/// </summary>
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
            // Pass control to the next middleware.
            // If nothing throws, the response flows back normally.
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Extract the trace ID from the request for correlation.
        // When a user reports an error, they can share the traceId and you can
        // find the exact log entry in your logging system.
        var traceId = context.TraceIdentifier;

        int statusCode;
        string message;
        IReadOnlyDictionary<string, string[]>? errors = null;

        switch (exception)
        {
            // Map our custom exception types to HTTP status codes
            case MeterlyException meterlyEx:
                statusCode = meterlyEx.StatusCode;
                message = meterlyEx.Message;

                if (meterlyEx is ValidationException validationEx)
                    errors = validationEx.Errors;

                // Validation and not-found errors are expected — log as Warning, not Error
                if (statusCode is 400 or 404 or 422)
                    _logger.LogWarning("Business exception: {Message} | TraceId: {TraceId}",
                        message, traceId);
                else
                    _logger.LogError(exception, "Application exception | TraceId: {TraceId}", traceId);
                break;

            // Anything unexpected — log as Error and return 500
            default:
                statusCode = 500;
                message = "An unexpected error occurred. Please try again later.";

                // Log the full exception (with stack trace) for debugging.
                // We do NOT expose the stack trace to the API response.
                _logger.LogError(exception,
                    "Unhandled exception | TraceId: {TraceId}", traceId);
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        // Build a consistent error response structure
        var response = new
        {
            succeeded = false,
            message,
            errors,
            traceId,
            statusCode
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
