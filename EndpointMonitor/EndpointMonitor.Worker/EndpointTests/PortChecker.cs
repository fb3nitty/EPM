
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using EndpointMonitor.Worker.Models;
using Microsoft.Extensions.Logging;

namespace EndpointMonitor.Worker.EndpointTests
{
    public class PortChecker : IEndpointTester
    {
        private readonly ILogger<PortChecker> _logger;

        public PortChecker(ILogger<PortChecker> logger)
        {
            _logger = logger;
        }

        public bool CanHandle(EndpointConfig endpoint)
        {
            return endpoint.TestType.Equals("Port", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<TestResult> TestEndpointAsync(EndpointConfig endpoint)
        {
            _logger.LogInformation("Testing port connectivity to {Endpoint}:{Port}", endpoint.Host, endpoint.Port);
            
            var stopwatch = Stopwatch.StartNew();
            bool success = false;
            string message = string.Empty;

            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(endpoint.Host, endpoint.Port);
                    var timeoutTask = Task.Delay(endpoint.Timeout);
                    
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    
                    if (completedTask == connectTask)
                    {
                        // Connection succeeded
                        success = true;
                        message = $"Successfully connected to {endpoint.Host}:{endpoint.Port}";
                    }
                    else
                    {
                        // Connection timed out
                        success = false;
                        message = $"Connection to {endpoint.Host}:{endpoint.Port} timed out after {endpoint.Timeout}ms";
                    }
                }
            }
            catch (SocketException ex)
            {
                success = false;
                message = $"Failed to connect to {endpoint.Host}:{endpoint.Port}. Error: {ex.Message}";
                _logger.LogError(ex, message);
            }
            catch (Exception ex)
            {
                success = false;
                message = $"Unexpected error testing {endpoint.Host}:{endpoint.Port}. Error: {ex.Message}";
                _logger.LogError(ex, message);
            }
            
            stopwatch.Stop();
            
            return new TestResult(success, message, stopwatch.Elapsed, endpoint);
        }
    }
}
