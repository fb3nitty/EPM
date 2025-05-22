#!/bin/bash

# Script to check EndpointMonitor logs
# Created on: May 22, 2025

echo "EndpointMonitor Log Checker"
echo "=========================="
echo

# Define log paths
CONSOLE_LOG_PATH="./EndpointMonitor.Worker/bin/Debug/net9.0/logs"
WINDOWS_LOG_PATH="/ProgramData/EndpointMonitor/logs"

echo "Checking for logs in console app path..."
if [ -d "$CONSOLE_LOG_PATH" ]; then
    echo "✅ Found logs directory at: $CONSOLE_LOG_PATH"
    LOG_FILES=$(find "$CONSOLE_LOG_PATH" -name "endpoint-monitor-*.log" | sort)
    
    if [ -n "$LOG_FILES" ]; then
        echo "✅ Found log files:"
        for file in $LOG_FILES; do
            echo "   - $file ($(du -h "$file" | cut -f1) - last modified: $(stat -c %y "$file"))"
        done
        
        # Show the most recent log file
        LATEST_LOG=$(echo "$LOG_FILES" | tail -n 1)
        echo
        echo "Most recent log file: $LATEST_LOG"
        echo "Last 10 log entries:"
        echo "--------------------"
        tail -n 10 "$LATEST_LOG"
    else
        echo "❌ No log files found in $CONSOLE_LOG_PATH"
    fi
else
    echo "❌ Console app logs directory not found at: $CONSOLE_LOG_PATH"
fi

echo
echo "Checking for logs in Windows service path..."
if [ -d "$WINDOWS_LOG_PATH" ]; then
    echo "✅ Found logs directory at: $WINDOWS_LOG_PATH"
    LOG_FILES=$(find "$WINDOWS_LOG_PATH" -name "endpoint-monitor-*.log" | sort)
    
    if [ -n "$LOG_FILES" ]; then
        echo "✅ Found log files:"
        for file in $LOG_FILES; do
            echo "   - $file ($(du -h "$file" | cut -f1) - last modified: $(stat -c %y "$file"))"
        done
        
        # Show the most recent log file
        LATEST_LOG=$(echo "$LOG_FILES" | tail -n 1)
        echo
        echo "Most recent log file: $LATEST_LOG"
        echo "Last 10 log entries:"
        echo "--------------------"
        tail -n 10 "$LATEST_LOG"
    else
        echo "❌ No log files found in $WINDOWS_LOG_PATH"
    fi
else
    echo "❌ Windows service logs directory not found at: $WINDOWS_LOG_PATH"
fi

echo
echo "Log File Permissions:"
echo "--------------------"
if [ -d "$CONSOLE_LOG_PATH" ]; then
    echo "Console app logs directory permissions:"
    ls -la "$CONSOLE_LOG_PATH"
fi

if [ -d "$WINDOWS_LOG_PATH" ]; then
    echo "Windows service logs directory permissions:"
    ls -la "$WINDOWS_LOG_PATH"
fi

echo
echo "To view complete logs, use:"
echo "cat /path/to/logfile | less"
echo "or"
echo "tail -n 100 /path/to/logfile"
