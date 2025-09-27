using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Security;
using Nexo.Infrastructure.Services.Caching.Advanced;

namespace Nexo.CLI.Commands.AI
{
    /// <summary>
    /// Security compliance functionality for AI operations commands
    /// </summary>
    public partial class AIOperationsCommands
    {
        /// <summary>
        /// Creates security compliance commands.
        /// </summary>
        private Command CreateSecurityComplianceCommand()
        {
            var securityCommand = new Command("security", "Security compliance and monitoring");

            // Security health check
            var healthCommand = new Command("health", "Perform security health check");
            healthCommand.SetHandler(async () =>
            {
                try
                {
                    Console.WriteLine("Security Security Health Check");
                    Console.WriteLine(new string('=', 25));
                    
                    var healthCheck = await _securityComplianceService.PerformSecurityHealthCheckAsync();

                    Console.WriteLine($"Overall Score: {healthCheck.OverallScore}/100");
                    Console.WriteLine($"API Key Health: {healthCheck.ApiKeyHealth}/100");
                    Console.WriteLine($"Security Event Health: {healthCheck.SecurityEventHealth}/100");
                    Console.WriteLine();
                    Console.WriteLine("List Recommendations:");
                    foreach (var rec in healthCheck.Recommendations)
                    {
                        var priority = rec.Priority switch
                        {
                            RecommendationPriority.Low => "🟢",
                            RecommendationPriority.Medium => "🟡",
                            RecommendationPriority.High => "🟠",
                            RecommendationPriority.Critical => "🔴",
                            _ => "⚪"
                        };
                        Console.WriteLine($"  {priority} {rec.Title}: {rec.Description}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to perform security health check: {ex.Message}");
                    _logger.LogError(ex, "Failed to perform security health check");
                }
            });

            // Compliance report
            var complianceCommand = new Command("report", "Generate compliance report");
            var startDateOption = new Option<string>("--start", "Start date (yyyy-mm-dd)");
            var endDateOption = new Option<string>("--end", "End date (yyyy-mm-dd)");

            complianceCommand.AddOption(startDateOption);
            complianceCommand.AddOption(endDateOption);

            complianceCommand.SetHandler(async (string startDate, string endDate) =>
            {
                try
                {
                    var start = string.IsNullOrEmpty(startDate) 
                        ? DateTimeOffset.UtcNow.AddDays(-30) 
                        : DateTimeOffset.Parse(startDate);
                    var end = string.IsNullOrEmpty(endDate) 
                        ? DateTimeOffset.UtcNow 
                        : DateTimeOffset.Parse(endDate);

                    Console.WriteLine($"Security Security Compliance Report ({start:yyyy-MM-dd} to {end:yyyy-MM-dd})");
                    Console.WriteLine(new string('=', 60));
                    
                    var report = await _securityComplianceService.GenerateComplianceReportAsync(start, end);

                    Console.WriteLine($"Total Events: {report.TotalEvents:N0}");
                    Console.WriteLine($"Security Events: {report.SecurityEvents:N0}");
                    Console.WriteLine($"Compliance Events: {report.ComplianceEvents:N0}");
                    Console.WriteLine();
                    Console.WriteLine("🔑 API Key Statistics:");
                    Console.WriteLine($"  Total Keys: {report.ApiKeyStatistics.TotalKeys}");
                    Console.WriteLine($"  Active Keys: {report.ApiKeyStatistics.ActiveKeys}");
                    Console.WriteLine($"  Expired Keys: {report.ApiKeyStatistics.ExpiredKeys}");
                    Console.WriteLine($"  Revoked Keys: {report.ApiKeyStatistics.RevokedKeys}");
                    Console.WriteLine();
                    Console.WriteLine("Stats Security Metrics:");
                    Console.WriteLine($"  Threat Level: {report.SecurityMetrics.ThreatLevel}");
                    Console.WriteLine($"  Security Event Rate: {report.SecurityMetrics.SecurityEventRate:F2}/hour");
                    Console.WriteLine($"  Failed Auth Attempts: {report.SecurityMetrics.FailedAuthenticationAttempts:N0}");
                    Console.WriteLine();
                    Console.WriteLine("WARNING:  Violations:");
                    foreach (var violation in report.Violations)
                    {
                        var severity = violation.Severity switch
                        {
                            ComplianceViolationSeverity.Low => "🟢",
                            ComplianceViolationSeverity.Medium => "🟡",
                            ComplianceViolationSeverity.High => "🟠",
                            ComplianceViolationSeverity.Critical => "🔴",
                            _ => "⚪"
                        };
                        Console.WriteLine($"  {severity} {violation.Description}");
                        Console.WriteLine($"    Remediation: {violation.Remediation}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to generate compliance report: {ex.Message}");
                    _logger.LogError(ex, "Failed to generate compliance report");
                }
            }, startDateOption, endDateOption);

            securityCommand.AddCommand(healthCommand);
            securityCommand.AddCommand(complianceCommand);

            return securityCommand;
        }
    }
}
