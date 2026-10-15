using Microsoft.EntityFrameworkCore;

using Stockroom.Domain.Entities;

namespace Stockroom.Application.Abstractions;

public interface IStockroomDbContext
{
    DbSet<Product> Products { get; }

    DbSet<Customer> Customers { get; }

    DbSet<Order> Orders { get; }

    DbSet<OrderLine> OrderLines { get; }

    DbSet<StockMovement> StockMovements { get; }

    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
