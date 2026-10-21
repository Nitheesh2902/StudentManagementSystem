using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.API.Middleware;

/// <summary>Turns exceptions into consistent RFC 7807 problem responses. Never leaks internal details.</summary>
public class ExceptionHandlingMiddleware
{
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
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        ProblemDetails problem;

        switch (ex)
        {
            case ValidationException validation:
                var errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                problem = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred."
                };
                break;

            case UnauthorizedAccessException:
                problem = Simple(StatusCodes.Status401Unauthorized, "Authentication required.", ex.Message);
                break;

            case ForbiddenException:
                problem = Simple(StatusCodes.Status403Forbidden, "Forbidden.", ex.Message);
                break;

            case NotFoundException:
                problem = Simple(StatusCodes.Status404NotFound, "Resource not found.", ex.Message);
                break;

            case ConflictException:
                problem = Simple(StatusCodes.Status409Conflict, "Conflict.", ex.Message);
                break;

            case DbUpdateConcurrencyException:
                problem = Simple(StatusCodes.Status409Conflict, "Conflict.",
                    "The record was changed by someone else. Reload it and try again.");
                break;

            case DomainException:
                problem = Simple(StatusCodes.Status422UnprocessableEntity, "Business rule violation.", ex.Message);
                break;

            default:
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                problem = Simple(StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null);
                break;
        }

        problem.Instance = context.Request.Path;
        context.Response.StatusCode = problem.Status!.Value;
        await context.Response.WriteAsJsonAsync(
            problem, problem.GetType(), options: null, contentType: "application/problem+json");
    }

    private static ProblemDetails Simple(int status, string title, string? detail)
        => new() { Status = status, Title = title, Detail = detail };
}
