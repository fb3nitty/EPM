using EndpointMonitor.Worker;
using EndpointMonitor.Worker.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Serilog;
using System.Diagnostics;
using Microsoft.Extensions.Hosting.WindowsServices;
using System.IO;
using System.Reflection;

try
{
    // Add startup diagnostic logging to console
    Console.WriteLine($"EndpointMonitor starting at: {DateTime.UtcNow}");
    Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");
    Console.WriteLine($"Application directory: {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}");
    
    // Write a test file to verify file system permissions
    try
    {
        string testFilePath = Path.Combine(Environment.CurrentDirectory, "test-write-permissions.txt");
        File.WriteAllText(testFilePath, $"Test file written at {DateTime.UtcNow}");
        Console.WriteLine($"Test file successfully written to: {testFilePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to write test file: {ex.Message}");
    }
    
    // Determine log directory - use ProgramData for Windows service
    string logDirectory;
    if (WindowsServiceHelpers.IsWindowsService())
    {
        // Use ProgramData for Windows service
        logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EndpointMonitor", 
            "logs");
    }
    else
    {
        // Use relative path for console app
        logDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory,
            "logs");
    }
    
    // Ensure log directory exists
    Directory.CreateDirectory(logDirectory);
    
    string logFilePath = Path.Combine(logDirectory, "endpoint-monitor-.log");
    
    // Print log directory information for easier troubleshooting
    Console.WriteLine($"Log directory: {logDirectory}");
    Console.WriteLine($"Log directory exists: {Directory.Exists(logDirectory)}");
    Console.WriteLine($"Log directory permissions: {new DirectoryInfo(logDirectory).Attributes}");
    Console.WriteLine($"Log file path: {logFilePath}");

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
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, shared: true, flushToDiskInterval: TimeSpan.FromSeconds(1));
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
        services.AddTransient<EndpointMonitor.Worker.EndpointTests.IEndpointTester, EndpointMonitor.Worker.EndpointTests.SqlServerChecker>();
        services.AddTransient<EndpointMonitor.Worker.EndpointTests.IEndpointTester, EndpointMonitor.Worker.EndpointTests.RedisChecker>();
        
        // Register scheduler
        services.AddSingleton<EndpointMonitor.Worker.Schedulers.SimpleScheduler>();
    })
    .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    // Log startup exceptions to both console and a fallback file
    Console.WriteLine($"Fatal error during startup: {ex}");
    
    try
    {
        // Try to write to a fallback log file in case the main logging isn't working
        string fallbackLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EndpointMonitor", 
            "startup-error.log");
            
        Directory.CreateDirectory(Path.GetDirectoryName(fallbackLogPath));
        
        File.AppendAllText(
            fallbackLogPath, 
            $"{DateTime.UtcNow}: FATAL ERROR: {ex}{Environment.NewLine}");
    }
    catch
    {
        // Last resort - can't even write to fallback log
    }
    
    // Re-throw to terminate the application with error
    throw;
}
