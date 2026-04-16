namespace DemoMonitorApp.Models;

/// <summary>
/// Represents a product returned by the demo monitor API and stored in Azure SQL.
/// </summary>
public sealed class Product
{
    internal const int MaxNameLength = 200;
    internal const int MaxDescriptionLength = 1000;

    /// <summary>
    /// Gets or sets the database identifier for the product.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the product.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the optional product description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets when the product record was created in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
