using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Stockroom.IntegrationTests;

public sealed class StockroomApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"stockroom-{Guid.NewGuid():N}.db");

    public StockroomApiFactory()
    {
        Environment.SetEnvironmentVariable("DatabaseProvider", "Sqlite");
        Environment.SetEnvironmentVariable("ConnectionStrings__Sqlite", $"Data Source={_databasePath};Pooling=False");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "stockroom");
        Environment.SetEnvironmentVariable("Jwt__Audience", "stockroom");
        Environment.SetEnvironmentVariable("Jwt__Key", "integration-test-signing-key-32bytes!");
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        TryDelete(_databasePath);
        TryDelete(_databasePath + "-wal");
        TryDelete(_databasePath + "-shm");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // The test process is exiting; a leftover temp file is harmless.
        }
    }
}
