using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProviderHub.Infrastructure.Authentication;
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

    /// <summary>Credentials this factory configures, so the tests never depend on the dev settings.</summary>
    public const string UserName = "test-user";

    public const string Password = "Sup3rSecret!2026";

    /// <summary>The address the system preferences point at during the tests.</summary>
    public const string NotificationRecipient = "operations@tekus.test";

    // Hashed with the very code the application uses, rather than pasted in as a constant: if
    // the hashing changes, these tests keep working, and if it breaks, they fail.
    private static readonly string PasswordHash = PasswordHasher.Hash(Password);

    /// <summary>Where the file transport drops the notifications this run produces.</summary>
    public string Outbox { get; } = Path.Combine(
        Path.GetTempPath(),
        "providerhub-tests",
        Guid.NewGuid().ToString("N"));

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

        if (Directory.Exists(Outbox))
        {
            Directory.Delete(Outbox, recursive: true);
        }

        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    /// <summary>A client that has already signed in, which is what most of the tests need.</summary>
    public async Task<HttpClient> CreateSignedInClientAsync()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { userName = UserName, password = Password });

        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<JsonElement>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Overriding configuration rather than swapping the DbContext registration: the point is
        // to exercise the wiring the application actually uses, not a version of it built for
        // the tests. Only the database it talks to changes.
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:ProviderHub", TestConnectionString);

        // Its own user and its own signing key, so a change to the developer's local settings
        // cannot make the test suite pass or fail.
        builder.UseSetting("Authentication:UserName", UserName);
        builder.UseSetting("Authentication:PasswordHash", PasswordHash);
        builder.UseSetting(
            "Authentication:SigningKey",
            "integration-tests-signing-key-that-is-long-enough-for-hmac-sha256");

        // Notifications are written to a folder of this run's own, so a test can read what would
        // have been sent without an SMTP server anywhere in sight.
        builder.UseSetting("Notifications:Transport", "File");
        builder.UseSetting("Notifications:OutboxDirectory", Outbox);
        builder.UseSetting("Notifications:NewServiceRecipient", NotificationRecipient);
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
