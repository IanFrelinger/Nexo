using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.CLI.Dashboard
{
    /// <summary>
    /// Project status widget showing current project information
    /// </summary>
    public class ProjectStatusWidget : IDashboardWidget
    {
        public string Title => "Project Status";
        
        public async Task RenderAsync(Rectangle area)
        {
            var status = await GetProjectStatus();
            
            Console.SetCursorPosition(area.X, area.Y);
            Console.WriteLine($"┌─ {Title} ─".PadRight(area.Width - 1, '─') + "┐");
            
            Console.SetCursorPosition(area.X, area.Y + 1);
            Console.WriteLine($"│ Project: {status.CurrentProject ?? "None"}".PadRight(area.Width - 1) + "│");
            
            Console.SetCursorPosition(area.X, area.Y + 2);
            Console.WriteLine($"│ Platform: {status.CurrentPlatform ?? "Not Set"}".PadRight(area.Width - 1) + "│");
            
            Console.SetCursorPosition(area.X, area.Y + 3);
            Console.WriteLine($"│ Processes: {status.ActiveProcesses}".PadRight(area.Width - 1) + "│");
            
            Console.SetCursorPosition(area.X, area.Y + 4);
            var errorStatus = status.HasErrors ? "❌ Errors" : "✅ No Errors";
            Console.WriteLine($"│ {errorStatus}".PadRight(area.Width - 1) + "│");
            
            Console.SetCursorPosition(area.X, area.Y + 5);
            var warningStatus = status.HasWarnings ? "⚠️ Warnings" : "✅ No Warnings";
            Console.WriteLine($"│ {warningStatus}".PadRight(area.Width - 1) + "│");
            
            // Show recent activity
            Console.SetCursorPosition(area.X, area.Y + 6);
            Console.WriteLine($"│ Last Build: {DateTime.UtcNow.AddMinutes(-15):HH:mm:ss}".PadRight(area.Width - 1) + "│");
            
            Console.SetCursorPosition(area.X, area.Y + area.Height - 1);
            Console.WriteLine("└" + new string('─', area.Width - 2) + "┘");
        }
        
        public async Task HandleInteractionAsync()
        {
            // Show detailed project information
            Console.Clear();
            Console.WriteLine("📁 Detailed Project Information");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            var status = await GetProjectStatus();
            
            Console.WriteLine($"Project: {status.CurrentProject ?? "None"}");
            Console.WriteLine($"Platform: {status.CurrentPlatform ?? "Not Set"}");
            Console.WriteLine($"Active Processes: {status.ActiveProcesses}");
            Console.WriteLine($"Has Errors: {status.HasErrors}");
            Console.WriteLine($"Has Warnings: {status.HasWarnings}");
            Console.WriteLine();
            
            Console.WriteLine("📊 Recent Activity:");
            Console.WriteLine($"  • Last Build: {DateTime.UtcNow.AddMinutes(-15):yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  • Last Test: {DateTime.UtcNow.AddMinutes(-30):yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  • Last Analysis: {DateTime.UtcNow.AddHours(-1):yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();
            
            Console.WriteLine("🔧 Available Actions:");
            Console.WriteLine("  • Build project");
            Console.WriteLine("  • Run tests");
            Console.WriteLine("  • Analyze code");
            Console.WriteLine("  • Generate documentation");
            Console.WriteLine();
            
            Console.WriteLine("Press any key to return to dashboard...");
            Console.ReadKey(true);
        }
        
        private async Task<ProjectStatus> GetProjectStatus()
        {
            // This would integrate with actual project management
            return new ProjectStatus
            {
                CurrentProject = "Nexo",
                CurrentPlatform = "Cross-Platform",
                ActiveProcesses = 3,
                HasErrors = false,
                HasWarnings = true
            };
        }
    }
}
