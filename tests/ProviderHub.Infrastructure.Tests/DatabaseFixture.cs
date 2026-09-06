using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProviderHub.Infrastructure.Persistence;

namespace ProviderHub.Infrastructure.Tests;

/// <summary>
/// Creates a throwaway database for the whole test class and tears it down afterwards.
/// <para>
/// These tests run against a real SQL Server, on purpose. The unit tests already proved the
/// rules; what is being checked here is everything only the database can answer: whether the
/// mapping round-trips an aggregate, whether the unique indexes hold, and whether the LINQ that
/// searches and sorts actually translates to SQL. An in-memory provider would answer all three
/// questions convincingly and wrongly.
/// </para>
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string DatabaseName = "ProviderHub_IntegrationTests";

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

    /// <summary>Whether a SQL Server is listening, so the tests can be skipped instead of failing.</summary>
    public static bool IsAvailable => Reachable.Value;

    public string ConnectionString { get; } =
        new SqlConnectionStringBuilder(MasterConnection) { InitialCatalog = DatabaseName }.ConnectionString;

    public async Task InitializeAsync()
    {
        if (!IsAvailable)
        {
            return;
        }

        await using var context = CreateContext();

        // Starting from nothing every run: a test that depends on what a previous run left
        // behind is a test that passes locally and fails in CI.
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (!IsAvailable)
        {
            return;
        }

        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    /// <summary>
    /// A brand new context, which matters: reloading through the same one would only read back
    /// the objects the change tracker is already holding, and prove nothing about the mapping.
    /// </summary>
    public ProviderHubDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ProviderHubDbContext>()
            .UseSqlServer(ConnectionString)
            .Options);
}

/// <summary>
/// Ties every persistence test class to a single fixture instance.
/// <para>
/// With one fixture per class, each class would try to create and drop the same database at the
/// same time, and xUnit runs classes in parallel. A collection means one database, created once,
/// and the classes inside it run in sequence.
/// </para>
/// </summary>
[CollectionDefinition(Name)]
public sealed class SharedDatabase : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}

/// <summary>
/// A fact that is skipped, rather than failed, when no database is reachable. Cloning the
/// repository and running <c>dotnet test</c> without Docker should not look like broken code.
/// </summary>
public sealed class RequiresDatabaseFactAttribute : FactAttribute
{
    public RequiresDatabaseFactAttribute()
    {
        if (!DatabaseFixture.IsAvailable)
        {
            Skip = "No SQL Server on localhost,1433. Start it with: docker compose up -d";
        }
    }
}
