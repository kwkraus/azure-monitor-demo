using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddHttpClient();
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddScoped<LoadTestFunction.LoadGenerator>();
    })
    .Build();

host.Run();
