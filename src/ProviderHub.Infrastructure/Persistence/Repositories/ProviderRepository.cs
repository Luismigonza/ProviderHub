using Microsoft.EntityFrameworkCore;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Providers.ValueObjects;

namespace ProviderHub.Infrastructure.Persistence.Repositories;

/// <summary>
/// The Entity Framework side of <see cref="IProviderRepository"/>. Everything about SQL Server
/// stops here: no layer above this file knows the database exists.
/// </summary>
internal sealed class ProviderRepository(ProviderHubDbContext context) : IProviderRepository
{
    public async Task<Provider?> FindAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Providers
            .Include(provider => provider.Offerings)
            .FirstOrDefaultAsync(provider => provider.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<bool> NitExistsAsync(
        Nit nit,
        int? excludedProviderId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nit);

        var query = context.Providers.Where(provider => provider.Nit.BaseNumber == nit.BaseNumber);

        // Written as a separate step on purpose: "provider.Id != excludedProviderId" with a null
        // exclusion becomes "Id <> NULL" in SQL, which is never true, and the check would
        // silently stop finding duplicates.
        if (excludedProviderId is { } excluded)
        {
            query = query.Where(provider => provider.Id != excluded);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<Provider>> SearchAsync(
        PageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Providers
            .Include(provider => provider.Offerings)

            // Without this, a provider with five offerings across four countries is returned as
            // twenty duplicated rows for Entity Framework to fold back together. Split queries
            // ask for the parents and the children separately instead.
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{SearchPattern.Escape(request.Search)}%";

            query = query.Where(provider =>
                EF.Functions.Like(provider.Name, pattern)
                || EF.Functions.Like(provider.Nit.BaseNumber, pattern)
                || EF.Functions.Like(provider.Email.Value, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await Sort(query, request)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<Provider>(items, request.Page, request.PageSize, totalCount);
    }

    public void Add(Provider provider) => context.Providers.Add(provider);

    public void Remove(Provider provider) => context.Providers.Remove(provider);

    /// <summary>
    /// Applies one of the sort options the application layer allows.
    /// <para>
    /// Every branch ends on the identifier. Without that tiebreaker, rows sharing a name have no
    /// defined order, and the same row can appear on page one and again on page two while
    /// another one is never shown at all.
    /// </para>
    /// </summary>
    private static IQueryable<Provider> Sort(IQueryable<Provider> query, PageRequest request)
    {
        var descending = request.Direction == SortDirection.Descending;

        if (Matches(request.SortBy, ProviderSortFields.Nit))
        {
            return descending
                ? query.OrderByDescending(provider => provider.Nit.BaseNumber).ThenBy(provider => provider.Id)
                : query.OrderBy(provider => provider.Nit.BaseNumber).ThenBy(provider => provider.Id);
        }

        if (Matches(request.SortBy, ProviderSortFields.Email))
        {
            return descending
                ? query.OrderByDescending(provider => provider.Email.Value).ThenBy(provider => provider.Id)
                : query.OrderBy(provider => provider.Email.Value).ThenBy(provider => provider.Id);
        }

        // Name is also the default: a list with no explicit order is a list that changes shape
        // between two identical requests.
        return descending
            ? query.OrderByDescending(provider => provider.Name).ThenBy(provider => provider.Id)
            : query.OrderBy(provider => provider.Name).ThenBy(provider => provider.Id);
    }

    private static bool Matches(string? sortBy, string field) =>
        string.Equals(sortBy, field, StringComparison.OrdinalIgnoreCase);
}
