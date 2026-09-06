using Microsoft.EntityFrameworkCore;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Services;

namespace ProviderHub.Infrastructure.Persistence;

/// <summary>
/// The Entity Framework session, and the implementation of
/// <see cref="IUnitOfWork"/>.
/// <para>
/// The change tracker already is a unit of work: it accumulates every modification and writes
/// them in one transaction on save. Exposing it through the application's own interface means
/// the use cases can commit without ever naming Entity Framework.
/// </para>
/// </summary>
public sealed class ProviderHubDbContext(DbContextOptions<ProviderHubDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Provider> Providers => Set<Provider>();

    public DbSet<Service> Services => Set<Service>();

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // One configuration class per aggregate, discovered by scanning. Mapping lives here and
        // not as attributes on the entities: the domain must not know it is being persisted.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProviderHubDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
