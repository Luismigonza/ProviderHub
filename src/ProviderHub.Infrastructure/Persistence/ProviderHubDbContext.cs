using Microsoft.EntityFrameworkCore;
using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Services;

namespace ProviderHub.Infrastructure.Persistence;

/// <summary>
/// The Entity Framework session.
/// <para>
/// The change tracker already is a unit of work: it accumulates every modification and writes
/// them in one transaction on save. It is <see cref="UnitOfWork"/> that exposes this through the
/// application's own interface, so that committing and publishing domain events stay one
/// deliberate sequence rather than an override buried in the context.
/// </para>
/// </summary>
public sealed class ProviderHubDbContext(DbContextOptions<ProviderHubDbContext> options)
    : DbContext(options)
{
    public DbSet<Provider> Providers => Set<Provider>();

    public DbSet<Service> Services => Set<Service>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // One configuration class per aggregate, discovered by scanning. Mapping lives here and
        // not as attributes on the entities: the domain must not know it is being persisted.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProviderHubDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
