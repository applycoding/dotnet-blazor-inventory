using Microsoft.EntityFrameworkCore;

using Stockroom.Application.Abstractions;
using Stockroom.Domain.Entities;

namespace Stockroom.Infrastructure.Persistence;

public class StockroomDbContext : DbContext, IStockroomDbContext
{
    public StockroomDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StockroomDbContext).Assembly);
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            // SQLite cannot order by DateTimeOffset. Ticks keep both providers' queries portable.
            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()))
            {
                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(new DateTimeOffsetToTicksConverter());
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(new NullableDateTimeOffsetToTicksConverter());
                }
            }
        }
    }
}

public sealed class SqliteStockroomDbContext : StockroomDbContext
{
    public SqliteStockroomDbContext(DbContextOptions<SqliteStockroomDbContext> options)
        : base(options)
    {
    }
}

public sealed class SqlServerStockroomDbContext : StockroomDbContext
{
    public SqlServerStockroomDbContext(DbContextOptions<SqlServerStockroomDbContext> options)
        : base(options)
    {
    }
}
