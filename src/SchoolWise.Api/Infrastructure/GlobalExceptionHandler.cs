using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SchoolWise.Api.Infrastructure;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed", "validation_error"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The resource changed", "concurrency_conflict"),
            DbUpdateException
            {
                InnerException: PostgresException { SqlState: "23505", ConstraintName: "ix_reviews_user_id_school_id" }
            } => (StatusCodes.Status409Conflict, "You already reviewed this school. Reload My Reviews.", "review_exists"),
            DbUpdateException
            {
                InnerException: PostgresException { SqlState: "23505", ConstraintName: "ix_review_versions_review_id_version" }
            } => (StatusCodes.Status409Conflict, "This review changed. Reload before trying again.", "review_conflict"),
            DbUpdateException
            {
                InnerException: PostgresException { SqlState: "23503" or "23001", ConstraintName: "fk_reviews_schools_school_id" }
            } => (StatusCodes.Status409Conflict, "This school has review history. Unapprove it instead of deleting it.", "school_has_reviews"),
            DbUpdateException
            {
                InnerException: PostgresException
                {
                    SqlState: "23503" or "23001",
                    ConstraintName: "fk_verification_codes_schools_school_id" or "fk_parent_school_verifications_schools_school_id"
                }
            } => (StatusCodes.Status409Conflict, "This school has verification history. Unapprove it instead of deleting it.", "school_has_verifications"),
            DbUpdateException
            {
                InnerException: PostgresException
                {
                    SqlState: "23505",
                    ConstraintName: "ix_users_email" or "ix_schools_slug"
                }
            } => (StatusCodes.Status409Conflict, "A resource with the same unique value already exists.", "unique_conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "unexpected_error")
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception for request {RequestId}", httpContext.TraceIdentifier);
        }
        else
        {
            logger.LogWarning(exception, "Request rejected with {ErrorCode}", code);
        }

        httpContext.Response.StatusCode = status;
        var details = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://httpstatuses.com/{status}",
            Instance = httpContext.Request.Path
        };
        details.Extensions["code"] = code;
        details.Extensions["requestId"] = httpContext.TraceIdentifier;

        if (exception is ValidationException validationException)
        {
            details.Extensions["errors"] = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = details
        });
    }
}
