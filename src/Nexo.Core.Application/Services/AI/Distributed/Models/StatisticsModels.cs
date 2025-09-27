using System;

namespace Nexo.Core.Application.Services.AI.Distributed.Models
{
    /// <summary>
    /// Distribution statistics
    /// </summary>
    public partial class DistributionStatistics
    {
        public int TotalNodes { get; set; }
        public int AvailableNodes { get; set; }
        public int BusyNodes { get; set; }
        public int OfflineNodes { get; set; }
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int RunningTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int FailedTasks { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
