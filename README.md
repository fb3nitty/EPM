# EndpointMonitor

A comprehensive .NET 9 application for monitoring various types of endpoints, including HTTP services, TCP ports, and SSL certificates. This application can run as both a console application and a Windows service.

## Project Overview

EndpointMonitor is designed to provide reliable monitoring of network endpoints with configurable tests, schedules, and notifications. It helps system administrators and DevOps teams ensure their services are running correctly and proactively detect issues before they impact users.

## Features

- **Multiple Test Types**:
  - **Port Check**: Verifies if a TCP port is open and accessible
  - **HTTP Check**: Tests HTTP/HTTPS endpoints with status code and content validation
  - **Certificate Check**: Monitors SSL certificate expiration dates
  - **SQL Server Check**: Connects to a SQL Server instance and lists available databases
  - **Redis Check**: Connects to Redis instances (including Azure Redis) and verifies connectivity

- **Flexible Configuration**:
  - JSON-based configuration via appsettings.json
  - Per-endpoint test configuration
  - Custom timeout settings

- **Advanced Scheduling**:
  - Cron-based scheduling using NCrontab
  - Individual schedules for each endpoint
  - Default fallback schedules

- **Comprehensive Logging**:
  - Structured logging with Serilog
  - Console and file output
  - Configurable log levels and retention

- **Deployment Options**:
  - Run as a console application for testing and debugging
  - Install as a Windows service for production environments

## Prerequisites

