using Inventory.Application.Abstractions;
using Inventory.Domain.Identity;
using Inventory.Domain.Products;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure.Seeding;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; }
}

public sealed class DatabaseSeeder
{
    public const string DemoEmail = "demo@northline.local";
    public const string DemoPassword = "Demo1234!";
    public const string DemoDisplayName = "Demo Admin";

    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwords;
    private readonly SeedOptions _options;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        AppDbContext db,
        IPasswordHasher passwords,
        IOptions<SeedOptions> options,
        ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _passwords = passwords;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (!await _db.Users.AnyAsync(cancellationToken))
        {
            _db.Users.Add(AppUser.Create(
                DemoEmail,
                DemoDisplayName,
                _passwords.Hash(DemoPassword),
                "Admin",
                now));
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded demo user {Email}. The password is published in the README.", DemoEmail);
        }

        if (await _db.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var catalog = new (string Sku, string Name, string Description, decimal Price, int Quantity, int Reorder)[]
        {
            ("NL-COP-12", "Copper elbow, 1/2 in", "Sweat fitting for a domestic copper line.", 4.80m, 6, 12),
            ("NL-COP-34", "Copper elbow, 3/4 in", "Larger elbow kept for service calls.", 6.40m, 40, 15),
            ("NL-PIN-CR", "Pine crate, standard", "Slatted crate for counter stock.", 18.00m, 4, 8),
            ("NL-LED-A4", "Ledger paper, A4 ream", "500-sheet ream used at the bench.", 7.25m, 22, 10),
            ("NL-TAP-BL", "Packing tape, black", "Heavy tape, 48 mm roll.", 3.10m, 2, 20),
            ("NL-LBL-SK", "Shelf label, kraft", "Blank kraft label, sold by the sheet.", 0.15m, 500, 100)
        };

        foreach (var item in catalog)
        {
            var product = Product.Create(item.Sku, item.Name, item.Description, item.Price, item.Reorder, now);
            _db.Products.Add(product);
            if (item.Quantity > 0)
            {
                var movement = StockMovement.Record(
                    product,
                    StockMovementType.Receipt,
                    item.Quantity,
                    "Opening balance",
                    "seed",
                    now);
                _db.StockMovements.Add(movement);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} demo products.", catalog.Length);
    }
}
