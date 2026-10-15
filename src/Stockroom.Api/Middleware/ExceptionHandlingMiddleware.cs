using System.Text.Json;

using FluentValidation;

using Stockroom.Application.Common;
using Stockroom.Domain.Common;

namespace Stockroom.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            throw exception;
        }

        var (status, title, detail, errors) = Map(exception);
        if (status >= 500)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogInformation("Request rejected with {Status}: {Detail}", status, detail);
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        var payload = new Dictionary<string, object?>
        {
            ["title"] = title,
            ["status"] = status,
            ["detail"] = detail
        };
        if (errors is not null)
        {
            payload["errors"] = errors;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static (int Status, string Title, string Detail, IDictionary<string, string[]>? Errors) Map(Exception exception)
    {
        return exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "See errors for details.",
                validation.Errors
                    .GroupBy(failure => string.IsNullOrWhiteSpace(failure.PropertyName) ? "request" : failure.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray())),
            UnauthorizedException unauthorized => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                unauthorized.Message,
                null),
            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Not found",
                notFound.Message,
                null),
            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflict.Message,
                null),
            DomainException domain => (
                StatusCodes.Status422UnprocessableEntity,
                "Request could not be completed",
                domain.Message,
                null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                "The request could not be completed.",
                null)
        };
    }
}
