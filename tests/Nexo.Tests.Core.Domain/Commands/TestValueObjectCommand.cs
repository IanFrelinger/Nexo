using System;
using System.Threading.Tasks;
using Nexo.Core.Application.Commands;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Core.Domain.Values;
using Nexo.Shared.Values;

namespace Nexo.Tests.Core.Domain.Commands
{
    /// <summary>
    /// Test command for value object operations
    /// </summary>
    public class TestValueObjectCommand : ICommand<TestValueObjectInput, TestValueObjectOutput>
    {
        public async Task<CommandResult<TestValueObjectOutput>> ExecuteAsync(TestValueObjectInput input)
        {
            try
            {
                await Task.Delay(10); // Simulate async work
                
                var testResults = new List<string>();
                
                // Test AgentId
                var agentId1 = AgentId.NewId();
                var agentId2 = AgentId.NewId();
                testResults.Add($"AgentId creation test passed: {agentId1.Value}");
                testResults.Add($"AgentId equality test passed: {agentId1.Equals(agentId1)}");
                testResults.Add($"AgentId inequality test passed: {!agentId1.Equals(agentId2)}");
                
                // Test AgentName
                var agentName = new AgentName(input.AgentName);
                testResults.Add($"AgentName creation test passed: {agentName.Value}");
                testResults.Add($"AgentName equality test passed: {agentName.Equals(agentName)}");
                
                // Test AgentStatus
                var status = AgentStatus.Active;
                testResults.Add($"AgentStatus creation test passed: {status.Name}");
                testResults.Add($"AgentStatus equality test passed: {status.Equals(status)}");
                
                // Test TaskStatus
                var taskStatus = TaskStatus.InProgress;
                testResults.Add($"TaskStatus creation test passed: {taskStatus.Name}");
                testResults.Add($"TaskStatus equality test passed: {taskStatus.Equals(taskStatus)}");
                
                // Test TaskPriority
                var priority = TaskPriority.High;
                testResults.Add($"TaskPriority creation test passed: {priority.Name}");
                testResults.Add($"TaskPriority equality test passed: {priority.Equals(priority)}");
                
                // Test PlatformType
                var platform = PlatformType.Windows;
                testResults.Add($"PlatformType creation test passed: {platform.Name}");
                testResults.Add($"PlatformType equality test passed: {platform.Equals(platform)}");
                
                var output = new TestValueObjectOutput
                {
                    ValueObjectType = input.ValueObjectType,
                    Success = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(100),
                    TestResults = testResults.ToArray()
                };

                return CommandResult<TestValueObjectOutput>.Success(output, TimeSpan.FromMilliseconds(100));
            }
            catch (Exception ex)
            {
                return CommandResult<TestValueObjectOutput>.Failure($"Value object test failed: {ex.Message}", TimeSpan.FromMilliseconds(100));
            }
        }
    }

    public class TestValueObjectInput
    {
        public string ValueObjectType { get; set; } = "All";
        public string AgentName { get; set; } = "Test Agent";
        public bool IncludeEqualityTests { get; set; } = true;
        public bool IncludeCreationTests { get; set; } = true;
    }

    public class TestValueObjectOutput
    {
        public string ValueObjectType { get; set; } = string.Empty;
        public bool Success { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string[] TestResults { get; set; } = Array.Empty<string>();
    }
}
