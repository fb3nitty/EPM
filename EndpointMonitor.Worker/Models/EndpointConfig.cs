
using System;
using System.Collections.Generic;

namespace EndpointMonitor.Worker.Models
{
    public class EndpointConfig
    {
        public required string Name { get; set; }
        public required string Host { get; set; }
        public int Port { get; set; }
        public required string TestType { get; set; } // Port, Http, Certificate, SqlServer, Redis
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
        
        // SQL Server specific properties
        public string SqlServerDatabase { get; set; } = "master"; // Default database to connect to
        public string? SqlServerUsername { get; set; } // SQL Server authentication username
        public string? SqlServerPassword { get; set; } // SQL Server authentication password
        
        // Redis specific properties
        public string? RedisUsername { get; set; } // Redis authentication username (for Redis 6+ with ACL)
        public string? RedisPassword { get; set; } // Redis authentication password
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
