# EndpointMonitor Logging Guide

## Logging Configuration

The EndpointMonitor application uses Serilog for logging with the following sinks:
- Console output
- File output with daily rolling

## Log File Locations

Logs are stored in different locations depending on how the application is run:

1. **Console Application Mode**:
   - Path: `[Application Directory]/logs/endpoint-monitor-YYYYMMDD.log`
   - Example: `/home/ubuntu/EndpointMonitor/EndpointMonitor.Worker/bin/Debug/net9.0/logs/endpoint-monitor-20250522.log`

2. **Windows Service Mode**:
   - Path: `%PROGRAMDATA%/EndpointMonitor/logs/endpoint-monitor-YYYYMMDD.log`
   - Example: `C:\ProgramData\EndpointMonitor\logs\endpoint-monitor-20250522.log`

## Recent Changes to Logging

The following changes were made to fix logging issues:

1. **Fixed path inconsistency**:
   - Updated appsettings.json to use a relative path (`logs/endpoint-monitor-.log`) instead of hardcoded Windows path
   - This ensures consistency between the path in Program.cs and appsettings.json

2. **Improved log flushing**:
   - Added `shared: true` and `flushToDiskInterval: TimeSpan.FromSeconds(1)` parameters to ensure logs are written immediately
   - This prevents logs from being buffered and potentially lost

3. **Enhanced diagnostic information**:
   - Added additional console output to show log directory information
   - Added directory permission checks to help diagnose permission issues

## Checking Logs

A utility script has been provided to help check logs:

```bash
# Run from the EndpointMonitor directory
./check-logs.sh
```

This script will:
- Check both possible log locations
- Display information about found log files
- Show the most recent log entries
- Display directory permissions

## Troubleshooting

If logs are not being created:

1. **Check permissions**:
   - Ensure the application has write permissions to the log directory
   - For Windows services, ensure the service account has access to %PROGRAMDATA%

2. **Check disk space**:
   - Insufficient disk space can prevent log files from being created

3. **Check configuration**:
   - Verify that the log path in appsettings.json matches the expected location
   - Ensure Serilog is properly configured in Program.cs

4. **Check for errors**:
   - Look for startup errors in the console output
   - Check the Windows Event Log for service-related errors

## Manual Log Viewing

To view logs manually:

```bash
# View entire log file
cat /path/to/logfile | less

# View last 100 lines
tail -n 100 /path/to/logfile

# Follow log in real-time
tail -f /path/to/logfile
```
