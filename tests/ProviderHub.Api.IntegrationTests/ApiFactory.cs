using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProviderHub.Infrastructure.Persistence;

namespace ProviderHub.Api.IntegrationTests;

/// <summary>
/// Boots the real application in memory and points it at a throwaway database.
/// <para>
/// Nothing is stubbed: the request travels through routing, model binding, the exception
/// handler, the use cases, Entity Framework and SQL Server, and comes back as an HTTP response.
/// These tests are the only ones that can catch what lives between the layers, such as a route
/// name that collides at startup or a validation error that never reaches the caller in the
/// shape the contract promises.
/// </para>
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DatabaseName = "ProviderHub_ApiTests";

    private static readonly string MasterConnection =
        Environment.GetEnvironmentVariable("PROVIDERHUB_TEST_CONNECTION")
        ?? "Server=localhost,1433;Database=master;User Id=sa;Password=ProviderHub!2026;" +
           "TrustServerCertificate=True;Encrypt=False";

    private static readonly Lazy<bool> Reachable = new(() =>
    {
        try
        {
            using var connection = new SqlConnection(MasterConnection);
            connection.Open();

            return true;
        }
        catch (SqlException)
        {
            return false;
        }
    });

    public static bool IsAvailable => Reachable.Value;

    private static string TestConnectionString =>
        new SqlConnectionStringBuilder(MasterConnection) { InitialCatalog = DatabaseName }.ConnectionString;

    public async Task InitializeAsync()
    {
        if (!IsAvailable)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProviderHubDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        if (IsAvailable)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ProviderHubDbContext>();
            await context.Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Overriding configuration rather than swapping the DbContext registration: the point is
        // to exercise the wiring the application actually uses, not a version of it built for
        // the tests. Only the database it talks to changes.
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:ProviderHub", TestConnectionString);
    }
}

/// <summary>A fact that is skipped, not failed, when no database is reachable.</summary>
public sealed class RequiresDatabaseFactAttribute : FactAttribute
{
    public RequiresDatabaseFactAttribute()
    {
        if (!ApiFactory.IsAvailable)
        {
            Skip = "No SQL Server on localhost,1433. Start it with: docker compose up -d";
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ApiTests : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
