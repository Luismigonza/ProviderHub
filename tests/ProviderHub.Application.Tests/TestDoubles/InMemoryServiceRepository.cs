using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Domain.Services;

namespace ProviderHub.Application.Tests.TestDoubles;

/// <summary>A service catalogue backed by a list. See <see cref="InMemoryProviderRepository"/>.</summary>
internal sealed class InMemoryServiceRepository : IServiceRepository
{
    private readonly List<Service> _services = [];
    private int _nextId = 1;

    public IReadOnlyList<Service> Items => _services;

    public Service Seed(Service service)
    {
        TestIdentity.Assign(service, _nextId++);
        _services.Add(service);

        return service;
    }

    public Task<Service?> FindAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_services.Find(service => service.Id == id));

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_services.Exists(service => service.Id == id));

    public Task<bool> NameExistsAsync(
        string name,
        int? excludedServiceId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_services.Exists(service =>
            string.Equals(service.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase)
            && service.Id != excludedServiceId));

    public Task<IReadOnlyList<Service>> GetManyAsync(
        IReadOnlyCollection<int> serviceIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Service>>(
            [.. _services.Where(service => serviceIds.Contains(service.Id))]);

    public Task<PagedResult<Service>> SearchAsync(
        PageRequest request,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Service> query = _services;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(service =>
                service.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase));
        }

        var matches = query.ToList();

        var page = matches
            .OrderBy(service => service.Name, StringComparer.OrdinalIgnoreCase)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<Service>(page, request.Page, request.PageSize, matches.Count));
    }

    public void Add(Service service) => Seed(service);

    public void Remove(Service service) => _services.Remove(service);
}

/// <summary>
/// Records how many times the use case committed, which is how the tests check that a handler
/// that should have saved did save, and that one that failed did not.
/// </summary>
internal sealed class RecordingUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;

        return Task.CompletedTask;
    }
}
