using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.AI.Distributed.Models;
using Nexo.Core.Application.Services.AI.Distributed.Processing;
using Nexo.Core.Application.Services.AI.Distributed.Statistics;

namespace Nexo.Core.Application.Services.AI.Distributed.Core
{
    /// <summary>
    /// Distributed AI processing service for multi-device coordination
    /// </summary>
    public class AIDistributedProcessor
    {
        private readonly ILogger<AIDistributedProcessor> _logger;
        private readonly NodeManager _nodeManager;
        private readonly TaskProcessor _taskProcessor;
        private readonly StatisticsCollector _statisticsCollector;

        public AIDistributedProcessor(ILogger<AIDistributedProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nodeManager = new NodeManager(_logger);
            _taskProcessor = new TaskProcessor(_logger, _nodeManager);
            _statisticsCollector = new StatisticsCollector(_logger);
        }

        /// <summary>
        /// Registers a processing node
        /// </summary>
        public Task<bool> RegisterNodeAsync(NodeRegistrationRequest request)
        {
            return _nodeManager.RegisterNodeAsync(request);
        }

        /// <summary>
        /// Unregisters a processing node
        /// </summary>
        public Task<bool> UnregisterNodeAsync(string nodeId)
        {
            return _nodeManager.UnregisterNodeAsync(nodeId);
        }

        /// <summary>
        /// Updates node heartbeat
        /// </summary>
        public Task<bool> UpdateNodeHeartbeatAsync(string nodeId, NodeResourceInfo resourceInfo)
        {
            return _nodeManager.UpdateNodeHeartbeatAsync(nodeId, resourceInfo);
        }

        /// <summary>
        /// Submits a distributed task
        /// </summary>
        public Task<DistributedTask> SubmitTaskAsync(DistributedTaskRequest request)
        {
            return _taskProcessor.SubmitTaskAsync(request);
        }

        /// <summary>
        /// Gets task status
        /// </summary>
        public Task<DistributedTask?> GetTaskStatusAsync(string taskId)
        {
            return _taskProcessor.GetTaskStatusAsync(taskId);
        }

        /// <summary>
        /// Cancels a distributed task
        /// </summary>
        public Task<bool> CancelTaskAsync(string taskId)
        {
            return _taskProcessor.CancelTaskAsync(taskId);
        }

        /// <summary>
        /// Gets all available nodes
        /// </summary>
        public Task<List<ProcessingNode>> GetAvailableNodesAsync()
        {
            return _nodeManager.GetAvailableNodesAsync();
        }

        /// <summary>
        /// Gets task distribution statistics
        /// </summary>
        public Task<DistributionStatistics> GetDistributionStatisticsAsync()
        {
            return _statisticsCollector.GetDistributionStatisticsAsync();
        }
    }
}
