using EndpointMonitor.Worker.Models;
using EndpointMonitor.Worker.Schedulers;
using Microsoft.Extensions.Options;

namespace EndpointMonitor.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly SimpleScheduler _scheduler;
    private readonly MonitorConfig _config;

    public Worker(
        ILogger<Worker> logger,
        SimpleScheduler scheduler,
        IOptions<MonitorConfig> config)
    {
        _logger = logger;
        _scheduler = scheduler;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EndpointMonitor service started at: {time}", DateTimeOffset.Now);
        
        try
        {
            // Log the configured endpoints
            _logger.LogInformation("Monitoring {count} endpoints:", _config.Endpoints.Count);
            foreach (var endpoint in _config.Endpoints)
            {
                _logger.LogInformation("  - {name}: {type} test for {host}:{port} (Schedule: {schedule})",
                    endpoint.Name, endpoint.TestType, endpoint.Host, endpoint.Port, endpoint.Schedule);
            }

            // Main service loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Run scheduled tests
                    await _scheduler.ExecuteScheduledTestsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing scheduled tests");
                }

                // Wait before checking again (default: 30 seconds)
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in worker service");
            throw;
        }
        finally
        {
            _logger.LogInformation("EndpointMonitor service stopping at: {time}", DateTimeOffset.Now);
        }
    }
}
