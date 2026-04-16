using DemoMonitorApp.Data;
using DemoMonitorApp.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ProductCatalogInitializer>();
builder.Services.AddDbContextFactory<DemoMonitorDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is required for product endpoints. " +
            "In Azure this is provided by the App Service connection strings. " +
            $"The current environment is '{environment.EnvironmentName}'. " +
            "For local development, run with ASPNETCORE_ENVIRONMENT=Development and set ConnectionStrings__DefaultConnection " +
            "to an Azure SQL connection string that uses Microsoft Entra authentication.");
    }

    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        sqlServerOptions.CommandTimeout(30);
        sqlServerOptions.EnableRetryOnFailure();
    });
});

var app = builder.Build();

await InitializeDatabaseAsync(app);

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/api/health", (TelemetryClient telemetry) =>
{
    telemetry.TrackEvent("HealthCheck", new Dictionary<string, string> { { "Status", "Healthy" } });
    return Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
});

app.MapGet("/api/products", async (
    IDbContextFactory<DemoMonitorDbContext> dbContextFactory,
    TelemetryClient telemetry,
    CancellationToken cancellationToken) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var products = await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Id)
            .ToListAsync(cancellationToken);

        stopwatch.Stop();
        TrackSqlDependency(telemetry, "GetProducts", "Products", stopwatch.Elapsed, true);
        telemetry.TrackMetric("ProductCount", products.Count);

        return Results.Ok(products);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        TrackSqlDependency(telemetry, "GetProducts", "Products", stopwatch.Elapsed, false);
        telemetry.TrackException(ex);
        return Results.Problem("Error retrieving products");
    }
});

app.MapGet("/api/products/{id:int}", async (
    int id,
    IDbContextFactory<DemoMonitorDbContext> dbContextFactory,
    TelemetryClient telemetry,
    CancellationToken cancellationToken) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var product = await dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        stopwatch.Stop();
        TrackSqlDependency(telemetry, "GetProductById", "Products", stopwatch.Elapsed, true);

        return product is null ? Results.NotFound() : Results.Ok(product);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        TrackSqlDependency(telemetry, "GetProductById", "Products", stopwatch.Elapsed, false);
        telemetry.TrackException(ex);
        return Results.Problem("Error retrieving product");
    }
});

app.MapPost("/api/products", async (
    CreateProductRequest request,
    IDbContextFactory<DemoMonitorDbContext> dbContextFactory,
    TelemetryClient telemetry,
    CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateProductRequest(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var product = new Product
    {
        Name = request.Name.Trim(),
        Price = decimal.Round(request.Price, 2, MidpointRounding.AwayFromZero),
        Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
        CreatedAt = DateTime.UtcNow
    };

    try
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();
        TrackSqlDependency(telemetry, "CreateProduct", "Products", stopwatch.Elapsed, true);
        telemetry.TrackEvent("ProductCreated", new Dictionary<string, string> { { "ProductName", product.Name } });
        telemetry.TrackMetric("ProductCreated", 1);

        return Results.Created($"/api/products/{product.Id}", product);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        TrackSqlDependency(telemetry, "CreateProduct", "Products", stopwatch.Elapsed, false);
        telemetry.TrackException(ex);
        return Results.Problem("Error creating product");
    }
});

app.MapGet("/api/simulate-error", (TelemetryClient telemetry) =>
{
    if (Random.Shared.Next(1, 100) <= 30)
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
    var startTime = DateTime.UtcNow;
    var tasks = new List<Task>();

    for (var i = 0; i < 10; i++)
    {
        tasks.Add(Task.Run(() =>
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < 2000)
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
    var startMemory = GC.GetTotalMemory(false);

    var data = new List<byte[]>();
    for (var i = 0; i < 1000; i++)
    {
        data.Add(new byte[1024 * 100]);
    }

    var endMemory = GC.GetTotalMemory(false);
    var memoryUsed = endMemory - startMemory;

    telemetry.TrackMetric("MemoryAllocated", memoryUsed);
    telemetry.TrackEvent("MemoryTestCompleted", new Dictionary<string, string>
    {
        { "MemoryUsed", memoryUsed.ToString() }
    });

    data.Clear();
    GC.Collect();

    return Results.Ok(new { MemoryAllocated = memoryUsed, Message = "Memory test completed" });
});

app.Run();

static void TrackSqlDependency(TelemetryClient telemetry, string name, string target, TimeSpan duration, bool success)
{
    telemetry.TrackDependency(new DependencyTelemetry
    {
        Type = "Azure SQL",
        Name = name,
        Target = target,
        Timestamp = DateTimeOffset.UtcNow.Subtract(duration),
        Duration = duration,
        Success = success
    });
}

static Dictionary<string, string[]> ValidateProductRequest(CreateProductRequest request)
{
    var validationErrors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        validationErrors["name"] = ["Name is required."];
    }
    else if (request.Name.Trim().Length > Product.MaxNameLength)
    {
        validationErrors["name"] = [$"Name must be {Product.MaxNameLength} characters or fewer."];
    }

    if (request.Price <= 0)
    {
        validationErrors["price"] = ["Price must be greater than zero."];
    }

    if (!string.IsNullOrWhiteSpace(request.Description) &&
        request.Description.Trim().Length > Product.MaxDescriptionLength)
    {
        validationErrors["description"] = [$"Description must be {Product.MaxDescriptionLength} characters or fewer."];
    }

    return validationErrors;
}

static async Task InitializeDatabaseAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var initializer = scope.ServiceProvider.GetRequiredService<ProductCatalogInitializer>();
    await initializer.EnsureInitializedAsync(CancellationToken.None);
}

internal sealed record CreateProductRequest(string Name, decimal Price, string? Description);
