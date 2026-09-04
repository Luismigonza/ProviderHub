using ProviderHub.Application.Common;
using ProviderHub.Domain.Services;

namespace ProviderHub.Application.Abstractions.Persistence;

/// <summary>How the application layer reaches the service catalogue.</summary>
public interface IServiceRepository
{
    Task<Service?> FindAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Checks for a service with the same name, optionally ignoring the one being edited.</summary>
    Task<bool> NameExistsAsync(
        string name,
        int? excludedServiceId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads several services at once.
    /// <para>
    /// Provider details show the name and rate of every offered service. Asking for all of them
    /// in a single call keeps that screen at two queries instead of one per row, which is the
    /// classic N+1 problem.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<Service>> GetManyAsync(
        IReadOnlyCollection<int> serviceIds,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Service>> SearchAsync(PageRequest request, CancellationToken cancellationToken = default);

    void Add(Service service);

    void Remove(Service service);
}

/// <summary>Fields a service list can be sorted by.</summary>
public static class ServiceSortFields
{
    public const string Name = "name";
    public const string HourlyRate = "hourlyRate";

    public static IReadOnlyCollection<string> All { get; } = [Name, HourlyRate];
}
