namespace ProviderHub.Application.Common;

/// <summary>Direction of a sort.</summary>
public enum SortDirection
{
    Ascending = 0,
    Descending = 1,
}

/// <summary>
/// The three things the test requires of every list, in one place: which slice of the results is
/// wanted, what to search for, and how to order it.
/// <para>
/// Keeping them together means the rules are written once instead of once per endpoint, and that
/// every list of the API ends up behaving the same way, which is what makes an API predictable.
/// </para>
/// </summary>
public sealed record PageRequest
{
    public const int DefaultPageSize = 20;

    /// <summary>
    /// Upper bound on the page size. Without it, <c>?pageSize=1000000</c> is a denial of service
    /// that any visitor can trigger by editing the address bar.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>One-based page number.</summary>
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>Free-text filter. Which fields it looks into is decided per list.</summary>
    public string? Search { get; init; }

    /// <summary>
    /// Field to sort by, restricted by each list to a known set of names. Passing the value
    /// straight to the database would be an injection waiting to happen.
    /// </summary>
    public string? SortBy { get; init; }

    public SortDirection Direction { get; init; } = SortDirection.Ascending;

    /// <summary>Number of rows to skip to reach the requested page.</summary>
    public int Skip => (Page - 1) * PageSize;
}

/// <summary>
/// One page of results together with everything a client needs to render a pager without
/// guessing: which page this is, how big it is, and how many rows exist in total.
/// </summary>
/// <typeparam name="T">Type of the items in the page.</typeparam>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Converts the items while preserving the pagination metadata. It lets a repository return
    /// domain objects and a use case turn them into DTOs without rebuilding the page by hand.
    /// </summary>
    public PagedResult<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return new PagedResult<TResult>([.. Items.Select(selector)], Page, PageSize, TotalCount);
    }
}
