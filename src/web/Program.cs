using Microsoft.ApplicationInsights;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

var telemetryClient = app.Services.GetRequiredService<TelemetryClient>();

// Demo endpoints that generate different types of metrics and logs
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/api/health", (TelemetryClient telemetry) =>
{
    telemetry.TrackEvent("HealthCheck", new Dictionary<string, string> { { "Status", "Healthy" } });
    return Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
});

app.MapGet("/api/products", async (TelemetryClient telemetry) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    try
    {
        // Simulate some processing time
        await Task.Delay(Random.Shared.Next(100, 500));
        
        var products = new[]
        {
            new { Id = 1, Name = "Demo Product 1", Price = 19.99m, Description = "Sample product for demo" },
            new { Id = 2, Name = "Demo Product 2", Price = 29.99m, Description = "Another sample product" },
            new { Id = 3, Name = "Demo Product 3", Price = 39.99m, Description = "Third sample product" }
        };
        
        stopwatch.Stop();
        telemetry.TrackDependency("InMemory", "GetProducts", DateTime.UtcNow.AddMilliseconds(-stopwatch.ElapsedMilliseconds), stopwatch.Elapsed, true);
        telemetry.TrackMetric("ProductCount", products.Length);
        
        return Results.Ok(products);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        telemetry.TrackDependency("InMemory", "GetProducts", DateTime.UtcNow.AddMilliseconds(-stopwatch.ElapsedMilliseconds), stopwatch.Elapsed, false);
        telemetry.TrackException(ex);
        return Results.Problem("Error retrieving products");
    }
});

app.MapPost("/api/products", async (Product product, TelemetryClient telemetry) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    try
    {
        // Simulate saving to database
        await Task.Delay(Random.Shared.Next(50, 200));
        product.Id = Random.Shared.Next(1000, 9999);
        
        stopwatch.Stop();
        telemetry.TrackDependency("InMemory", "CreateProduct", DateTime.UtcNow.AddMilliseconds(-stopwatch.ElapsedMilliseconds), stopwatch.Elapsed, true);
        telemetry.TrackEvent("ProductCreated", new Dictionary<string, string> { { "ProductName", product.Name } });
        telemetry.TrackMetric("ProductCreated", 1);
        
        return Results.Created($"/api/products/{product.Id}", product);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        telemetry.TrackDependency("InMemory", "CreateProduct", DateTime.UtcNow.AddMilliseconds(-stopwatch.ElapsedMilliseconds), stopwatch.Elapsed, false);
        telemetry.TrackException(ex);
        return Results.Problem("Error creating product");
    }
});

app.MapGet("/api/simulate-error", (TelemetryClient telemetry) =>
{
    // Randomly generate errors for demo purposes
    if (Random.Shared.Next(1, 100) <= 30) // 30% chance of error
    {
        var exception = new InvalidOperationException("Simulated error for demo purposes");
        telemetry.TrackException(exception);
        telemetry.TrackEvent("ErrorSimulated", new Dictionary<string, string> { { "ErrorType", "Simulated" } });
        throw exception;
    }
    
    telemetry.TrackEvent("SuccessfulOperation");
    return Results.Ok(new { Message = "Operation completed successfully", Timestamp = DateTime.UtcNow });
});

app.MapGet("/api/load-test", async (TelemetryClient telemetry) =>
{
    // Simulate high CPU load for metrics demonstration
    var startTime = DateTime.UtcNow;
    var tasks = new List<Task>();
    
    for (int i = 0; i < 10; i++)
    {
        tasks.Add(Task.Run(() =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 2000) // 2 seconds of work
            {
                Math.Sqrt(Random.Shared.NextDouble());
            }
        }));
    }
    
    await Task.WhenAll(tasks);
    
    var duration = DateTime.UtcNow - startTime;
    telemetry.TrackMetric("LoadTestDuration", duration.TotalMilliseconds);
    telemetry.TrackEvent("LoadTestCompleted");
    
    return Results.Ok(new { Message = "Load test completed", Duration = duration.TotalSeconds });
});

app.MapGet("/api/memory-test", (TelemetryClient telemetry) =>
{
    // Simulate memory usage for metrics demonstration
    var startMemory = GC.GetTotalMemory(false);
    
    // Allocate some memory
    var data = new List<byte[]>();
    for (int i = 0; i < 1000; i++)
    {
        data.Add(new byte[1024 * 100]); // 100KB each
    }
    
    var endMemory = GC.GetTotalMemory(false);
    var memoryUsed = endMemory - startMemory;
    
    telemetry.TrackMetric("MemoryAllocated", memoryUsed);
    telemetry.TrackEvent("MemoryTestCompleted", new Dictionary<string, string> 
    { 
        { "MemoryUsed", memoryUsed.ToString() } 
    });
    
    // Clean up
    data.Clear();
    GC.Collect();
    
    return Results.Ok(new { MemoryAllocated = memoryUsed, Message = "Memory test completed" });
});

app.Run();

// Product model for API
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
