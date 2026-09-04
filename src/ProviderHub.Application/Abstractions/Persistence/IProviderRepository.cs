using ProviderHub.Application.Common;
using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Providers.ValueObjects;

namespace ProviderHub.Application.Abstractions.Persistence;

/// <summary>
/// How the application layer reaches providers.
/// <para>
/// This interface lives here, next to the code that needs it, and is implemented one layer out
/// in Infrastructure. That inversion is the whole point of the architecture: the use cases
/// depend on an idea they defined themselves, not on Entity Framework. Swapping the ORM, or
/// running the tests against an in-memory list, changes nothing above this line.
/// </para>
/// </summary>
public interface IProviderRepository
{
    /// <summary>Loads a provider with its offerings, or <see langword="null"/> if there is none.</summary>
    Task<Provider?> FindAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a NIT is already registered. <c>excludedProviderId</c> names the provider
    /// being edited, so that saving it unchanged is not reported as a duplicate of itself.
    /// </summary>
    Task<bool> NitExistsAsync(Nit nit, int? excludedProviderId = null, CancellationToken cancellationToken = default);

    /// <summary>Returns one page of providers, filtered and sorted according to the request.</summary>
    Task<PagedResult<Provider>> SearchAsync(PageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new provider. Nothing is written until
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> is called.
    /// </summary>
    void Add(Provider provider);

    void Remove(Provider provider);
}

/// <summary>Fields a provider list can be sorted by.</summary>
public static class ProviderSortFields
{
    public const string Name = "name";
    public const string Nit = "nit";
    public const string Email = "email";

    public static IReadOnlyCollection<string> All { get; } = [Name, Nit, Email];
}
