using FluentValidation;
using FluentValidation.Results;
using ProviderHub.Application.Common;

namespace ProviderHub.Api.Contracts;

/// <summary>
/// The paging, searching and sorting options as they arrive on the query string.
/// <para>
/// It exists so that HTTP conventions stay in the HTTP layer: a caller writes
/// <c>?direction=desc</c>, which is what people expect from a REST API, while the application
/// layer keeps working with a typed <see cref="SortDirection"/> and never sees a loose string.
/// </para>
/// </summary>
public sealed record ListQuery
{
    /// <summary>One-based page number. Defaults to the first page.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Rows per page, capped at <see cref="PageRequest.MaxPageSize"/>.</summary>
    public int PageSize { get; init; } = PageRequest.DefaultPageSize;

    /// <summary>Free-text filter.</summary>
    public string? Search { get; init; }

    /// <summary>Field to sort by. Each list documents which ones it accepts.</summary>
    public string? SortBy { get; init; }

    /// <summary>Either <c>asc</c> or <c>desc</c>. Defaults to ascending.</summary>
    public string? Direction { get; init; }

    /// <exception cref="ValidationException">The direction is neither asc nor desc.</exception>
    public PageRequest ToPageRequest() => new()
    {
        Page = Page,
        PageSize = PageSize,
        Search = Search,
        SortBy = SortBy,
        Direction = ParseDirection(),
    };

    private SortDirection ParseDirection()
    {
        if (string.IsNullOrWhiteSpace(Direction))
        {
            return SortDirection.Ascending;
        }

        if (string.Equals(Direction, "asc", StringComparison.OrdinalIgnoreCase))
        {
            return SortDirection.Ascending;
        }

        if (string.Equals(Direction, "desc", StringComparison.OrdinalIgnoreCase))
        {
            return SortDirection.Descending;
        }

        // Quietly falling back to ascending would hide the typo and return a list the caller did
        // not ask for. Failing is the kinder behaviour.
        throw new ValidationException([
            new ValidationFailure(nameof(Direction), "Direction must be either 'asc' or 'desc'.")
        ]);
    }
}
