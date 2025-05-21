
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using EndpointMonitor.Worker.Models;
using Microsoft.Extensions.Logging;

namespace EndpointMonitor.Worker.EndpointTests
{
    public class HttpChecker : IEndpointTester
    {
        private readonly ILogger<HttpChecker> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpChecker(ILogger<HttpChecker> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public bool CanHandle(EndpointConfig endpoint)
        {
            return endpoint.TestType.Equals("Http", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<TestResult> TestEndpointAsync(EndpointConfig endpoint)
        {
            string url = endpoint.Url ?? string.Empty;
            if (string.IsNullOrEmpty(url))
            {
                url = $"http{(endpoint.UseSsl ? "s" : "")}://{endpoint.Host}";
                if (endpoint.Port != 80 && endpoint.Port != 443)
                {
                    url += $":{endpoint.Port}";
                }
                url += endpoint.Path ?? "/";
            }

            _logger.LogInformation("Testing HTTP connectivity to {Url}", url);
            
            var stopwatch = Stopwatch.StartNew();
            bool success = false;
            string message = string.Empty;

            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromMilliseconds(endpoint.Timeout);
                
                var response = await httpClient.GetAsync(url);
                
                // Check if status code is in expected range
                if (endpoint.ExpectedStatusCode.HasValue)
                {
                    success = (int)response.StatusCode == endpoint.ExpectedStatusCode.Value;
                    message = success 
                        ? $"HTTP request to {url} returned expected status code {response.StatusCode}"
                        : $"HTTP request to {url} returned status code {response.StatusCode}, expected {endpoint.ExpectedStatusCode.Value}";
                }
                else
                {
                    success = response.IsSuccessStatusCode;
                    message = success 
                        ? $"HTTP request to {url} succeeded with status code {response.StatusCode}"
                        : $"HTTP request to {url} failed with status code {response.StatusCode}";
                }
                
                // Check for expected content if specified
                if (success && !string.IsNullOrEmpty(endpoint.ExpectedContent))
                {
                    string content = await response.Content.ReadAsStringAsync();
                    if (!content.Contains(endpoint.ExpectedContent))
                    {
                        success = false;
                        message = $"HTTP response from {url} did not contain expected content";
                    }
                }
            }
            catch (TaskCanceledException)
            {
                success = false;
                message = $"HTTP request to {url} timed out after {endpoint.Timeout}ms";
                _logger.LogWarning(message);
            }
            catch (HttpRequestException ex)
            {
                success = false;
                message = $"HTTP request to {url} failed. Error: {ex.Message}";
                _logger.LogError(ex, message);
            }
            catch (Exception ex)
            {
                success = false;
                message = $"Unexpected error testing {url}. Error: {ex.Message}";
                _logger.LogError(ex, message);
            }
            
            stopwatch.Stop();
            
            return new TestResult(success, message, stopwatch.Elapsed, endpoint);
        }
    }
}
