using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Infrastructure.Persistence;
using ProviderHub.Infrastructure.Persistence.Repositories;

namespace ProviderHub.Infrastructure;

/// <summary>
/// Wires the implementations to the ports declared by the application layer. This is the only
/// place where the two sides meet, and the reason nothing above it mentions Entity Framework.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<ProviderHubDbContext>(options =>
            options.UseSqlServer(connectionString, sqlServer =>
            {
                sqlServer.MigrationsAssembly(typeof(ProviderHubDbContext).Assembly.FullName);

                // A container that is still starting, or a connection dropped by a failover, is
                // a transient fault: retrying beats failing the request.
                sqlServer.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), null);
            }));

        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();

        // The DbContext is the unit of work. Resolving the same instance through both types
        // keeps repositories and commit inside one change tracker, and therefore one transaction.
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ProviderHubDbContext>());

        return services;
    }
}
