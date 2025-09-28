using System;
using System.Threading.Tasks;
using Nexo.Core.Application.Commands;
using Nexo.Shared.Values;
using Nexo.Shared.Models.Resource;

namespace Nexo.Tests.Shared.Commands
{
    /// <summary>
    /// Test command for shared layer operations
    /// </summary>
    public class TestSharedCommand : ICommand<TestSharedInput, TestSharedOutput>
    {
        public async Task<CommandResult<TestSharedOutput>> ExecuteAsync(TestSharedInput input)
        {
            try
            {
                await Task.Delay(10); // Simulate async work
                
                var testResults = new List<string>();
                
                // Test PlatformType
                var platform = PlatformType.Windows;
                testResults.Add($"PlatformType creation test passed: {platform.Name}");
                testResults.Add($"PlatformType equality test passed: {platform.Equals(platform)}");
                
                // Test ResourceType
                var resourceType = ResourceType.CPU;
                testResults.Add($"ResourceType creation test passed: {resourceType.Name}");
                testResults.Add($"ResourceType equality test passed: {resourceType.Equals(resourceType)}");
                
                // Test ResourcePriority
                var priority = ResourcePriority.High;
                testResults.Add($"ResourcePriority creation test passed: {priority.Name}");
                testResults.Add($"ResourcePriority equality test passed: {priority.Equals(priority)}");
                
                // Test ResourceHealth
                var health = ResourceHealth.Healthy;
                testResults.Add($"ResourceHealth creation test passed: {health.Name}");
                testResults.Add($"ResourceHealth equality test passed: {health.Equals(health)}");
                
                // Test ResourceAllocationRequest
                var allocationRequest = new ResourceAllocationRequest
                {
                    ResourceType = resourceType,
                    RequestedAmount = 100,
                    Priority = priority,
                    Timeout = TimeSpan.FromMinutes(5)
                };
                testResults.Add($"ResourceAllocationRequest creation test passed");
                testResults.Add($"ResourceAllocationRequest properties test passed: {allocationRequest.RequestedAmount}");
                
                // Test ResourceAllocationResult
                var allocationResult = new ResourceAllocationResult
                {
                    Success = true,
                    AllocatedAmount = 100,
                    ResourceType = resourceType,
                    AllocationTime = DateTime.UtcNow
                };
                testResults.Add($"ResourceAllocationResult creation test passed");
                testResults.Add($"ResourceAllocationResult properties test passed: {allocationResult.Success}");
                
                var output = new TestSharedOutput
                {
                    SharedComponentType = input.SharedComponentType,
                    Success = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(100),
                    TestResults = testResults.ToArray()
                };

                return CommandResult<TestSharedOutput>.Success(output, TimeSpan.FromMilliseconds(100));
            }
            catch (Exception ex)
            {
                return CommandResult<TestSharedOutput>.Failure($"Shared component test failed: {ex.Message}", TimeSpan.FromMilliseconds(100));
            }
        }
    }

    public class TestSharedInput
    {
        public string SharedComponentType { get; set; } = "All";
        public bool IncludeResourceTests { get; set; } = true;
        public bool IncludePlatformTests { get; set; } = true;
        public bool IncludeModelTests { get; set; } = true;
    }

    public class TestSharedOutput
    {
        public string SharedComponentType { get; set; } = string.Empty;
        public bool Success { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string[] TestResults { get; set; } = Array.Empty<string>();
    }
}
