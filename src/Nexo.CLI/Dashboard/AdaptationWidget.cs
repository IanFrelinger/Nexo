using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.CLI.Dashboard
{
    /// <summary>
    /// Adaptation status widget showing real-time adaptations
    /// </summary>
    public class AdaptationWidget : IDashboardWidget
    {
        public string Title => "Real-Time Adaptations";
        
        private readonly Queue<AdaptationEvent> _recentAdaptations = new();
        
        public async Task RenderAsync(Rectangle area)
        {
            var status = await GetAdaptationStatus();
            
            Console.SetCursorPosition(area.X, area.Y);
            Console.WriteLine($"┌─ {Title} ─".PadRight(area.Width - 1, '─') + "┐");
            
            Console.SetCursorPosition(area.X, area.Y + 1);
            var statusColor = status.EngineStatus == AdaptationEngineStatus.Active ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.ForegroundColor = statusColor;
            Console.WriteLine($"│ Status: {status.EngineStatus}".PadRight(area.Width - 1) + "│");
            Console.ResetColor();
            
            Console.SetCursorPosition(area.X, area.Y + 2);
            Console.WriteLine($"│ Active: {status.ActiveAdaptations.Count()}".PadRight(area.Width - 1) + "│");
            
            Console.SetCursorPosition(area.X, area.Y + 3);
            Console.WriteLine($"│ Recent: {status.RecentImprovements.Count()}".PadRight(area.Width - 1) + "│");
            
            // Show recent adaptations
            var recentAdaptations = status.RecentImprovements.Take(area.Height - 6).ToList();
            
            for (int i = 0; i < recentAdaptations.Count; i++)
            {
                var adaptation = recentAdaptations[i];
                Console.SetCursorPosition(area.X, area.Y + 4 + i);
                
                var timeAgo = DateTime.UtcNow - adaptation.AppliedAt;
                var timeString = timeAgo.TotalMinutes < 1 ? "now" : $"{timeAgo.TotalMinutes:F0}m ago";
                
                var line = $"│ {adaptation.Type}: +{adaptation.ActualImprovement:P0} ({timeString})";
                Console.WriteLine(line.PadRight(area.Width - 1) + "│");
            }
            
            Console.SetCursorPosition(area.X, area.Y + area.Height - 1);
            Console.WriteLine("└" + new string('─', area.Width - 2) + "┘");
        }
        
        public async Task HandleInteractionAsync()
        {
            // Show detailed adaptation information
            Console.Clear();
            Console.WriteLine("🔄 Detailed Adaptation Information");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            var status = await GetAdaptationStatus();
            
            Console.WriteLine($"Engine Status: {status.EngineStatus}");
            Console.WriteLine($"Active Adaptations: {status.ActiveAdaptations.Count()}");
            Console.WriteLine($"Recent Improvements: {status.RecentImprovements.Count()}");
            Console.WriteLine();
            
            if (status.ActiveAdaptations.Any())
            {
                Console.WriteLine("🔄 Active Adaptations:");
                foreach (var adaptation in status.ActiveAdaptations)
                {
                    Console.WriteLine($"  • {adaptation.Type}: {adaptation.Description}");
                    Console.WriteLine($"    Progress: {adaptation.Progress:P0} | Started: {adaptation.StartedAt:HH:mm:ss}");
                }
                Console.WriteLine();
            }
            
            if (status.RecentImprovements.Any())
            {
                Console.WriteLine("📈 Recent Improvements:");
                foreach (var improvement in status.RecentImprovements.Take(10))
                {
                    var timeAgo = DateTime.UtcNow - improvement.AppliedAt;
                    Console.WriteLine($"  • {improvement.Type}: +{improvement.ActualImprovement:P2} ({timeAgo.TotalMinutes:F0}m ago)");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("Press any key to return to dashboard...");
            Console.ReadKey(true);
        }
        
        private async Task<AdaptationStatus> GetAdaptationStatus()
        {
            // This would integrate with actual adaptation engine
            var random = new Random();
            
            return new AdaptationStatus
            {
                EngineStatus = AdaptationEngineStatus.Active,
                ActiveAdaptations = new List<ActiveAdaptation>
                {
                    new ActiveAdaptation
                    {
                        Type = "Performance",
                        Description = "Optimizing memory usage",
                        StartedAt = DateTime.UtcNow.AddMinutes(-5),
                        Progress = 0.75
                    },
                    new ActiveAdaptation
                    {
                        Type = "Resource",
                        Description = "Adjusting thread pool",
                        StartedAt = DateTime.UtcNow.AddMinutes(-2),
                        Progress = 0.4
                    }
                },
                RecentImprovements = new List<RecentImprovement>
                {
                    new RecentImprovement
                    {
                        Type = "Memory",
                        ActualImprovement = 0.15,
                        AppliedAt = DateTime.UtcNow.AddMinutes(-2)
                    },
                    new RecentImprovement
                    {
                        Type = "CPU",
                        ActualImprovement = 0.08,
                        AppliedAt = DateTime.UtcNow.AddMinutes(-5)
                    },
                    new RecentImprovement
                    {
                        Type = "Response Time",
                        ActualImprovement = 0.12,
                        AppliedAt = DateTime.UtcNow.AddMinutes(-8)
                    }
                }
            };
        }
    }
    
    /// <summary>
    /// Represents an adaptation event for history tracking
    /// </summary>
    public class AdaptationEvent
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Impact { get; set; }
    }
}
