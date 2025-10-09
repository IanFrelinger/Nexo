using System;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;

namespace Nexo.Tests.Core.Application.Commands
{
    /// <summary>
    /// Test command for agent operations
    /// </summary>
    public class TestAgentCommand : ICommand<TestAgentInput, TestAgentOutput>
    {
        public async ValueTask<TestAgentOutput> ExecuteAsync(TestAgentInput input, CancellationToken ct = default)
        {
            await Task.Delay(10, ct); // Simulate async work
            
            return new TestAgentOutput
            {
                AgentName = input.AgentName,
                Success = true,
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                TestResults = new[]
                {
                    "Agent creation test passed",
                    "Agent initialization test passed",
                    "Agent capability test passed"
                }
            };
        }
    }

    public class TestAgentInput
    {
        public string AgentName { get; set; } = string.Empty;
        public string AgentType { get; set; } = string.Empty;
        public bool IncludeInitialization { get; set; } = true;
        public bool IncludeCapabilities { get; set; } = true;
    }

    public class TestAgentOutput
    {
        public string AgentName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string[] TestResults { get; set; } = Array.Empty<string>();
    }
}