- **.NET 9 RC** or later
  - As this application uses .NET 9 RC, you'll need to install the .NET 9 SDK from the preview channel
  - [Download .NET 9 RC](https://dotnet.microsoft.com/download/dotnet/9.0)

- **Windows OS** (for Windows service functionality)
  - The application can run as a console app on any OS supported by .NET 9
  - Windows service functionality requires Windows OS

## Installation

### Installing .NET 9 RC

#### Windows
```powershell
# Download and run the .NET 9 RC installer from:
# https://dotnet.microsoft.com/download/dotnet/9.0

# Verify installation
dotnet --version
# Should show 9.0.xxx
```

#### Linux (Ubuntu/Debian)
```bash
# Add the Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install the .NET 9 SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0

# Verify installation
dotnet --version
# Should show 9.0.xxx
```

### Building the Application

1. Clone the repository
```bash
git clone https://github.com/yourusername/EndpointMonitor.git
cd EndpointMonitor
```

2. Build the application
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Publish the application
dotnet publish -c Release -o ./publish
```

## Configuration

The application is configured through the `appsettings.json` file. Here's an example configuration:

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
      },
      {
        "Name": "SQL Server Database Check",
        "Host": "localhost",
        "Port": 1433,
        "TestType": "SqlServer",
        "Timeout": 5000,
        "SqlServerDatabase": "master",
        "SqlServerUsername": "sa",
        "SqlServerPassword": "YourStrongPassword123",
        "Schedule": "*/15 * * * *"
      },
      {
        "Name": "Azure Redis Cache",
        "Host": "your-azure-redis.redis.cache.windows.net",
        "Port": 6380,
        "TestType": "Redis",
        "UseSsl": true,
        "Timeout": 5000,
        "RedisPassword": "YourAzureRedisAccessKey",
        "Schedule": "*/10 * * * *"
      },
      {
        "Name": "Local Redis Server",
        "Host": "localhost",
        "Port": 6379,
        "TestType": "Redis",
        "UseSsl": false,
        "Timeout": 2000,
        "RedisUsername": "default",
        "RedisPassword": "YourLocalRedisPassword",
        "Schedule": "*/5 * * * *"
      }
    ]
  }
}
```

### Configuration Options

#### Endpoint Configuration

| Property | Description | Default |
|----------|-------------|---------|
| `Name` | Friendly name for the endpoint | Required |
| `Host` | Hostname or IP address | Required |
| `Port` | TCP port number | Required |
| `TestType` | Type of test: "Port", "Http", "Certificate", "SqlServer", or "Redis" | Required |
| `Timeout` | Timeout in milliseconds | 5000 |
| `Schedule` | Cron expression for test schedule | "*/5 * * * *" (every 5 minutes) |

#### HTTP-Specific Properties

| Property | Description | Default |
|----------|-------------|---------|
| `Url` | Complete URL (optional, constructed from Host/Port if not provided) | null |
| `Path` | URL path | "/" |
| `UseSsl` | Whether to use HTTPS | false |
| `ExpectedStatusCode` | Expected HTTP status code | null (any success code) |
| `ExpectedContent` | String that should be present in the response | null |

#### Certificate-Specific Properties

| Property | Description | Default |
|----------|-------------|---------|
| `MinCertificateDaysValid` | Minimum days before certificate expiration | 30 |

#### SQL Server-Specific Properties

| Property | Description | Default |
|----------|-------------|---------|
| `SqlServerDatabase` | Database to connect to | "master" |
| `SqlServerUsername` | SQL Server authentication username | null (uses Windows Authentication if not specified) |
| `SqlServerPassword` | SQL Server authentication password | null |

#### Redis-Specific Properties

| Property | Description | Default |
|----------|-------------|---------|
| `RedisUsername` | Redis authentication username | null (not used for Redis versions before 6.0 or when ACL is not enabled) |
| `RedisPassword` | Redis authentication password | null |

#### Scheduler Configuration

| Property | Description | Default |
|----------|-------------|---------|
| `Enabled` | Whether scheduling is enabled | true |
| `DefaultIntervalSeconds` | Default interval if no schedule is specified | 300 |

## Usage

### Running as a Console Application

```bash
# Navigate to the publish directory
cd ./publish

# Run the application
dotnet EndpointMonitor.Worker.dll
```

The application will start monitoring the configured endpoints according to their schedules and log the results to the console and log files.

### Running as a Windows Service

#### Installing the Service

1. Create an installation script (install-service.ps1):

```powershell
# Run as Administrator
$serviceName = "EndpointMonitor"
$displayName = "Endpoint Monitoring Service"
$description = "Monitors network endpoints for availability and performance"
$exePath = Join-Path $PSScriptRoot "EndpointMonitor.Worker.exe"

# Stop and remove the service if it exists
if (Get-Service $serviceName -ErrorAction SilentlyContinue) {
    Stop-Service $serviceName
    sc.exe delete $serviceName
    Write-Host "Removed existing service: $serviceName"
}

# Create the service
New-Service -Name $serviceName `
    -BinaryPathName "$exePath --windows-service" `
    -DisplayName $displayName `
    -Description $description `
    -StartupType Automatic
Write-Host "Created service: $serviceName"

# Create log directory and set permissions
$logDir = Join-Path $env:ProgramData "EndpointMonitor\logs"
if (-not (Test-Path $logDir)) {
    New-Item -Path $logDir -ItemType Directory -Force | Out-Null
    Write-Host "Created log directory: $logDir"
}

# Grant SYSTEM and Administrators full control to the log directory
$acl = Get-Acl $logDir
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("SYSTEM", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("Administrators", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $logDir $acl
Write-Host "Set permissions on log directory"

# Start the service
Start-Service $serviceName
Write-Host "Started service: $serviceName"
```

2. Run the installation script as Administrator:

```powershell
powershell -ExecutionPolicy Bypass -File install-service.ps1
```

#### Managing the Service

```powershell
# Start the service
Start-Service EndpointMonitor

# Stop the service
Stop-Service EndpointMonitor

# Check service status
Get-Service EndpointMonitor
```

#### Alternative Installation Using sc.exe

You can also install the service directly using the Windows `sc.exe` command:

```cmd
sc.exe create EndpointMonitor binPath= "\"C:\Path\To\EndpointMonitor.Worker.exe\" --windows-service" DisplayName= "Endpoint Monitoring Service" start= auto
sc.exe description EndpointMonitor "Monitors network endpoints for availability and performance"
```

> **Important Notes:**
> 1. Always use the `.exe` file, not the `.dll` file when installing as a Windows service
> 2. Logs are stored in `%PROGRAMDATA%\EndpointMonitor\logs\` when running as a service
> 3. Ensure the service account (typically SYSTEM) has write permissions to the log directory

#### Uninstalling the Service

```powershell
# Stop and remove the service
Stop-Service EndpointMonitor
sc.exe delete EndpointMonitor
```

## Troubleshooting

### Common Issues

1. **Service fails to start**
   - Check the Windows Event Viewer for error details
   - Verify the application configuration in appsettings.json
   - Ensure the service account has necessary permissions
   - Check the startup error log at `%PROGRAMDATA%\EndpointMonitor\startup-error.log`

2. **Logging not working when running as a service**
   - Verify that the `%PROGRAMDATA%\EndpointMonitor\logs` directory exists
   - Ensure the service account (typically SYSTEM) has write permissions to this directory
   - Check if any antivirus or security software is blocking file writes
   - Review the Windows Event Viewer for any permission-related errors

3. **Endpoints always show as failed**
   - Verify network connectivity to the target hosts
   - Check firewall settings that might block connections
   - Ensure the correct ports are specified in the configuration

4. **Scheduling issues**
   - Verify that cron expressions are correctly formatted
   - Check system time and timezone settings
   - Ensure the service is running continuously

### Logging

When running as a console application, the application logs to both the console and log files in the `logs` directory relative to the application.

When running as a Windows service, logs are written to `%PROGRAMDATA%\EndpointMonitor\logs\`. This is typically `C:\ProgramData\EndpointMonitor\logs\` on most Windows systems.

Log files are named with the pattern `endpoint-monitor-YYYYMMDD.log` and are rotated daily.

To increase log verbosity, modify the Serilog configuration in appsettings.json:

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Debug",  // Change from "Information" to "Debug"
    ...
  },
  ...
}
```

## .NET 9 RC Considerations

As this application is built on .NET 9 RC (Release Candidate), there are some important considerations:

1. **Preview Status**: .NET 9 is currently in RC status and not yet recommended for production use
2. **API Changes**: APIs may change between RC and the final release
3. **Compatibility**: Some NuGet packages may not be fully compatible with .NET 9 RC
4. **Updates**: Regular updates to .NET 9 RC are recommended to stay current with bug fixes

When .NET 9 reaches general availability (GA), it's recommended to update the application to the final release version.


