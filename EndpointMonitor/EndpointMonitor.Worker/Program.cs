using System;
using System.Net.Http;
using EndpointMonitor.Worker;
using EndpointMonitor.Worker.EndpointTests;
using EndpointMonitor.Worker.Models;
using EndpointMonitor.Worker.Schedulers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting EndpointMonitor");
    
    IHost host = Host.CreateDefaultBuilder(args)
        .UseWindowsService() // Enables running as a Windows service
        .UseSerilog() // Use Serilog for logging
        .ConfigureServices((hostContext, services) =>
        {
            // Register configuration
            services.Configure<MonitorConfig>(
                hostContext.Configuration.GetSection("MonitorConfig"));

            // Register HTTP client factory
            services.AddHttpClient();

            // Register endpoint testers
            services.AddSingleton<IEndpointTester, PortChecker>();
            services.AddSingleton<IEndpointTester, HttpChecker>();
            services.AddSingleton<IEndpointTester, CertificateChecker>();

            // Register scheduler
            services.AddSingleton<SimpleScheduler>();

            // Register worker
            services.AddHostedService<Worker>();
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EndpointMonitor terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
