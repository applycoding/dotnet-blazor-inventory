using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Seeding;

namespace Inventory.Api.Hosting;

public static class DatabaseStartup
{
    public static async Task InitializeAsync(WebApplication app)
    {
        const int attempts = 8;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // One model serves SQL Server and SQLite, so this demo creates the schema from the
                // EF model instead of keeping two provider-specific migration sets.
                await db.Database.EnsureCreatedAsync();
                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                await seeder.SeedAsync();
                return;
            }
            catch (Exception exception) when (attempt < attempts && IsConnectionFailure(exception))
            {
                app.Logger.LogWarning(
                    exception,
                    "Database is not ready (attempt {Attempt} of {Attempts}).",
                    attempt,
                    attempts);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }

    private static bool IsConnectionFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current.GetType().Name == "SqlException")
            {
                return true;
            }

            var message = current.Message;
            if (message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase)
                || message.Contains("actively refused", StringComparison.OrdinalIgnoreCase)
                || message.Contains("network-related", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase)
                || message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
