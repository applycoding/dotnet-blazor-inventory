using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Stockroom.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Stockroom.Database");
        var configuration = services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var provider = DatabaseProviders.Parse(configuration["DatabaseProvider"]);
        var attempts = provider == DatabaseProviders.SqlServer ? 12 : 1;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<StockroomDbContext>();
                await db.Database.MigrateAsync(cancellationToken);
                await DatabaseSeeder.SeedAsync(db, cancellationToken);
                logger.LogInformation("Database ready ({Provider}).", provider);
                return;
            }
            catch (Exception ex) when (attempt < attempts)
            {
                logger.LogWarning(ex, "Database not ready, attempt {Attempt} of {Attempts}.", attempt, attempts);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}
