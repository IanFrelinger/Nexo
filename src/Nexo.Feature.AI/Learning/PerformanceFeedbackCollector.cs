using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Agents.Specialized;

namespace Nexo.Feature.AI.Learning;

/// <summary>
/// Collects and records performance feedback for agent learning
/// </summary>
public class PerformanceFeedbackCollector : IPerformanceFeedbackCollector
{
    private readonly ILogger<PerformanceFeedbackCollector> _logger;
    private readonly Dictionary<string, Stopwatch> _activeRequests = new();
    
    public PerformanceFeedbackCollector(ILogger<PerformanceFeedbackCollector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<PerformanceMetrics> CollectMetricsAsync(string agentId, AgentRequest request, AgentResponse response)
    {
        try
        {
            _logger.LogDebug("Collecting performance metrics for agent {AgentId}", agentId);
            
            var metrics = new PerformanceMetrics
            {
                ExecutionTime = GetExecutionTime(agentId),
                MemoryUsage = GetCurrentMemoryUsage(),
                CpuUsage = await GetCurrentCpuUsageAsync(),
                Success = response.Success,
                ErrorMessage = response.ErrorMessage,
                AdditionalMetrics = new Dictionary<string, object>
                {
                    ["AgentId"] = agentId,
                    ["RequestSize"] = request.Input.Length,
                    ["ResponseSize"] = response.Result.Length,
                    ["Confidence"] = response.Confidence,
                    ["Timestamp"] = DateTime.UtcNow
                }
            };
            
            _logger.LogDebug("Collected metrics for agent {AgentId}: {ExecutionTime}ms, {MemoryUsage}MB, Success: {Success}", 
                agentId, metrics.ExecutionTime.TotalMilliseconds, metrics.MemoryUsage / 1024 / 1024, metrics.Success);
            
            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting performance metrics for agent {AgentId}", agentId);
            return new PerformanceMetrics
            {
                ExecutionTime = TimeSpan.Zero,
                MemoryUsage = 0,
                CpuUsage = 0.0,
                Success = false,
                ErrorMessage = $"Metrics collection failed: {ex.Message}"
            };
        }
    }
    
    public async Task RecordFeedbackAsync(string agentId, AgentRequest request, AgentResponse response, PerformanceMetrics metrics)
    {
        try
        {
            _logger.LogDebug("Recording feedback for agent {AgentId}", agentId);
            
            // In a real implementation, this would store the feedback in a database
            // For now, we'll just log it
            _logger.LogInformation("Agent {AgentId} feedback: Success={Success}, Confidence={Confidence}, ExecutionTime={ExecutionTime}ms", 
                agentId, response.Success, response.Confidence, metrics.ExecutionTime.TotalMilliseconds);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording feedback for agent {AgentId}", agentId);
        }
    }
    
    public void StartRequestTracking(string agentId)
    {
        if (!_activeRequests.ContainsKey(agentId))
        {
            _activeRequests[agentId] = Stopwatch.StartNew();
        }
    }
    
    public void StopRequestTracking(string agentId)
    {
        if (_activeRequests.ContainsKey(agentId))
        {
            _activeRequests[agentId].Stop();
        }
    }
    
    private TimeSpan GetExecutionTime(string agentId)
    {
        if (_activeRequests.TryGetValue(agentId, out var stopwatch))
        {
            var elapsed = stopwatch.Elapsed;
            _activeRequests.Remove(agentId);
            return elapsed;
        }
        
        return TimeSpan.Zero;
    }
    
    private long GetCurrentMemoryUsage()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return process.WorkingSet64;
        }
        catch
        {
            return 0;
        }
    }
    
    private async Task<double> GetCurrentCpuUsageAsync()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            
            await Task.Delay(100); // Small delay to measure CPU usage
            
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            
            return Math.Min(100.0, cpuUsageTotal * 100);
        }
        catch
        {
            return 0.0;
        }
    }
}
