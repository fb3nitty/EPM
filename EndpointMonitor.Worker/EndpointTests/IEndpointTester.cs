
using System;
using System.Threading.Tasks;
using EndpointMonitor.Worker.Models;

namespace EndpointMonitor.Worker.EndpointTests
{
    public interface IEndpointTester
    {
        Task<TestResult> TestEndpointAsync(EndpointConfig endpoint);
        bool CanTest(EndpointConfig endpoint);
    }

    public class TestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public DateTime TestTime { get; set; } = DateTime.UtcNow;
        public EndpointConfig Endpoint { get; set; }
        
        public TestResult(bool success, string message, TimeSpan responseTime, EndpointConfig endpoint)
        {
            Success = success;
            Message = message;
            ResponseTime = responseTime;
            Endpoint = endpoint;
        }
    }
}
