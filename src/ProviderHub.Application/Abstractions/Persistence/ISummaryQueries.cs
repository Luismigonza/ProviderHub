using ProviderHub.Application.Summary.Contracts;

namespace ProviderHub.Application.Abstractions.Persistence;

/// <summary>
/// The read side of the system, kept apart from the repositories on purpose.
/// <para>
/// A repository hands back aggregates, because that is what the write side needs in order to
/// enforce its rules. A dashboard needs none of that: asking a repository for every provider and
/// counting in memory would load the whole database to produce four numbers, and it would get
/// slower exactly as the data grows.
/// </para>
/// <para>
/// So the read side gets its own port, returning the shape the screen wants and letting the
/// database do the counting. Same idea as CQRS, without the machinery: two models over one set
/// of tables, each good at its own job.
/// </para>
/// </summary>
public interface ISummaryQueries
{
    Task<SummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
