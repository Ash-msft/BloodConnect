using BloodConnect.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BloodConnect.Tests.Integration;

/// <summary>
/// Spins up the full API host against an isolated, file-based SQLite database (one per factory
/// instance, created under the test output directory and deleted on dispose) so integration tests
/// exercise the real DI graph, EF Core migrations, and demo-data seeding exactly as in Development.
/// </summary>
public class BloodConnectApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(
        AppContext.BaseDirectory, $"bloodconnect-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();

        // SQLite may still hold the WAL/journal file open briefly after host shutdown; clearing the
        // connection pool releases native handles before we attempt to delete the database file.
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        foreach (var path in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>
    /// Creates an authenticated HttpClient for the given demo external id (e.g. "demo-priya").
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string demoExternalId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Demo-User", demoExternalId);
        return client;
    }

    public async Task<BloodConnectDbContext> CreateDbContextAsync()
    {
        var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BloodConnectDbContext>();
        await db.Database.EnsureCreatedAsync();
        return db;
    }
}
