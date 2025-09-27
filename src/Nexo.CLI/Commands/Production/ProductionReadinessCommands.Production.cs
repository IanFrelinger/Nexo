using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Nexo.Core.Application.Interfaces.Performance;
using Nexo.Core.Application.Interfaces.Security;

namespace Nexo.CLI.Commands.Production
{
    /// <summary>
    /// Production readiness checks functionality for ProductionReadinessCommands
    /// </summary>
    public partial class ProductionReadinessCommands
    {
        /// <summary>
        /// Runs comprehensive production readiness check.
        /// </summary>
        [Command("production readiness")]
        [Description("Run comprehensive production readiness check")]
        public async Task RunProductionReadinessCheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]Running Production Readiness Check[/]");
                AnsiConsole.MarkupLine("[dim]Running comprehensive production readiness assessment...[/]");
                
                // Run performance optimization
                AnsiConsole.MarkupLine("\n[bold]1. Performance Optimization[/]");
                var perfOptions = new PerformanceOptimizationOptions
                {
                    OptimizeCaching = true,
                    OptimizeMemory = true,
                    OptimizeAI = true,
                    OptimizeSecurity = true,
                    OptimizeDatabase = true,
                    OptimizeNetwork = true
                };
                var perfResult = await _performanceOptimizer.OptimizePerformanceAsync(perfOptions, cancellationToken);
                
                // Run security audit
                AnsiConsole.MarkupLine("\n[bold]2. Security Audit[/]");
                var securityOptions = new SecurityAuditOptions
                {
                    AuditApiKeys = true,
                    AuditAuthentication = true,
                    AuditAuthorization = true,
                    AuditEncryption = true,
                    AuditAuditLogging = true,
                    AuditNetwork = true,
                    AuditCompliance = true,
                    AuditPerformance = true
                };
                var securityResult = await _securityAuditor.RunSecurityAuditAsync(securityOptions, cancellationToken);
                
                // Check compliance
                AnsiConsole.MarkupLine("\n[bold]3. Compliance Check[/]");
                var complianceStatus = await _securityAuditor.GetSecurityComplianceStatusAsync(cancellationToken);
                
                // Display overall results
                AnsiConsole.MarkupLine("\n[bold]Stats Production Readiness Summary[/]");
                
                var overallScore = (perfResult.Success ? 50 : 0) + (securityResult.Success ? 30 : 0) + (complianceStatus.IsCompliant ? 20 : 0);
                var scoreColor = overallScore switch
                {
                    >= 90 => "green",
                    >= 80 => "yellow",
                    >= 70 => "orange3",
                    _ => "red"
                };
                
                AnsiConsole.MarkupLine($"[bold {scoreColor}]Overall Readiness Score: {overallScore}/100[/]");
                
                var table = new Table();
                table.AddColumn("Component");
                table.AddColumn("Status");
                table.AddColumn("Score");
                
                table.AddRow("Performance Optimization", 
                    perfResult.Success ? "SUCCESS: Ready" : "ERROR: Issues",
                    perfResult.Success ? "50/50" : "0/50");
                
                table.AddRow("Security Audit", 
                    securityResult.Success ? "SUCCESS: Ready" : "ERROR: Issues",
                    securityResult.Success ? "30/30" : "0/30");
                
                table.AddRow("Compliance", 
                    complianceStatus.IsCompliant ? "SUCCESS: Compliant" : "ERROR: Non-Compliant",
                    complianceStatus.IsCompliant ? "20/20" : "0/20");
                
                AnsiConsole.Write(table);
                
                if (overallScore >= 90)
                {
                    AnsiConsole.MarkupLine("\n[bold green]SUCCESS System is production ready![/]");
                }
                else if (overallScore >= 80)
                {
                    AnsiConsole.MarkupLine("\n[bold yellow]WARNING: System is mostly ready with minor issues.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("\n[bold red]ERROR: System is not production ready. Address issues before deployment.[/]");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during production readiness check");
                AnsiConsole.MarkupLine($"[bold red]ERROR: Error: {ex.Message}[/]");
            }
        }
    }
}
