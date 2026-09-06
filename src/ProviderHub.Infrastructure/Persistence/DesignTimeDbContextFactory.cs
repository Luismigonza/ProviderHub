using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProviderHub.Infrastructure.Persistence;

/// <summary>
/// Builds a context for the <c>dotnet ef</c> tools.
/// <para>
/// Without it, generating a migration would require starting the whole API, which means the
/// database schema could only be changed from a project that also needs an HTTP host to be
/// configured. This keeps persistence self-contained: the connection string comes from
/// <c>PROVIDERHUB_CONNECTION</c> when it is set, and otherwise points at the local container
/// described in <c>docker-compose.yml</c>. It is used at design time only, never at runtime.
/// </para>
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ProviderHubDbContext>
{
    private const string LocalContainerConnection =
        "Server=localhost,1433;Database=ProviderHub;User Id=sa;Password=ProviderHub!2026;" +
        "TrustServerCertificate=True;Encrypt=False";

    public ProviderHubDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("PROVIDERHUB_CONNECTION") ?? LocalContainerConnection;

        var options = new DbContextOptionsBuilder<ProviderHubDbContext>()
            .UseSqlServer(connectionString, sqlServer =>
                sqlServer.MigrationsAssembly(typeof(ProviderHubDbContext).Assembly.FullName))
            .Options;

        return new ProviderHubDbContext(options);
    }
}
