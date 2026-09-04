using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Providers.ValueObjects;

namespace ProviderHub.Application.Tests.TestDoubles;

/// <summary>
/// A provider repository backed by a list.
/// <para>
/// This is what the dependency inversion of the architecture buys: the use cases can be tested
/// completely, with real domain objects and real rules, and not a single database in sight. The
/// tests run in milliseconds and never fail for reasons that have nothing to do with the code.
/// </para>
/// <para>
/// What it does <b>not</b> prove is that searching and sorting behave the same in SQL Server.
/// That is what the integration tests are for.
/// </para>
/// </summary>
internal sealed class InMemoryProviderRepository : IProviderRepository
{
    private readonly List<Provider> _providers = [];
    private int _nextId = 1;

    public IReadOnlyList<Provider> Items => _providers;

    /// <summary>Puts an already existing provider in the store, as if it had been saved before.</summary>
    public Provider Seed(Provider provider)
    {
        TestIdentity.Assign(provider, _nextId++);
        _providers.Add(provider);

        return provider;
    }

    public Task<Provider?> FindAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_providers.Find(provider => provider.Id == id));

    public Task<bool> NitExistsAsync(
        Nit nit,
        int? excludedProviderId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_providers.Exists(provider =>
            provider.Nit == nit && provider.Id != excludedProviderId));

    public Task<PagedResult<Provider>> SearchAsync(
        PageRequest request,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Provider> query = _providers;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(provider =>
                provider.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase)
                || provider.Nit.Value.Contains(request.Search, StringComparison.OrdinalIgnoreCase));
        }

        var matches = query.ToList();

        var page = matches
            .OrderBy(provider => provider.Name, StringComparer.OrdinalIgnoreCase)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<Provider>(page, request.Page, request.PageSize, matches.Count));
    }

    public void Add(Provider provider) => Seed(provider);

    public void Remove(Provider provider) => _providers.Remove(provider);
}
