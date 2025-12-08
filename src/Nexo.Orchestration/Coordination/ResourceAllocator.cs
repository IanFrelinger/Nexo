using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Coordination;

/// <summary>
/// Allocates resources (compute, context, memory) to agents based on priority and availability.
/// </summary>
public sealed class ResourceAllocator
{
    private readonly ILogger<ResourceAllocator> _logger;
    private readonly ResourceBudget _budget;
    private readonly ConcurrentDictionary<string, ResourceAllocation> _allocations = new();
    private int _allocatedComputeSeconds;
    private int _allocatedContextTokens;
    private int _allocatedMemoryMB;
    private readonly object _allocationLock = new();

    public ResourceAllocator(ILogger<ResourceAllocator> logger, ResourceBudget? budget = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _budget = budget ?? new ResourceBudget
        {
            MaxComputeSeconds = 3600,
            MaxContextTokens = 200000,
            MaxMemoryMB = 16384
        };
    }

    /// <summary>
    /// Allocates resources to an agent based on priority.
    /// </summary>
    public bool TryAllocate(AgentContainer container, out ResourceAllocation? allocation)
    {
        allocation = null;

        if (container == null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        var spec = container.Agent.Spec;
        var requirements = spec.ResourceRequirements;

        if (requirements == null)
        {
            // No requirements - allocate minimal resources
            allocation = new ResourceAllocation
            {
                AgentId = container.AgentId,
                AllocatedComputeSeconds = 0,
                AllocatedContextTokens = 0,
                AllocatedMemoryMB = 0
            };
            _allocations[container.AgentId] = allocation;
            return true;
        }

        var requestedCompute = requirements.EstimatedComputeSeconds ?? 0;
        var requestedContext = requirements.RequiredContextTokens ?? 0;
        var requestedMemory = requirements.RequiredMemoryMB ?? 0;

        // Check if allocation is possible (thread-safe)
        lock (_allocationLock)
        {
            var availableCompute = (_budget.MaxComputeSeconds ?? int.MaxValue) - _allocatedComputeSeconds;
            var availableContext = (_budget.MaxContextTokens ?? int.MaxValue) - _allocatedContextTokens;
            var availableMemory = (_budget.MaxMemoryMB ?? int.MaxValue) - _allocatedMemoryMB;

            if (requestedCompute > availableCompute ||
                requestedContext > availableContext ||
                requestedMemory > availableMemory)
            {
                _logger.LogWarning(
                    "Cannot allocate resources for agent {AgentId}: requested ({Compute}s, {Context} tokens, {Memory}MB) exceeds available ({AvailableCompute}s, {AvailableContext} tokens, {AvailableMemory}MB)",
                    container.AgentId, requestedCompute, requestedContext, requestedMemory,
                    availableCompute, availableContext, availableMemory);
                return false;
            }

            // Allocate resources
            allocation = new ResourceAllocation
            {
                AgentId = container.AgentId,
                AllocatedComputeSeconds = requestedCompute,
                AllocatedContextTokens = requestedContext,
                AllocatedMemoryMB = requestedMemory,
                Priority = spec.Priority
            };

            _allocations[container.AgentId] = allocation;
            _allocatedComputeSeconds += requestedCompute;
            _allocatedContextTokens += requestedContext;
            _allocatedMemoryMB += requestedMemory;
        }

        _logger.LogDebug("Allocated resources to agent {AgentId}: {Compute}s, {Context} tokens, {Memory}MB",
            container.AgentId, requestedCompute, requestedContext, requestedMemory);

        return true;
    }

    /// <summary>
    /// Reallocates resources when an agent completes (frees up resources).
    /// </summary>
    public void ReleaseResources(string agentId)
    {
        if (!_allocations.TryRemove(agentId, out var allocation))
        {
            return;
        }

        lock (_allocationLock)
        {
            _allocatedComputeSeconds -= allocation.AllocatedComputeSeconds;
            _allocatedContextTokens -= allocation.AllocatedContextTokens;
            _allocatedMemoryMB -= allocation.AllocatedMemoryMB;
        }

        _logger.LogDebug("Released resources from agent {AgentId}: {Compute}s, {Context} tokens, {Memory}MB",
            agentId, allocation.AllocatedComputeSeconds, allocation.AllocatedContextTokens, allocation.AllocatedMemoryMB);
    }

    /// <summary>
    /// Gets current resource usage statistics.
    /// </summary>
    public ResourceUsageReport GetUsageReport()
    {
        return new ResourceUsageReport
        {
            TotalAllocatedComputeSeconds = _allocatedComputeSeconds,
            TotalAllocatedContextTokens = _allocatedContextTokens,
            TotalAllocatedMemoryMB = _allocatedMemoryMB,
            MaxComputeSeconds = _budget.MaxComputeSeconds,
            MaxContextTokens = _budget.MaxContextTokens,
            MaxMemoryMB = _budget.MaxMemoryMB,
            UtilizationCompute = _budget.MaxComputeSeconds.HasValue
                ? (double)_allocatedComputeSeconds / _budget.MaxComputeSeconds.Value
                : 0.0,
            UtilizationContext = _budget.MaxContextTokens.HasValue
                ? (double)_allocatedContextTokens / _budget.MaxContextTokens.Value
                : 0.0,
            UtilizationMemory = _budget.MaxMemoryMB.HasValue
                ? (double)_allocatedMemoryMB / _budget.MaxMemoryMB.Value
                : 0.0
        };
    }

    /// <summary>
    /// Gets all current allocations.
    /// </summary>
    public IReadOnlyList<ResourceAllocation> GetAllocations()
    {
        return _allocations.Values.ToList();
    }
}

/// <summary>
/// Resource allocation for an agent.
/// </summary>
public sealed record ResourceAllocation
{
    public required string AgentId { get; init; }
    public int AllocatedComputeSeconds { get; init; }
    public int AllocatedContextTokens { get; init; }
    public int AllocatedMemoryMB { get; init; }
    public int Priority { get; init; }
}

/// <summary>
/// Resource usage report.
/// </summary>
public sealed record ResourceUsageReport
{
    public int TotalAllocatedComputeSeconds { get; init; }
    public int TotalAllocatedContextTokens { get; init; }
    public int TotalAllocatedMemoryMB { get; init; }
    public int? MaxComputeSeconds { get; init; }
    public int? MaxContextTokens { get; init; }
    public int? MaxMemoryMB { get; init; }
    public double UtilizationCompute { get; init; }
    public double UtilizationContext { get; init; }
    public double UtilizationMemory { get; init; }
}

