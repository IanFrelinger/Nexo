using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.AI.Distributed.Models;

namespace Nexo.Core.Application.Services.AI.Distributed.Processing
{
    /// <summary>
    /// Manages processing nodes for distributed AI processing
    /// </summary>
    public partial class NodeManager
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, ProcessingNode> _nodes;
        private readonly object _lockObject = new object();

        public NodeManager(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nodes = new Dictionary<string, ProcessingNode>();
        }

        /// <summary>
        /// Registers a processing node
        /// </summary>
        public Task<bool> RegisterNodeAsync(NodeRegistrationRequest request)
        {
            try
            {
                _logger.LogInformation("Registering processing node {NodeId} with capabilities {Capabilities}", 
                    request.NodeId, string.Join(", ", request.Capabilities));

                var node = new ProcessingNode
                {
                    NodeId = request.NodeId,
                    Name = request.Name,
                    Capabilities = request.Capabilities,
                    Status = NodeStatus.Available,
                    LastHeartbeat = DateTime.UtcNow,
                    ResourceInfo = request.ResourceInfo,
                    Location = request.Location
                };

                lock (_lockObject)
                {
                    _nodes[request.NodeId] = node;
                }

                _logger.LogInformation("Processing node {NodeId} registered successfully", request.NodeId);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register processing node {NodeId}", request.NodeId);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Unregisters a processing node
        /// </summary>
        public Task<bool> UnregisterNodeAsync(string nodeId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_nodes.ContainsKey(nodeId))
                    {
                        _nodes.Remove(nodeId);
                        _logger.LogInformation("Processing node {NodeId} unregistered", nodeId);
                        return Task.FromResult(true);
                    }
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister processing node {NodeId}", nodeId);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Updates node heartbeat
        /// </summary>
        public Task<bool> UpdateNodeHeartbeatAsync(string nodeId, NodeResourceInfo resourceInfo)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_nodes.TryGetValue(nodeId, out var node))
                    {
                        node.LastHeartbeat = DateTime.UtcNow;
                        node.ResourceInfo = resourceInfo;
                        return Task.FromResult(true);
                    }
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update heartbeat for node {NodeId}", nodeId);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Gets all available nodes
        /// </summary>
        public Task<List<ProcessingNode>> GetAvailableNodesAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    return Task.FromResult(_nodes.Values
                        .Where(n => n.Status == NodeStatus.Available && 
                                   DateTime.UtcNow - n.LastHeartbeat < TimeSpan.FromMinutes(5))
                        .ToList());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available nodes");
                return Task.FromResult(new List<ProcessingNode>());
            }
        }

        /// <summary>
        /// Gets all nodes
        /// </summary>
        public List<ProcessingNode> GetAllNodes()
        {
            lock (_lockObject)
            {
                return _nodes.Values.ToList();
            }
        }
    }
}
