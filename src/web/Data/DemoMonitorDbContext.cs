using DemoMonitorApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoMonitorApp.Data;

/// <summary>
/// Provides Entity Framework access to the demo monitor application database.
/// </summary>
public sealed class DemoMonitorDbContext(DbContextOptions<DemoMonitorDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the persisted products shown by the demo monitor API.
    /// </summary>
    public DbSet<Product> Products => Set<Product>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DemoMonitorDbContext).Assembly);
    }
}
