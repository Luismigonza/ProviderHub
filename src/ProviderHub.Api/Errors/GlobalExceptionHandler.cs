using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProviderHub.Application.Common;
using ProviderHub.Domain.Common;

namespace ProviderHub.Api.Errors;

/// <summary>
/// Turns the exceptions the inner layers speak into HTTP status codes.
/// <para>
/// This is the only place in the solution that knows a 404 exists. The use cases raise
/// <see cref="NotFoundException"/> because that is the language of the application, and this
/// handler translates it at the boundary. Doing it here rather than in every controller means no
/// endpoint can forget, and adding a new use case cannot introduce an inconsistent error shape.
/// </para>
/// <para>
/// Every response is a <c>ProblemDetails</c> (RFC 9457), so a client parses failures the same way
/// whichever endpoint produced them.
/// </para>
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = Describe(exception);

        // PathString converts to a string on every read, so it is done once rather than inside
        // the logging call, where the analyzer rightly points out it would happen even when the
        // level is disabled.
        var path = httpContext.Request.Path.Value ?? string.Empty;

        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            // Unexpected: worth an alert, and worth keeping the details out of the response.
            LogUnhandled(logger, exception, path);
        }
        else
        {
            LogRejected(logger, path, problemDetails.Status, exception.Message);
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        }).ConfigureAwait(false);
    }

    // Source-generated log methods: the message template is compiled once instead of being
    // parsed on every call, and nothing is allocated when the level is disabled.
    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing {Path}.")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request to {Path} rejected with {Status}: {Reason}")]
    private static partial void LogRejected(ILogger logger, string path, int? status, string reason);

    private static ProblemDetails Describe(Exception exception) => exception switch
    {
        ValidationException validation => new ValidationProblemDetails(ToFieldErrors(validation))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
        },

        NotFoundException notFound => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Resource not found.",
            Detail = notFound.Message,
        },

        ConflictException conflict => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "The request conflicts with the current state.",
            Detail = conflict.Message,
        },

        // Reaching this means a validator is missing upstream: the model defended itself against
        // input the application layer should have rejected first. It is still the caller's
        // mistake, so it is a 400, but it is worth noticing in the logs.
        DomainException domain => new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "The request violates a business rule.",
            Detail = domain.Message,
        },

        // Anything else is a bug. The client is told that something failed and nothing else:
        // stack traces and connection strings do not belong in an HTTP response.
        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
        },
    };

    /// <summary>
    /// Groups the failures by field, so the caller receives every problem at once instead of
    /// fixing one, retrying, and discovering the next.
    /// </summary>
    private static Dictionary<string, string[]> ToFieldErrors(ValidationException exception) =>
        exception.Errors
            .GroupBy(failure => ToCamelCase(failure.PropertyName), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

    /// <summary>
    /// FluentValidation reports CLR property names; the JSON contract is camel case. Without
    /// this, a client would have to match "Nit" against a field it received as "nit".
    /// </summary>
    private static string ToCamelCase(string propertyName) =>
        string.IsNullOrEmpty(propertyName) || char.IsLower(propertyName[0])
            ? propertyName
            : char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
}
