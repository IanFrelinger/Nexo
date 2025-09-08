using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.CLI.Dashboard
{
    /// <summary>
    /// Performance monitoring widget for the dashboard
    /// </summary>
    public class PerformanceWidget : IDashboardWidget
    {
        public string Title => "Performance Metrics";
        
        private readonly Queue<PerformanceSnapshot> _performanceHistory = new();
        
        public async Task RenderAsync(Rectangle area)
        {
            var metrics = await GetCurrentPerformanceMetrics();
            
            // Add to history
            _performanceHistory.Enqueue(new PerformanceSnapshot
            {
                Timestamp = DateTime.UtcNow,
                CpuUsage = metrics.CpuUsage,
                MemoryUsage = metrics.MemoryUsage,
                FrameRate = metrics.FrameRate
            });
            
            // Keep only recent history
            while (_performanceHistory.Count > 60) // 60 seconds of history
            {
                _performanceHistory.Dequeue();
            }
            
            // Render widget
            Console.SetCursorPosition(area.X, area.Y);
            Console.WriteLine($"┌─ {Title} ─".PadRight(area.Width - 1, '─') + "┐");
            
            Console.SetCursorPosition(area.X, area.Y + 1);
            Console.WriteLine($"│ CPU Usage: {metrics.CpuUsage:P1}".PadRight(area.Width - 1) + "│");
            
            Console.SetCursorPosition(area.X, area.Y + 2);
            Console.WriteLine($"│ Memory: {metrics.MemoryUsage / 1024 / 1024:F1} MB".PadRight(area.Width - 1) + "│");
            
            if (metrics.FrameRate.HasValue)
            {
                Console.SetCursorPosition(area.X, area.Y + 3);
                Console.WriteLine($"│ FPS: {metrics.FrameRate:F1}".PadRight(area.Width - 1) + "│");
            }
            
            Console.SetCursorPosition(area.X, area.Y + 4);
            Console.WriteLine($"│ Threads: {metrics.ActiveThreads}".PadRight(area.Width - 1) + "│");
            
            Console.SetCursorPosition(area.X, area.Y + 5);
            Console.WriteLine($"│ Uptime: {metrics.Uptime:hh\\:mm\\:ss}".PadRight(area.Width - 1) + "│");
            
            // Render mini performance graph
            await RenderPerformanceGraph(area, 6);
            
            Console.SetCursorPosition(area.X, area.Y + area.Height - 1);
            Console.WriteLine("└" + new string('─', area.Width - 2) + "┘");
        }
        
        public async Task HandleInteractionAsync()
        {
            // Show detailed performance information
            Console.Clear();
            Console.WriteLine("📊 Detailed Performance Information");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            var metrics = await GetCurrentPerformanceMetrics();
            
            Console.WriteLine($"CPU Usage: {metrics.CpuUsage:P2}");
            Console.WriteLine($"Memory Usage: {metrics.MemoryUsage / 1024 / 1024:F2} MB");
            Console.WriteLine($"Frame Rate: {metrics.FrameRate:F2} FPS");
            Console.WriteLine($"Active Threads: {metrics.ActiveThreads}");
            Console.WriteLine($"Uptime: {metrics.Uptime:dd\\.hh\\:mm\\:ss}");
            Console.WriteLine();
            
            Console.WriteLine("Press any key to return to dashboard...");
            Console.ReadKey(true);
        }
        
        private async Task<PerformanceMetrics> GetCurrentPerformanceMetrics()
        {
            // This would integrate with actual performance monitoring
            return new PerformanceMetrics
            {
                CpuUsage = 0.25 + (Math.Sin(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) * 0.1),
                MemoryUsage = 1024 * 1024 * (512 + (Math.Sin(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond * 0.5) * 50)),
                FrameRate = 60.0 + (Math.Sin(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond * 2) * 5),
                ActiveThreads = Environment.ProcessorCount,
                Uptime = TimeSpan.FromHours(2.5)
            };
        }
        
        private async Task RenderPerformanceGraph(Rectangle area, int startRow)
        {
            var graphHeight = area.Height - startRow - 1;
            var graphWidth = area.Width - 4;
            
            if (_performanceHistory.Count < 2 || graphHeight < 2) return;
            
            var data = _performanceHistory.TakeLast(graphWidth).ToList();
            var maxValue = data.Max(d => d.CpuUsage);
            var minValue = data.Min(d => d.CpuUsage);
            
            if (maxValue == minValue) return;
            
            for (int row = 0; row < graphHeight; row++)
            {
                Console.SetCursorPosition(area.X, area.Y + startRow + row);
                Console.Write("│ ");
                
                for (int col = 0; col < Math.Min(data.Count, graphWidth); col++)
                {
                    var value = data[col].CpuUsage;
                    var normalizedValue = (value - minValue) / (maxValue - minValue);
                    var threshold = 1.0 - (double)row / graphHeight;
                    
                    Console.Write(normalizedValue >= threshold ? "█" : " ");
                }
                
                Console.Write("│");
            }
        }
    }
    
    /// <summary>
    /// Represents a performance snapshot for history tracking
    /// </summary>
    public class PerformanceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
        public double? FrameRate { get; set; }
    }
}
