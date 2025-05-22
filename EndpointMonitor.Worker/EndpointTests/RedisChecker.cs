using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EndpointMonitor.Worker.Models;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EndpointMonitor.Worker.EndpointTests
{
    public class RedisChecker : IEndpointTester
    {
        private readonly ILogger<RedisChecker> _logger;

        public RedisChecker(ILogger<RedisChecker> logger)
        {
            _logger = logger;
        }

        public bool CanTest(EndpointConfig endpoint)
        {
            return endpoint.TestType.Equals("Redis", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<TestResult> TestEndpointAsync(EndpointConfig endpoint)
        {
            _logger.LogInformation("Testing Redis connectivity to {Host}:{Port}", endpoint.Host, endpoint.Port);
            
            var stopwatch = Stopwatch.StartNew();
            bool success = false;
            string message = string.Empty;

            try
            {
                // Build Redis connection configuration
                var options = new ConfigurationOptions
                {
                    EndPoints = { $"{endpoint.Host}:{endpoint.Port}" },
                    ConnectTimeout = endpoint.Timeout,
                    SyncTimeout = endpoint.Timeout,
                    AbortOnConnectFail = false
                };

                // Add authentication if provided
                if (!string.IsNullOrEmpty(endpoint.RedisUsername))
                {
                    options.User = endpoint.RedisUsername;
                }

                if (!string.IsNullOrEmpty(endpoint.RedisPassword))
                {
                    options.Password = endpoint.RedisPassword;
                }

                // Add SSL if enabled
                if (endpoint.UseSsl)
                {
                    options.Ssl = true;
                    options.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                }

                // Connect to Redis
                using (var connection = await ConnectionMultiplexer.ConnectAsync(options))
                {
                    if (connection.IsConnected)
                    {
                        var db = connection.GetDatabase();
                        
                        // Perform a simple PING operation to verify connectivity
                        var pingResult = await db.PingAsync();
                        
                        success = true;
                        message = $"Successfully connected to Redis at {endpoint.Host}:{endpoint.Port}. Ping response time: {pingResult.TotalMilliseconds}ms";
                        _logger.LogInformation(message);
                    }
                    else
                    {
                        success = false;
                        message = $"Failed to connect to Redis at {endpoint.Host}:{endpoint.Port}. Connection not established.";
                        _logger.LogWarning(message);
                    }
                }
            }
            catch (RedisConnectionException ex)
            {
                success = false;
                message = $"Redis connection to {endpoint.Host}:{endpoint.Port} failed. Error: {ex.Message}";
                _logger.LogError(ex, message);
            }
            catch (RedisTimeoutException ex)
            {
                success = false;
                message = $"Redis connection to {endpoint.Host}:{endpoint.Port} timed out after {endpoint.Timeout}ms. Error: {ex.Message}";
                _logger.LogWarning(ex, message);
            }
            catch (Exception ex)
            {
                success = false;
                message = $"Unexpected error testing Redis at {endpoint.Host}:{endpoint.Port}. Error: {ex.Message}";
                _logger.LogError(ex, message);
            }
            
            stopwatch.Stop();
            
            return new TestResult(success, message, stopwatch.Elapsed, endpoint);
        }
    }
}
