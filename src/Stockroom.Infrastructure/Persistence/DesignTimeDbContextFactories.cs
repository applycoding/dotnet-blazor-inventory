using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Stockroom.Infrastructure.Persistence;

public sealed class SqliteStockroomDbContextFactory : IDesignTimeDbContextFactory<SqliteStockroomDbContext>
{
    public SqliteStockroomDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SqliteStockroomDbContext>()
            .UseSqlite("Data Source=stockroom.design.db")
            .Options;
        return new SqliteStockroomDbContext(options);
    }
}

public sealed class SqlServerStockroomDbContextFactory : IDesignTimeDbContextFactory<SqlServerStockroomDbContext>
{
    public SqlServerStockroomDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SqlServerStockroomDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=Stockroom;User Id=sa;Password=Stockroom-demo-9!;TrustServerCertificate=True")
            .Options;
        return new SqlServerStockroomDbContext(options);
    }
}
