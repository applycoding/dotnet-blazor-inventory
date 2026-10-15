using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Stockroom.Application.Abstractions;
using Stockroom.Application.Options;
using Stockroom.Infrastructure.Identity;
using Stockroom.Infrastructure.Persistence;

namespace Stockroom.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, JwtOptions jwt)
    {
        services.AddSingleton(jwt);
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();

        var provider = DatabaseProviders.Parse(configuration["DatabaseProvider"]);
        if (provider == DatabaseProviders.Sqlite)
        {
            var connectionString = configuration.GetConnectionString("Sqlite");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:Sqlite is required when DatabaseProvider is Sqlite.");
            }

            services.AddDbContext<SqliteStockroomDbContext>(options => options.UseSqlite(connectionString));
            services.AddScoped<StockroomDbContext>(sp => sp.GetRequiredService<SqliteStockroomDbContext>());
        }
        else
        {
            var connectionString = configuration.GetConnectionString("SqlServer");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:SqlServer is required when DatabaseProvider is SqlServer.");
            }

            services.AddDbContext<SqlServerStockroomDbContext>(options =>
                options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(5)));
            services.AddScoped<StockroomDbContext>(sp => sp.GetRequiredService<SqlServerStockroomDbContext>());
        }

        services.AddScoped<IStockroomDbContext>(sp => sp.GetRequiredService<StockroomDbContext>());
        return services;
    }
}
