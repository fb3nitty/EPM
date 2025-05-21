using EndpointMonitor.Worker;
using EndpointMonitor.Worker.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Serilog;
using System.Diagnostics;
using Microsoft.Extensions.Hosting.WindowsServices;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "EndpointMonitor";
    })
    .UseSerilog((hostingContext, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(hostingContext.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/endpoint-monitor-.log", rollingInterval: RollingInterval.Day);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Worker>();
        
        // Configure monitor settings
        services.Configure<MonitorConfig>(hostContext.Configuration.GetSection("MonitorConfig"));
        
        // Register HttpClient
        services.AddHttpClient();
        
        // Register endpoint testers
        services.AddTransient<EndpointMonitor.Worker.EndpointTests.IEndpointTester, EndpointMonitor.Worker.EndpointTests.PortChecker>();
        services.AddTransient<EndpointMonitor.Worker.EndpointTests.IEndpointTester, EndpointMonitor.Worker.EndpointTests.HttpChecker>();
        services.AddTransient<EndpointMonitor.Worker.EndpointTests.IEndpointTester, EndpointMonitor.Worker.EndpointTests.CertificateChecker>();
        
        // Register scheduler
        services.AddSingleton<EndpointMonitor.Worker.Schedulers.SimpleScheduler>();
    })
    .Build();

await host.RunAsync();
