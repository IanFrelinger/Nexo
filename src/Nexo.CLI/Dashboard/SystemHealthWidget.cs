using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.CLI.Dashboard
{
    /// <summary>
    /// System health widget showing overall system status
    /// </summary>
    public class SystemHealthWidget : IDashboardWidget
    {
        public string Title => "System Health";
        
        public async Task RenderAsync(Rectangle area)
        {
            var health = await GetSystemHealth();
            
            Console.SetCursorPosition(area.X, area.Y);
            Console.WriteLine($"┌─ {Title} ─".PadRight(area.Width - 1, '─') + "┐");
            
            Console.SetCursorPosition(area.X, area.Y + 1);
            var healthIcon = health.IsHealthy ? "✅" : "❌";
            var healthStatus = health.IsHealthy ? "Healthy" : "Issues Detected";
            Console.WriteLine($"│ {healthIcon} {healthStatus}".PadRight(area.Width - 1) + "│");
            
            Console.SetCursorPosition(area.X, area.Y + 2);
            Console.WriteLine($"│ Issues: {health.Issues.Count()}".PadRight(area.Width - 1) + "│");
            
            Console.SetCursorPosition(area.X, area.Y + 3);
            Console.WriteLine($"│ Warnings: {health.Warnings.Count()}".PadRight(area.Width - 1) + "│");
            
            // Show recent issues/warnings
            var recentIssues = health.Issues.Take(area.Height - 6).ToList();
            var recentWarnings = health.Warnings.Take(area.Height - 6 - recentIssues.Count).ToList();
            
            int lineIndex = 4;
            
            foreach (var issue in recentIssues)
            {
                Console.SetCursorPosition(area.X, area.Y + lineIndex);
                Console.WriteLine($"│ ❌ {issue}".PadRight(area.Width - 1) + "│");
                lineIndex++;
            }
            
            foreach (var warning in recentWarnings)
            {
                Console.SetCursorPosition(area.X, area.Y + lineIndex);
                Console.WriteLine($"│ ⚠️ {warning}".PadRight(area.Width - 1) + "│");
                lineIndex++;
            }
            
            Console.SetCursorPosition(area.X, area.Y + area.Height - 1);
            Console.WriteLine("└" + new string('─', area.Width - 2) + "┘");
        }
        
        public async Task HandleInteractionAsync()
        {
            // Show detailed system health information
            Console.Clear();
            Console.WriteLine("🏥 Detailed System Health Information");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            var health = await GetSystemHealth();
            
            Console.WriteLine($"Overall Status: {(health.IsHealthy ? "✅ Healthy" : "❌ Issues Detected")}");
            Console.WriteLine($"Total Issues: {health.Issues.Count()}");
            Console.WriteLine($"Total Warnings: {health.Warnings.Count()}");
            Console.WriteLine();
            
            if (health.Issues.Any())
            {
                Console.WriteLine("❌ Issues:");
                foreach (var issue in health.Issues)
                {
                    Console.WriteLine($"  • {issue}");
                }
                Console.WriteLine();
            }
            
            if (health.Warnings.Any())
            {
                Console.WriteLine("⚠️ Warnings:");
                foreach (var warning in health.Warnings)
                {
                    Console.WriteLine($"  • {warning}");
                }
                Console.WriteLine();
            }
            
            Console.WriteLine("🔧 System Information:");
            Console.WriteLine($"  • OS: {Environment.OSVersion}");
            Console.WriteLine($"  • .NET Version: {Environment.Version}");
            Console.WriteLine($"  • Processor Count: {Environment.ProcessorCount}");
            Console.WriteLine($"  • Working Directory: {Environment.CurrentDirectory}");
            Console.WriteLine($"  • Machine Name: {Environment.MachineName}");
            Console.WriteLine($"  • User Name: {Environment.UserName}");
            Console.WriteLine();
            
            Console.WriteLine("Press any key to return to dashboard...");
            Console.ReadKey(true);
        }
        
        private async Task<SystemHealth> GetSystemHealth()
        {
            // This would integrate with actual system monitoring
            var issues = new List<string>();
            var warnings = new List<string>();
            
            // Simulate some health checks
            var memoryUsage = GC.GetTotalMemory(false) / 1024 / 1024; // MB
            if (memoryUsage > 1000)
            {
                warnings.Add($"High memory usage: {memoryUsage} MB");
            }
            
            var processorCount = Environment.ProcessorCount;
            if (processorCount < 4)
            {
                warnings.Add($"Low processor count: {processorCount}");
            }
            
            // Check for common issues
            if (DateTime.UtcNow.Hour > 22 || DateTime.UtcNow.Hour < 6)
            {
                warnings.Add("Running during off-hours");
            }
            
            return new SystemHealth
            {
                IsHealthy = !issues.Any(),
                Issues = issues,
                Warnings = warnings
            };
        }
    }
}
