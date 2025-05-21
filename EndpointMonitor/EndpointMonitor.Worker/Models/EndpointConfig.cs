
using System;
using System.Collections.Generic;

namespace EndpointMonitor.Worker.Models
{
    public class EndpointConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string TestType { get; set; } = string.Empty; // Port, Http, Certificate
        public int Timeout { get; set; } = 5000; // Default timeout in milliseconds
        public string Schedule { get; set; } = "*/5 * * * *"; // Default: every 5 minutes
        
        // HTTP specific properties
        public string? Url { get; set; }
        public string? Path { get; set; }
        public bool UseSsl { get; set; }
        public int? ExpectedStatusCode { get; set; }
        public string? ExpectedContent { get; set; }
        
        // Certificate specific properties
        public int MinCertificateDaysValid { get; set; } = 30; // Default: 30 days
    }

    public class MonitorConfig
    {
        public List<EndpointConfig> Endpoints { get; set; } = new List<EndpointConfig>();
        public SchedulerConfig Scheduler { get; set; } = new SchedulerConfig();
    }

    public class SchedulerConfig
    {
        public bool Enabled { get; set; } = true;
        public int DefaultIntervalSeconds { get; set; } = 300; // Default: 5 minutes
    }
}
