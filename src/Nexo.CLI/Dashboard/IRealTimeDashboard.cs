using System.Threading;
using System.Threading.Tasks;

namespace Nexo.CLI.Dashboard
{
    /// <summary>
    /// Interface for real-time terminal dashboard with live performance metrics and status
    /// </summary>
    public interface IRealTimeDashboard
    {
        /// <summary>
        /// Shows the real-time dashboard with live updates
        /// </summary>
        Task ShowRealTimeDashboard(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates the dashboard with new data
        /// </summary>
        Task UpdateDashboardAsync(DashboardData data);
        
        /// <summary>
        /// Stops the dashboard
        /// </summary>
        Task StopDashboardAsync();
    }
    
    /// <summary>
    /// Interface for dashboard widgets
    /// </summary>
    public interface IDashboardWidget
    {
        string Title { get; }
        Task RenderAsync(Rectangle area);
        Task HandleInteractionAsync();
    }
    
    /// <summary>
    /// Represents dashboard data
    /// </summary>
    public class DashboardData
    {
        public PerformanceMetrics? PerformanceMetrics { get; set; }
        public AdaptationStatus? AdaptationStatus { get; set; }
        public ProjectStatus? ProjectStatus { get; set; }
        public SystemHealth? SystemHealth { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// Represents performance metrics
    /// </summary>
    public class PerformanceMetrics
    {
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
        public double? FrameRate { get; set; }
        public int ActiveThreads { get; set; }
        public TimeSpan Uptime { get; set; }
    }
    
    /// <summary>
    /// Represents adaptation status
    /// </summary>
    public class AdaptationStatus
    {
        public AdaptationEngineStatus EngineStatus { get; set; }
        public IEnumerable<ActiveAdaptation> ActiveAdaptations { get; set; } = new List<ActiveAdaptation>();
        public IEnumerable<RecentImprovement> RecentImprovements { get; set; } = new List<RecentImprovement>();
    }
    
    /// <summary>
    /// Represents project status
    /// </summary>
    public class ProjectStatus
    {
        public string? CurrentProject { get; set; }
        public string? CurrentPlatform { get; set; }
        public int ActiveProcesses { get; set; }
        public bool HasErrors { get; set; }
        public bool HasWarnings { get; set; }
    }
    
    /// <summary>
    /// Represents system health
    /// </summary>
    public class SystemHealth
    {
        public bool IsHealthy { get; set; }
        public IEnumerable<string> Issues { get; set; } = new List<string>();
        public IEnumerable<string> Warnings { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Represents an active adaptation
    /// </summary>
    public class ActiveAdaptation
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public double Progress { get; set; }
    }
    
    /// <summary>
    /// Represents a recent improvement
    /// </summary>
    public class RecentImprovement
    {
        public string Type { get; set; } = string.Empty;
        public double ActualImprovement { get; set; }
        public DateTime AppliedAt { get; set; }
    }
    
    /// <summary>
    /// Adaptation engine status
    /// </summary>
    public enum AdaptationEngineStatus
    {
        Inactive,
        Active,
        Paused,
        Error
    }
    
    /// <summary>
    /// Represents a rectangle area for rendering
    /// </summary>
    public struct Rectangle
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        
        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
