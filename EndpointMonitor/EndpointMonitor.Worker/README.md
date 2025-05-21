# Endpoint Monitor

## Overview

Endpoint Monitor is a .NET 8 application designed to monitor various types of endpoints and services. It provides a flexible and configurable solution for checking the availability and health of network services, web applications, and SSL certificates. The application can run as both a console application and a Windows service, making it suitable for server environments.

## Features

- **Multiple Test Types**:
  - **Port Check**: Verifies if a specific port on a host is open and accessible
  - **HTTP Check**: Tests HTTP/HTTPS endpoints with customizable expectations for status codes and response content
  - **Certificate Check**: Monitors SSL certificates for expiration dates and validity

- **Flexible Configuration**:
  - JSON-based configuration via appsettings.json
  - Customizable test parameters for each endpoint
  - Support for different timeouts and test frequencies

- **Scheduling**:
  - Cron-based scheduling using NCrontab
  - Individual schedules for each endpoint
  - Default scheduling options with override capabilities

- **Comprehensive Logging**:
  - Structured logging with Serilog
  - Console and file logging
  - Configurable log levels and retention policies

- **Service Integration**:
  - Can run as a Windows service
  - Easy installation and management
  - Automatic startup with Windows

## Installation

### Prerequisites

- .NET 8 SDK or Runtime
- Windows OS (for Windows Service functionality)

### Setup

1. Clone or download the repository
2. Navigate to the project directory
3. Build the application:

```bash
dotnet build --configuration Release
```

4. Publish the application:

```bash
dotnet publish --configuration Release --output ./publish
```

## Configuration

The application is configured through the `appsettings.json` file. Below is an example configuration with explanations:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/endpoint-monitor-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ]
  },
  "MonitorConfig": {
    "Scheduler": {
      "Enabled": true,
      "DefaultIntervalSeconds": 300
    },
    "Endpoints": [
      {
        "Name": "Google HTTP",
        "Host": "www.google.com",
        "Port": 443,
        "TestType": "Http",
        "UseSsl": true,
        "Timeout": 5000,
        "Schedule": "*/5 * * * *"
      },
      {
        "Name": "Google HTTPS Certificate",
        "Host": "www.google.com",
        "Port": 443,
        "TestType": "Certificate",
        "Timeout": 5000,
        "MinCertificateDaysValid": 30,
        "Schedule": "0 */6 * * *"
      },
      {
        "Name": "Local SQL Server",
        "Host": "localhost",
        "Port": 1433,
        "TestType": "Port",
        "Timeout": 2000,
        "Schedule": "*/15 * * * *"
      },
      {
        "Name": "Example API",
        "Url": "https://jsonplaceholder.typicode.com/posts/1",
        "Host": "jsonplaceholder.typicode.com",
        "Port": 443,
        "TestType": "Http",
        "UseSsl": true,
        "ExpectedStatusCode": 200,
        "ExpectedContent": "\"userId\"",
        "Timeout": 10000,
        "Schedule": "*/10 * * * *"
      }
    ]
  }
}
```

### Configuration Sections

#### Logging and Serilog

The `Logging` and `Serilog` sections control the application's logging behavior. Serilog is configured to write logs to both the console and daily rolling files in the `logs` directory.

#### MonitorConfig

The `MonitorConfig` section contains the core configuration for the monitoring functionality:

- **Scheduler**: Controls the global scheduling behavior
  - `Enabled`: Enables or disables the scheduler
  - `DefaultIntervalSeconds`: Default interval between checks if not specified by individual endpoints

- **Endpoints**: Array of endpoint configurations
  - `Name`: Friendly name for the endpoint
  - `Host`: Hostname or IP address to monitor
  - `Port`: Port number to check
  - `TestType`: Type of test to perform (`Port`, `Http`, or `Certificate`)
  - `Timeout`: Timeout in milliseconds
  - `Schedule`: Cron expression for scheduling (e.g., `*/5 * * * *` for every 5 minutes)

##### HTTP-Specific Properties

- `Url`: Full URL to check (optional, can be constructed from Host, Port, and Path)
- `Path`: Path component of the URL (optional)
- `UseSsl`: Whether to use HTTPS
- `ExpectedStatusCode`: Expected HTTP status code
- `ExpectedContent`: String that should be present in the response

##### Certificate-Specific Properties

- `MinCertificateDaysValid`: Minimum number of days the certificate should be valid

### Cron Expressions

The application uses NCrontab for scheduling. Here are some example cron expressions:

- `*/5 * * * *`: Every 5 minutes
- `0 */1 * * *`: Every hour at minute 0
- `0 0 * * *`: Once a day at midnight
- `0 8-17 * * 1-5`: Every hour from 8 AM to 5 PM, Monday to Friday

## Usage

### Running as a Console Application

To run the application as a console application:

```bash
cd ./publish
dotnet EndpointMonitor.Worker.dll
```

The application will start and begin monitoring endpoints according to the configuration.

### Running as a Windows Service

#### Installing the Service

1. Open an administrative command prompt
2. Navigate to the publish directory
3. Install the service using the SC command:

```cmd
sc create EndpointMonitor binPath= "C:\path\to\publish\EndpointMonitor.Worker.exe" DisplayName= "Endpoint Monitor Service" start= auto
sc description EndpointMonitor "Monitors network endpoints and services for availability and health"
```

#### Starting the Service

```cmd
sc start EndpointMonitor
```

#### Stopping the Service

```cmd
sc stop EndpointMonitor
```

#### Uninstalling the Service

```cmd
sc delete EndpointMonitor
```

## Troubleshooting

### Common Issues

#### Service Won't Start

1. Check the Windows Event Viewer for error messages
2. Verify the service account has appropriate permissions
3. Ensure the path in the `binPath` is correct

#### Endpoints Not Being Checked

1. Verify the cron expressions in the configuration
2. Check that the `Scheduler.Enabled` setting is `true`
3. Look for any error messages in the logs

#### HTTP Checks Failing

1. Verify network connectivity to the target
2. Check if the expected status code matches the actual response
3. Ensure the expected content pattern is correct

#### Certificate Checks Failing

1. Verify the certificate is accessible
2. Check if the certificate is actually expiring soon
3. Adjust the `MinCertificateDaysValid` setting if needed

### Logging

The application logs detailed information about its operation. Logs are written to:

- Console (when running as a console application)
- Log files in the `logs` directory

Check these logs for information about test results and any errors that occur.

## Advanced Configuration

### Custom HTTP Headers

To add custom headers to HTTP requests, modify the `HttpChecker.cs` file to include the required headers.

### Notification Integration

The application can be extended to send notifications when tests fail. Consider implementing:

- Email notifications
- SMS alerts
- Integration with monitoring platforms

### Load Balancing

For high-availability scenarios, consider running multiple instances of the application on different servers and monitoring different subsets of endpoints.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
