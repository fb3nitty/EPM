
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using EndpointMonitor.Worker.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace EndpointMonitor.Worker.EndpointTests
{
    public class SqlServerChecker : IEndpointTester
    {
        private readonly ILogger<SqlServerChecker> _logger;

        public SqlServerChecker(ILogger<SqlServerChecker> logger)
        {
            _logger = logger;
        }

        public bool CanTest(EndpointConfig endpoint)
        {
            return endpoint.TestType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<TestResult> TestEndpointAsync(EndpointConfig endpoint)
        {
            _logger.LogInformation("Testing SQL Server connectivity to {Host}:{Port}", endpoint.Host, endpoint.Port);
            
            var stopwatch = Stopwatch.StartNew();
            bool success = false;
            string message = string.Empty;
            List<string> databases = new List<string>();

            try
            {
                // Build connection string
                var connectionStringBuilder = new SqlConnectionStringBuilder
                {
                    DataSource = $"{endpoint.Host},{endpoint.Port}",
                    InitialCatalog = endpoint.SqlServerDatabase ?? "master",
                    ConnectTimeout = endpoint.Timeout / 1000, // Convert milliseconds to seconds
                    TrustServerCertificate = true // For simplicity in testing environments
                };

                // Add authentication details if provided
                if (!string.IsNullOrEmpty(endpoint.SqlServerUsername) && !string.IsNullOrEmpty(endpoint.SqlServerPassword))
                {
                    connectionStringBuilder.UserID = endpoint.SqlServerUsername;
                    connectionStringBuilder.Password = endpoint.SqlServerPassword;
                }
                else
                {
                    connectionStringBuilder.IntegratedSecurity = true;
                }

                // Connect to SQL Server
                using (var connection = new SqlConnection(connectionStringBuilder.ConnectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get list of databases
                    using (var command = new SqlCommand("SELECT name FROM sys.databases ORDER BY name", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                databases.Add(reader.GetString(0));
                            }
                        }
                    }
                    
                    success = true;
                    message = $"Successfully connected to SQL Server at {endpoint.Host}:{endpoint.Port} and found {databases.Count} databases: {string.Join(", ", databases)}";
                }
            }
            catch (SqlException ex)
            {
                success = false;
                message = $"SQL Server connection to {endpoint.Host}:{endpoint.Port} failed. Error: {ex.Message}";
                _logger.LogError(ex, message);
            }
            catch (Exception ex)
            {
                success = false;
                message = $"Unexpected error testing SQL Server at {endpoint.Host}:{endpoint.Port}. Error: {ex.Message}";
                _logger.LogError(ex, message);
            }
            
            stopwatch.Stop();
            
            return new TestResult(success, message, stopwatch.Elapsed, endpoint);
        }
    }
}
