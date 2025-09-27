using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.Workflows.GameTesting.Reporting
{
    public partial class TestReportGenerator
    {
        private readonly ILogger _logger;

        public TestReportGenerator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<GameTestReport> GenerateAsync(WorkflowResult workflowResult, GameTestingWorkflowRequest request)
        {
            _logger.LogInformation("Phase 5: Generating test report");
            var report = new GameTestReport
            {
                ProjectPath = request.ProjectPath,
                WorkflowId = workflowResult.WorkflowId,
                StartTime = workflowResult.StartTime,
                EndTime = workflowResult.EndTime,
                Summary = "Automated test report",
                Steps = workflowResult.Steps,
                Status = workflowResult.Status.ToString()
            };
            return Task.FromResult(report);
        }
    }
}


