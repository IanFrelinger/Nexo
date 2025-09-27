using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.AI.Distributed.Models;
using Nexo.Core.Application.Services.AI.Distributed.Processing;

namespace Nexo.Core.Application.Services.AI.Distributed.Statistics
{
    /// <summary>
    /// Collects distribution statistics for the distributed processor
    /// </summary>
    public partial class StatisticsCollector
    {
        private readonly ILogger _logger;
        private readonly NodeManager _nodeManager;
        private readonly TaskProcessor _taskProcessor;

        public StatisticsCollector(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // NodeManager and TaskProcessor will be resolved via setters for orchestration wiring
            _nodeManager = null!;
            _taskProcessor = null!;
        }

        public StatisticsCollector(ILogger logger, NodeManager nodeManager, TaskProcessor taskProcessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nodeManager = nodeManager ?? throw new ArgumentNullException(nameof(nodeManager));
            _taskProcessor = taskProcessor ?? throw new ArgumentNullException(nameof(taskProcessor));
        }

        public Task<DistributionStatistics> GetDistributionStatisticsAsync()
        {
            try
            {
                var nodes = _nodeManager.GetAllNodes();
                var tasks = _taskProcessor.GetAllTasks();

                var statistics = new DistributionStatistics
                {
                    TotalNodes = nodes.Count,
                    AvailableNodes = nodes.Count(n => n.Status == NodeStatus.Available),
                    BusyNodes = nodes.Count(n => n.Status == NodeStatus.Busy),
                    OfflineNodes = nodes.Count(n => n.Status == NodeStatus.Offline),
                    TotalTasks = tasks.Count,
                    PendingTasks = tasks.Count(t => t.Status == TaskStatus.Pending),
                    RunningTasks = tasks.Count(t => t.Status == TaskStatus.Running),
                    CompletedTasks = tasks.Count(t => t.Status == TaskStatus.Completed),
                    FailedTasks = tasks.Count(t => t.Status == TaskStatus.Failed),
                    GeneratedAt = DateTime.UtcNow
                };

                return Task.FromResult(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get distribution statistics");
                throw;
            }
        }
    }
}

