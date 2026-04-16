using DemoMonitorApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoMonitorApp.Data;

internal sealed class ProductCatalogInitializer(
    IDbContextFactory<DemoMonitorDbContext> dbContextFactory,
    ILogger<ProductCatalogInitializer> logger)
{
    public async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Applied EF Core migrations for the Products catalog.");

        if (!await dbContext.Products.AnyAsync(cancellationToken))
        {
            dbContext.Products.AddRange(
                new Product
                {
                    Name = "Demo Product 1",
                    Price = 19.99m,
                    Description = "Sample product for demo",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Demo Product 2",
                    Price = 29.99m,
                    Description = "Another sample product",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Demo Product 3",
                    Price = 39.99m,
                    Description = "Third sample product",
                    CreatedAt = DateTime.UtcNow
                });

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded the initial Products data.");
        }
    }
}
