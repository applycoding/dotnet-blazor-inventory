using FluentValidation;
using Inventory.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.ExceptionHandling;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validation => ValidationProblem(validation),
            DomainException domain => DomainProblem(domain),
            _ => Unexpected(exception)
        };

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync((object)problem, cancellationToken);
        return true;
    }

    private static ProblemDetails ValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => string.IsNullOrWhiteSpace(error.PropertyName) ? string.Empty : error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
        return new ValidationProblemDetails(errors)
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest
        };
    }

    private static ProblemDetails DomainProblem(DomainException exception)
    {
        var status = exception.Kind switch
        {
            DomainErrorKind.Validation => StatusCodes.Status400BadRequest,
            DomainErrorKind.NotFound => StatusCodes.Status404NotFound,
            DomainErrorKind.Conflict => StatusCodes.Status409Conflict,
            DomainErrorKind.InsufficientStock => StatusCodes.Status422UnprocessableEntity,
            DomainErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest
        };

        return new ProblemDetails
        {
            Title = exception.Message,
            Status = status
        };
    }

    private ProblemDetails Unexpected(Exception exception)
    {
        _logger.LogError(exception, "Unhandled API exception");
        return new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Detail = _environment.IsDevelopment() ? exception.Message : null,
            Status = StatusCodes.Status500InternalServerError
        };
    }
}
