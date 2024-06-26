using Infrastructure.Data.Context;
using Infrastructure.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddDbContextFactory<DataContext>(options =>
        {
            options.UseSqlServer(Environment.GetEnvironmentVariable("SQLDATABASE"));

        });
        services.AddScoped<ITokenService, TokenService>();
    })
    .Build();

host.Run();
