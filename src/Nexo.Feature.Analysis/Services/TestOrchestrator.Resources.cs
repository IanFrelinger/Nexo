using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Models;
using Nexo.Shared.Interfaces.Resource;

namespace Nexo.Feature.Analysis.Services
{
    public partial class TestOrchestrator
    {
        public async Task<ResourceUtilization> GetResourceUtilizationAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var cpuUsage = await _resourceMonitor.GetCpuUsageAsync(cancellationToken);
                var memoryInfo = await _resourceMonitor.GetMemoryInfoAsync(cancellationToken);
                var availableCores = Environment.ProcessorCount;

                // Calculate recommended parallelism based on available resources
                var recommendedParallelism = Math.Max(1, Math.Min(availableCores, 
                    (int)(availableCores * (1 - cpuUsage / 100.0))));

                return new ResourceUtilization
                {
                    CpuUsagePercent = cpuUsage,
                    MemoryUsageMB = memoryInfo.UsedBytes / (1024 * 1024),
                    AvailableMemoryMB = memoryInfo.AvailableBytes / (1024 * 1024),
                    AvailableCores = availableCores,
                    RecommendedMaxParallelism = recommendedParallelism,
                    IsResourceConstrained = cpuUsage > 80 || memoryInfo.UsagePercentage > 80
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resource utilization");
                return new ResourceUtilization
                {
                    CpuUsagePercent = 0,
                    MemoryUsageMB = 0,
                    AvailableMemoryMB = 0,
                    AvailableCores = Environment.ProcessorCount,
                    RecommendedMaxParallelism = Environment.ProcessorCount,
                    IsResourceConstrained = false
                };
            }
        }

        public TestOrchestrationValidation ValidateOptions(TestOrchestrationOptions options)
        {
            var validation = new TestOrchestrationValidation { IsValid = true };

            if (options.MaxParallelism <= 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("MaxParallelism must be greater than 0");
            }

            if (options.MaxParallelism > Environment.ProcessorCount * 2)
            {
                validation.Warnings.Add($"MaxParallelism ({options.MaxParallelism}) is higher than recommended ({Environment.ProcessorCount})");
            }

            if (options.MaxMemoryUsageMB <= 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("MaxMemoryUsageMB must be greater than 0");
            }

            if (options.MaxCpuUsagePercent <= 0 || options.MaxCpuUsagePercent > 100)
            {
                validation.IsValid = false;
                validation.Errors.Add("MaxCpuUsagePercent must be between 1 and 100");
            }

            if (options.TestTimeoutSeconds <= 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("TestTimeoutSeconds must be greater than 0");
            }

            if (options.MaxRetryAttempts < 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("MaxRetryAttempts must be non-negative");
            }

            return validation;
        }

        private void AdjustOptionsForResources(TestOrchestrationOptions options, ResourceUtilization utilization)
        {
            if (utilization.IsResourceConstrained)
            {
                var originalParallelism = options.MaxParallelism;
                options.MaxParallelism = Math.Min(options.MaxParallelism, utilization.RecommendedMaxParallelism);
                
                if (options.MaxParallelism != originalParallelism)
                {
                    _logger.LogWarning("Reduced max parallelism from {Original} to {Adjusted} due to resource constraints",
                        originalParallelism, options.MaxParallelism);
                }
            }
        }
    }
}
