using EndpointMonitor.Worker.EndpointTests;
using EndpointMonitor.Worker.Models;
using EndpointMonitor.Worker.Schedulers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EndpointMonitor.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEnumerable<IEndpointTester> _testers;
    private readonly SimpleScheduler _scheduler;

    public Worker(
        ILogger<Worker> logger,
        IConfiguration configuration,
        IEnumerable<IEndpointTester> testers,
        SimpleScheduler scheduler)
    {
        _logger = logger;
        _configuration = configuration;
        _testers = testers;
        _scheduler = scheduler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EndpointMonitor Worker service starting at: {time}", DateTimeOffset.Now);

        try
        {
            // Start monitoring loop
            while (!stoppingToken.IsCancellationRequested)
            {
                await _scheduler.ExecuteScheduledTestsAsync(stoppingToken);
                await Task.Delay(1000, stoppingToken); // Check every second for new tests to run
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in worker service");
        }
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EndpointMonitor Worker service starting");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EndpointMonitor Worker service stopping");
        return base.StopAsync(cancellationToken);
    }
}
