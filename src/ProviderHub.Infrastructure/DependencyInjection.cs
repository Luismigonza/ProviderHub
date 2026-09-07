using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProviderHub.Application.Abstractions.Authentication;
using ProviderHub.Application.Abstractions.Events;
using ProviderHub.Application.Abstractions.Messaging;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Providers.EventHandlers;
using ProviderHub.Domain.Providers.Events;
using ProviderHub.Infrastructure.Authentication;
using ProviderHub.Infrastructure.Events;
using ProviderHub.Infrastructure.Messaging;
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
            .AddAuthentication(configuration)
            .AddNotifications(configuration);
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

        // Repositories and the unit of work share one DbContext per request, and therefore one
        // change tracker and one transaction.
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    private static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<NotificationOptions>()
            .Bind(configuration.GetSection(NotificationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<INotificationPreferences, ConfiguredNotificationPreferences>();

        // Which transport is used is a deployment decision, not a code one. Development writes
        // files; anything else needs a server, and saying so in configuration keeps the choice
        // out of the code that sends.
        var transport = configuration
            .GetSection(NotificationOptions.SectionName)
            .GetValue<EmailTransport>(nameof(NotificationOptions.Transport));

        if (transport == EmailTransport.Smtp)
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, FileEmailSender>();
        }

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Registered against the event it handles, which is how the dispatcher finds it. A
        // second reaction to the same event is one more line here and nothing else.
        services.AddScoped<IDomainEventHandler<ServiceOfferedDomainEvent>, ServiceOfferedEmailNotifier>();

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
