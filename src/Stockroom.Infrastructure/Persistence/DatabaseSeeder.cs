using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Stockroom.Domain.Entities;
using Stockroom.Domain.Enums;

namespace Stockroom.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(StockroomDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var hasher = new PasswordHasher<User>();
        var admin = User.Create("admin@stockroom.demo", "Demo Admin", UserRole.Admin, "pending");
        var clerk = User.Create("clerk@stockroom.demo", "Demo Clerk", UserRole.Clerk, "pending");
        var adminWithHash = User.Create(admin.Email, admin.DisplayName, admin.Role, hasher.HashPassword(admin, "Demo-Admin-123!"));
        var clerkWithHash = User.Create(clerk.Email, clerk.DisplayName, clerk.Role, hasher.HashPassword(clerk, "Demo-Clerk-123!"));
        db.Users.AddRange(adminWithHash, clerkWithHash);

        var today = DateTimeOffset.UtcNow;
        var kettle = Product.Create("KET-01", "Gooseneck kettle", "1 L, variable temperature.", 42.00m, 12, 6, today.AddDays(-20));
        kettle.AdjustStock(6);
        var dripper = Product.Create("DRP-02", "Ceramic dripper", "Size 02.", 28.00m, 4, 8, today.AddDays(-20));
        var filters = Product.Create("FLT-03", "Paper filters", "Box of 100.", 6.50m, 40, 12, today.AddDays(-18));
        var scale = Product.Create("SCL-04", "Brewing scale", "0.1 g, with a timer.", 54.00m, 11, 4, today.AddDays(-18));
        var cups = Product.Create("CUP-05", "Tasting cup set", "Set of four.", 18.00m, 4, 6, today.AddDays(-16));
        var server = Product.Create("SRV-06", "Glass server", "360 ml.", 22.00m, 3, 4, today.AddDays(-16));
        server.AdjustStock(-3);
        cups.AdjustStock(-2);

        db.Products.AddRange(kettle, dripper, filters, scale, cups, server);
        db.StockMovements.AddRange(
            StockMovement.Record(kettle.Id, 12, 12, "Opening balance", today.AddDays(-20)),
            StockMovement.Record(kettle.Id, 6, 18, "Received from supplier", today.AddDays(-4)),
            StockMovement.Record(dripper.Id, 4, 4, "Opening balance", today.AddDays(-20)),
            StockMovement.Record(filters.Id, 40, 40, "Opening balance", today.AddDays(-18)),
            StockMovement.Record(scale.Id, 11, 11, "Opening balance", today.AddDays(-18)),
            StockMovement.Record(cups.Id, 4, 4, "Opening balance", today.AddDays(-16)),
            StockMovement.Record(server.Id, 3, 3, "Opening balance", today.AddDays(-16)),
            StockMovement.Record(server.Id, -3, 0, "Count correction", today.AddDays(-2)));

        var harbor = Customer.Create("Harbor Cafe", "orders@harborcafe.example", "555-0142", today.AddDays(-12));
        var northMill = Customer.Create("North Mill Roasters", "desk@northmill.example", "555-0198", today.AddDays(-10));
        var littleElm = Customer.Create("Little Elm Grocery", "buyer@littleelm.example", null, today.AddDays(-9));
        db.Customers.AddRange(harbor, northMill, littleElm);

        var year = today.Year;
        var fulfilledAt = today.AddDays(-6);
        var fulfilled = Order.Create($"SR-{year}-0001", littleElm.Id, littleElm.Name, fulfilledAt);
        fulfilled.AddLine(cups.Id, cups.Sku, cups.Name, 2, cups.UnitPrice);
        fulfilled.Submit(fulfilledAt);
        fulfilled.MarkFulfilled(fulfilledAt);
        db.StockMovements.Add(StockMovement.Record(cups.Id, -2, 2, $"Fulfilled {fulfilled.OrderNumber}", fulfilledAt));

        var submittedAt = today.AddDays(-3);
        var submitted = Order.Create($"SR-{year}-0002", harbor.Id, harbor.Name, submittedAt);
        submitted.AddLine(kettle.Id, kettle.Sku, kettle.Name, 2, kettle.UnitPrice);
        submitted.AddLine(filters.Id, filters.Sku, filters.Name, 3, filters.UnitPrice);
        submitted.Submit(submittedAt);

        var draftAt = today.AddDays(-1);
        var draft = Order.Create($"SR-{year}-0003", northMill.Id, northMill.Name, draftAt);
        draft.AddLine(dripper.Id, dripper.Sku, dripper.Name, 1, dripper.UnitPrice);
        draft.AddLine(scale.Id, scale.Sku, scale.Name, 1, scale.UnitPrice);

        db.Orders.AddRange(fulfilled, submitted, draft);
        await db.SaveChangesAsync(cancellationToken);
    }
}
