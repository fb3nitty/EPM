
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EndpointMonitor.Worker.EndpointTests;
using EndpointMonitor.Worker.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCrontab;

namespace EndpointMonitor.Worker.Schedulers
{
    public class SimpleScheduler
    {
        private readonly ILogger<SimpleScheduler> _logger;
        private readonly IEnumerable<IEndpointTester> _testers;
        private readonly MonitorConfig _config;
        private readonly Dictionary<string, DateTime> _lastRunTimes = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, CrontabSchedule> _schedules = new Dictionary<string, CrontabSchedule>();

        public SimpleScheduler(
            ILogger<SimpleScheduler> logger,
            IEnumerable<IEndpointTester> testers,
            IOptions<MonitorConfig> config)
        {
            _logger = logger;
            _testers = testers;
            _config = config.Value;
            
            // Parse all cron schedules
            foreach (var endpoint in _config.Endpoints)
            {
                try
                {
                    _schedules[endpoint.Name] = CrontabSchedule.Parse(endpoint.Schedule);
                    _lastRunTimes[endpoint.Name] = DateTime.MinValue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Invalid cron schedule '{Schedule}' for endpoint {Name}", endpoint.Schedule, endpoint.Name);
                }
            }
        }

        public async Task ExecuteScheduledTestsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Checking for scheduled tests to run");
            
            var now = DateTime.Now;
            var tasksToRun = new List<Task<TestResult>>();
            
            foreach (var endpoint in _config.Endpoints)
            {
                if (!_schedules.TryGetValue(endpoint.Name, out var schedule))
                {
                    continue;
                }
                
                if (!_lastRunTimes.TryGetValue(endpoint.Name, out var lastRun))
                {
                    lastRun = DateTime.MinValue;
                }
                
                var nextRun = schedule.GetNextOccurrence(lastRun);
                
                if (nextRun <= now)
                {
                    _logger.LogInformation("Scheduling test for endpoint {Name}", endpoint.Name);
                    _lastRunTimes[endpoint.Name] = now;
                    
                    var tester = _testers.FirstOrDefault(t => t.CanHandle(endpoint));
                    
                    if (tester != null)
                    {
                        tasksToRun.Add(tester.TestEndpointAsync(endpoint));
                    }
                    else
                    {
                        _logger.LogWarning("No tester found for endpoint {Name} with test type {TestType}", 
                            endpoint.Name, endpoint.TestType);
                    }
                }
            }
            
            if (tasksToRun.Count > 0)
            {
                _logger.LogInformation("Running {Count} scheduled tests", tasksToRun.Count);
                var results = await Task.WhenAll(tasksToRun);
                
                foreach (var result in results)
                {
                    LogTestResult(result);
                }
            }
        }
        
        private void LogTestResult(TestResult result)
        {
            var logLevel = result.Success ? LogLevel.Information : LogLevel.Warning;
            
            _logger.Log(logLevel, 
                "Test {TestType} for {Name} ({Host}:{Port}): {Status} in {Duration:N0}ms - {Message}",
                result.Endpoint.TestType,
                result.Endpoint.Name,
                result.Endpoint.Host,
                result.Endpoint.Port,
                result.Success ? "SUCCESS" : "FAILED",
                result.ResponseTime.TotalMilliseconds,
                result.Message);
        }
    }
}
