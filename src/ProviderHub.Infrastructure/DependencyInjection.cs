using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProviderHub.Application.Abstractions.Authentication;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Infrastructure.Authentication;
using ProviderHub.Infrastructure.Persistence;
using ProviderHub.Infrastructure.Persistence.Repositories;

namespace ProviderHub.Infrastructure;

/// <summary>
/// Wires the implementations to the ports declared by the application layer. This is the only
/// place where the two sides meet, and the reason nothing above it mentions Entity Framework.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Name of the connection string this application expects.</summary>
    public const string ConnectionStringName = "ProviderHub";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services
            .AddPersistence(configuration)
            .AddAuthentication(configuration);
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Failing at startup with a clear message beats failing on the first request with a null
        // reference. A misconfigured deployment should never reach the point of accepting traffic.
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing. " +
                "See appsettings.Development.json for the local one.");

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

    private static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<JwtAuthenticationOptions>()
            .Bind(configuration.GetSection(JwtAuthenticationOptions.SectionName))
            .ValidateDataAnnotations()

            // Without this the options are validated the first time someone asks for them, which
            // is during a request. A deployment missing its signing key should fail to start,
            // not fail its first sign-in.
            .ValidateOnStart();

        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddSingleton<ICredentialVerifier, ConfiguredCredentialVerifier>();

        return services;
    }
}
