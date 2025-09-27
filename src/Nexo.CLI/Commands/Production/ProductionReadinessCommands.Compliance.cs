using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Nexo.Core.Application.Interfaces.Security;

namespace Nexo.CLI.Commands.Production
{
    /// <summary>
    /// Compliance checking and recommendations functionality for ProductionReadinessCommands
    /// </summary>
    public partial class ProductionReadinessCommands
    {
        /// <summary>
        /// Checks security compliance status.
        /// </summary>
        [Command("security compliance")]
        [Description("Check security compliance status")]
        public async Task CheckComplianceStatusAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]List Security Compliance Status[/]");
                
                var status = await _securityAuditor.GetSecurityComplianceStatusAsync(cancellationToken);

                var scoreColor = status.OverallComplianceScore switch
                {
                    >= 90 => "green",
                    >= 80 => "yellow",
                    >= 70 => "orange3",
                    _ => "red"
                };

                AnsiConsole.MarkupLine($"[bold {scoreColor}]Overall Compliance Score: {status.OverallComplianceScore:F1}/100[/]");
                AnsiConsole.MarkupLine($"[bold {(status.IsCompliant ? "green" : "red")}]Compliance Status: {(status.IsCompliant ? "SUCCESS: Compliant" : "ERROR: Non-Compliant")}[/]");
                
                // Display compliance details
                var table = new Table();
                table.AddColumn("Standard");
                table.AddColumn("Status");
                table.AddColumn("Score");
                
                if (status.GDPRCompliance != null)
                    table.AddRow("GDPR", 
                        status.GDPRCompliance.IsCompliant ? "SUCCESS: Compliant" : "ERROR: Non-Compliant",
                        $"{status.GDPRCompliance.Score:F1}/100");
                
                if (status.HIPAACompliance != null)
                    table.AddRow("HIPAA", 
                        status.HIPAACompliance.IsCompliant ? "SUCCESS: Compliant" : "ERROR: Non-Compliant",
                        $"{status.HIPAACompliance.Score:F1}/100");
                
                if (status.SOXCompliance != null)
                    table.AddRow("SOX", 
                        status.SOXCompliance.IsCompliant ? "SUCCESS: Compliant" : "ERROR: Non-Compliant",
                        $"{status.SOXCompliance.Score:F1}/100");
                
                if (status.ISO27001Compliance != null)
                    table.AddRow("ISO 27001", 
                        status.ISO27001Compliance.IsCompliant ? "SUCCESS: Compliant" : "ERROR: Non-Compliant",
                        $"{status.ISO27001Compliance.Score:F1}/100");
                
                if (status.PCICompliance != null)
                    table.AddRow("PCI DSS", 
                        status.PCICompliance.IsCompliant ? "SUCCESS: Compliant" : "ERROR: Non-Compliant",
                        $"{status.PCICompliance.Score:F1}/100");
                
                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking compliance status");
                AnsiConsole.MarkupLine($"[bold red]ERROR: Error: {ex.Message}[/]");
            }
        }
    }
}
