using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Dashboard
{
    /// <summary>
    /// Real-time terminal dashboard with live performance metrics and status
    /// </summary>
    public class RealTimeDashboard : IRealTimeDashboard
    {
        private readonly ILogger<RealTimeDashboard> _logger;
        private readonly List<IDashboardWidget> _widgets;
        private volatile bool _isRunning;
        private DashboardData? _currentData;
        
        public RealTimeDashboard(ILogger<RealTimeDashboard> logger)
        {
            _logger = logger;
            _widgets = new List<IDashboardWidget>();
            InitializeWidgets();
        }
        
        public async Task ShowRealTimeDashboard(CancellationToken cancellationToken = default)
        {
            _isRunning = true;
            
            Console.Clear();
            Console.CursorVisible = false;
            
            try
            {
                // Initialize dashboard layout
                await InitializeDashboard();
                
                // Start update loop
                await DashboardUpdateLoop(cancellationToken);
            }
            finally
            {
                Console.CursorVisible = true;
                Console.Clear();
            }
        }
        
        public async Task UpdateDashboardAsync(DashboardData data)
        {
            _currentData = data;
            await Task.CompletedTask;
        }
        
        public async Task StopDashboardAsync()
        {
            _isRunning = false;
            await Task.CompletedTask;
        }
        
        private void InitializeWidgets()
        {
            // Add default widgets
            _widgets.Add(new PerformanceWidget());
            _widgets.Add(new AdaptationWidget());
            _widgets.Add(new ProjectStatusWidget());
            _widgets.Add(new SystemHealthWidget());
        }
        
        private async Task InitializeDashboard()
        {
            Console.Clear();
            await RenderDashboardHeader();
            await RenderMainContent();
            await RenderDashboardFooter();
        }
        
        private async Task DashboardUpdateLoop(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Collect current data
                    var dashboardData = await CollectDashboardData();
                    await UpdateDashboardAsync(dashboardData);
                    
                    // Update all widgets
                    await UpdateAllWidgets(dashboardData);
                    
                    // Render dashboard
                    await RenderDashboard();
                    
                    // Handle user input
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        await HandleDashboardInput(key);
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
                catch (Exception ex)
                {
                    await ShowDashboardError(ex);
                }
            }
        }
        
        private async Task<DashboardData> CollectDashboardData()
        {
            return new DashboardData
            {
                PerformanceMetrics = await GetPerformanceMetricsAsync(),
                AdaptationStatus = await GetAdaptationStatusAsync(),
                ProjectStatus = await GetProjectStatusAsync(),
                SystemHealth = await GetSystemHealthAsync(),
                Timestamp = DateTime.UtcNow
            };
        }
        
        private async Task<PerformanceMetrics> GetPerformanceMetricsAsync()
        {
            // This would integrate with actual performance monitoring
            return new PerformanceMetrics
            {
                CpuUsage = 0.25, // 25% CPU usage
                MemoryUsage = 1024 * 1024 * 512, // 512 MB
                FrameRate = 60.0,
                ActiveThreads = Environment.ProcessorCount,
                Uptime = TimeSpan.FromHours(2.5)
            };
        }
        
        private async Task<AdaptationStatus> GetAdaptationStatusAsync()
        {
            // This would integrate with actual adaptation engine
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
                    }
                },
                RecentImprovements = new List<RecentImprovement>
                {
                    new RecentImprovement
                    {
                        Type = "Memory",
                        ActualImprovement = 0.15, // 15% improvement
                        AppliedAt = DateTime.UtcNow.AddMinutes(-2)
                    }
                }
            };
        }
        
        private async Task<ProjectStatus> GetProjectStatusAsync()
        {
            return new ProjectStatus
            {
                CurrentProject = "Nexo",
                CurrentPlatform = "Cross-Platform",
                ActiveProcesses = 3,
                HasErrors = false,
                HasWarnings = true
            };
        }
        
        private async Task<SystemHealth> GetSystemHealthAsync()
        {
            return new SystemHealth
            {
                IsHealthy = true,
                Issues = new List<string>(),
                Warnings = new List<string> { "High memory usage detected" }
            };
        }
        
        private async Task UpdateAllWidgets(DashboardData data)
        {
            foreach (var widget in _widgets)
            {
                try
                {
                    // Widgets would update their internal state here
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update widget: {WidgetTitle}", widget.Title);
                }
            }
        }
        
        private async Task RenderDashboard()
        {
            Console.SetCursorPosition(0, 0);
            
            // Header
            await RenderDashboardHeader();
            
            // Main content area
            await RenderMainContent();
            
            // Footer with controls
            await RenderDashboardFooter();
        }
        
        private async Task RenderDashboardHeader()
        {
            var header = $"🚀 Nexo Real-Time Dashboard - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            WriteColoredLine(header, ConsoleColor.Cyan);
            WriteHorizontalLine('═', Console.WindowWidth);
        }
        
        private async Task RenderMainContent()
        {
            var contentArea = new Rectangle(0, 3, Console.WindowWidth, Console.WindowHeight - 6);
            
            // Split content area into widget areas
            var widgetAreas = CalculateWidgetAreas(contentArea, _widgets.Count);
            
            for (int i = 0; i < _widgets.Count; i++)
            {
                try
                {
                    await _widgets[i].RenderAsync(widgetAreas[i]);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to render widget: {WidgetTitle}", _widgets[i].Title);
                }
            }
        }
        
        private async Task RenderDashboardFooter()
        {
            var footerY = Console.WindowHeight - 3;
            Console.SetCursorPosition(0, footerY);
            
            WriteHorizontalLine('═', Console.WindowWidth);
            
            Console.SetCursorPosition(0, footerY + 1);
            var controls = "Controls: Q/Esc=Quit, R=Refresh, H=Help, 1-4=Widget Focus";
            Console.Write(controls.PadRight(Console.WindowWidth - 1));
            
            Console.SetCursorPosition(0, footerY + 2);
            var status = $"Status: {(_isRunning ? "Running" : "Stopped")} | Last Update: {DateTime.Now:HH:mm:ss}";
            Console.Write(status.PadRight(Console.WindowWidth - 1));
        }
        
        private Rectangle[] CalculateWidgetAreas(Rectangle contentArea, int widgetCount)
        {
            var areas = new Rectangle[widgetCount];
            
            if (widgetCount == 0) return areas;
            
            var cols = (int)Math.Ceiling(Math.Sqrt(widgetCount));
            var rows = (int)Math.Ceiling((double)widgetCount / cols);
            
            var widgetWidth = contentArea.Width / cols;
            var widgetHeight = contentArea.Height / rows;
            
            for (int i = 0; i < widgetCount; i++)
            {
                var col = i % cols;
                var row = i / cols;
                
                areas[i] = new Rectangle(
                    contentArea.X + col * widgetWidth,
                    contentArea.Y + row * widgetHeight,
                    widgetWidth,
                    widgetHeight
                );
            }
            
            return areas;
        }
        
        private async Task HandleDashboardInput(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    _isRunning = false;
                    break;
                    
                case ConsoleKey.R:
                    // Refresh dashboard
                    Console.Clear();
                    await InitializeDashboard();
                    break;
                    
                case ConsoleKey.H:
                    await ShowDashboardHelp();
                    break;
                    
                case ConsoleKey.D1:
                case ConsoleKey.D2:
                case ConsoleKey.D3:
                case ConsoleKey.D4:
                case ConsoleKey.D5:
                    var widgetIndex = key.Key - ConsoleKey.D1;
                    if (widgetIndex < _widgets.Count)
                    {
                        await _widgets[widgetIndex].HandleInteractionAsync();
                    }
                    break;
            }
        }
        
        private async Task ShowDashboardHelp()
        {
            Console.Clear();
            Console.WriteLine("🚀 Nexo Dashboard Help");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("📊 Widgets:");
            for (int i = 0; i < _widgets.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {_widgets[i].Title}");
            }
            Console.WriteLine();
            Console.WriteLine("⌨️  Controls:");
            Console.WriteLine("  Q, Esc    - Quit dashboard");
            Console.WriteLine("  R         - Refresh dashboard");
            Console.WriteLine("  H         - Show this help");
            Console.WriteLine("  1-4       - Focus on specific widget");
            Console.WriteLine();
            Console.WriteLine("Press any key to return to dashboard...");
            Console.ReadKey(true);
        }
        
        private async Task ShowDashboardError(Exception ex)
        {
            var errorY = Console.WindowHeight - 1;
            Console.SetCursorPosition(0, errorY);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"❌ Dashboard Error: {ex.Message}".PadRight(Console.WindowWidth - 1));
            Console.ResetColor();
        }
        
        private void WriteColoredLine(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }
        
        private void WriteHorizontalLine(char character, int width)
        {
            Console.WriteLine(new string(character, width));
        }
    }
}
