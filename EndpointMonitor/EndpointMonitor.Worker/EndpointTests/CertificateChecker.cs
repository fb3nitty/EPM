
using System;
using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using EndpointMonitor.Worker.Models;
using Microsoft.Extensions.Logging;

namespace EndpointMonitor.Worker.EndpointTests
{
    public class CertificateChecker : IEndpointTester
    {
        private readonly ILogger<CertificateChecker> _logger;

        public CertificateChecker(ILogger<CertificateChecker> logger)
        {
            _logger = logger;
        }

        public bool CanHandle(EndpointConfig endpoint)
        {
            return endpoint.TestType.Equals("Certificate", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<TestResult> TestEndpointAsync(EndpointConfig endpoint)
        {
            _logger.LogInformation("Testing SSL certificate for {Host}:{Port}", endpoint.Host, endpoint.Port);
            
            var stopwatch = Stopwatch.StartNew();
            bool success = false;
            string message = string.Empty;
            X509Certificate2? certificate = null;

            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(endpoint.Host, endpoint.Port);
                    var timeoutTask = Task.Delay(endpoint.Timeout);
                    
                    if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
                    {
                        throw new TimeoutException($"Connection to {endpoint.Host}:{endpoint.Port} timed out after {endpoint.Timeout}ms");
                    }

                    using (var sslStream = new SslStream(client.GetStream(), false, ValidateServerCertificate))
                    {
                        await sslStream.AuthenticateAsClientAsync(endpoint.Host);
                        if (sslStream.RemoteCertificate != null)
                        {
                            certificate = new X509Certificate2(sslStream.RemoteCertificate);
                        }
                    }
                    
                    if (certificate != null)
                    {
                        // Check certificate expiration
                        DateTime expirationDate = DateTime.Parse(certificate.GetExpirationDateString());
                        int daysUntilExpiration = (expirationDate - DateTime.Now).Days;
                        
                        success = daysUntilExpiration > endpoint.MinCertificateDaysValid;
                        message = success 
                            ? $"Certificate for {endpoint.Host} is valid for {daysUntilExpiration} days (expires on {expirationDate:yyyy-MM-dd})"
                            : $"Certificate for {endpoint.Host} expires in {daysUntilExpiration} days on {expirationDate:yyyy-MM-dd}, which is less than the minimum of {endpoint.MinCertificateDaysValid} days";
                    }
                    else
                    {
                        success = false;
                        message = $"No certificate found for {endpoint.Host}:{endpoint.Port}";
                    }
                }
            }
            catch (TimeoutException ex)
            {
                success = false;
                message = ex.Message;
                _logger.LogWarning(message);
            }
            catch (Exception ex)
            {
                success = false;
                message = $"Failed to check certificate for {endpoint.Host}:{endpoint.Port}. Error: {ex.Message}";
                _logger.LogError(ex, message);
            }
            
            stopwatch.Stop();
            
            return new TestResult(success, message, stopwatch.Elapsed, endpoint);
        }
        
        private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            // We're just retrieving the certificate, not validating it here
            // The actual validation logic is in the TestEndpointAsync method
            return true;
        }
    }
}
