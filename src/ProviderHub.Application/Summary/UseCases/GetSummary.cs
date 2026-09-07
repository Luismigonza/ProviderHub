using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Summary.Contracts;

namespace ProviderHub.Application.Summary.UseCases;

/// <summary>Asks for the dashboard figures.</summary>
public sealed record GetSummaryQuery;

/// <summary>
/// Deliberately thin: the query takes no input, so there is nothing to validate and nothing to
/// orchestrate. It earns its place by keeping the API layer talking to use cases rather than
/// reaching for a persistence port directly, and by being the one obvious place to add caching
/// the day these figures become expensive.
/// </summary>
public sealed class GetSummaryHandler(ISummaryQueries summaries)
{
    public async Task<SummaryDto> HandleAsync(
        GetSummaryQuery query,
        CancellationToken cancellationToken = default) =>
        await summaries.GetSummaryAsync(cancellationToken).ConfigureAwait(false);
}
