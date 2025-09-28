using System;
using System.Threading.Tasks;
using Nexo.Core.Application.Commands;
using Nexo.Core.Application.Commands.Project;

namespace Nexo.Tests.Core.Application.Commands
{
    /// <summary>
    /// Test command for project operations
    /// </summary>
    public class TestProjectCommand : ICommand<TestProjectInput, TestProjectOutput>
    {
        public async Task<CommandResult<TestProjectOutput>> ExecuteAsync(TestProjectInput input)
        {
            try
            {
                await Task.Delay(10); // Simulate async work
                
                var output = new TestProjectOutput
                {
                    ProjectName = input.ProjectName,
                    Success = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(100),
                    TestResults = new[]
                    {
                        "Project creation test passed",
                        "Project validation test passed",
                        "Project configuration test passed"
                    }
                };

                return CommandResult<TestProjectOutput>.Success(output, TimeSpan.FromMilliseconds(100));
            }
            catch (Exception ex)
            {
                return CommandResult<TestProjectOutput>.Failure($"Project test failed: {ex.Message}", TimeSpan.FromMilliseconds(100));
            }
        }
    }

    public class TestProjectInput
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectPath { get; set; } = string.Empty;
        public bool IncludeValidation { get; set; } = true;
        public bool IncludeConfiguration { get; set; } = true;
    }

    public class TestProjectOutput
    {
        public string ProjectName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string[] TestResults { get; set; } = Array.Empty<string>();
    }
}
