using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;
using Nexo.Feature.Unity.Workflows.GameTesting.Phases;
using Nexo.Feature.Unity.Workflows.GameTesting.Reporting;

namespace Nexo.Feature.Unity.Workflows.GameTesting.Core
{
    /// <summary>
    /// Orchestrator for automated game testing workflow delegating to specialized phases
    /// </summary>
    public partial class GameTestingWorkflow : IWorkflow
    {
        private readonly ILogger<GameTestingWorkflow> _logger;
        private readonly UnitTestingRunner _unitTestingRunner;
        private readonly PerformanceTestingRunner _performanceTestingRunner;
        private readonly GameplayTestingRunner _gameplayTestingRunner;
        private readonly BalanceTestingRunner _balanceTestingRunner;
        private readonly TestReportGenerator _reportGenerator;

        public GameTestingWorkflow(
            IUnityTestRunner testRunner,
            IPerformanceTester performanceTester,
            IGameplayTester gameplayTester,
            IBalanceTester balanceTester,
            ILogger<GameTestingWorkflow> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unitTestingRunner = new UnitTestingRunner(testRunner, logger);
            _performanceTestingRunner = new PerformanceTestingRunner(performanceTester, logger);
            _gameplayTestingRunner = new GameplayTestingRunner(gameplayTester, logger);
            _balanceTestingRunner = new BalanceTestingRunner(balanceTester, logger);
            _reportGenerator = new TestReportGenerator(logger);
        }

        public async Task<WorkflowResult> ExecuteAsync(GameTestingWorkflowRequest request)
        {
            _logger.LogInformation("Starting game testing workflow for project: {ProjectPath}", request.ProjectPath);

            var workflowResult = new WorkflowResult
            {
                WorkflowId = Guid.NewGuid().ToString(),
                StartTime = DateTime.UtcNow,
                Status = WorkflowStatus.Running
            };

            try
            {
                var unitResults = await _unitTestingRunner.RunAsync(request);
                workflowResult.AddStep("UnitTesting", unitResults);

                var perfResults = await _performanceTestingRunner.RunAsync(request);
                workflowResult.AddStep("PerformanceTesting", perfResults);

                if (request.RunGameplayTests)
                {
                    var gameplayResults = await _gameplayTestingRunner.RunAsync(request);
                    workflowResult.AddStep("GameplayTesting", gameplayResults);
                }

                if (request.TestBalance)
                {
                    var balanceResults = await _balanceTestingRunner.RunAsync(request);
                    workflowResult.AddStep("BalanceTesting", balanceResults);
                }

                workflowResult.FinalReport = await _reportGenerator.GenerateAsync(workflowResult, request);
                workflowResult.Status = WorkflowStatus.Completed;
                workflowResult.EndTime = DateTime.UtcNow;
                _logger.LogInformation("Game testing workflow completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Game testing workflow failed");
                workflowResult.Status = WorkflowStatus.Failed;
                workflowResult.ErrorMessage = ex.Message;
                workflowResult.EndTime = DateTime.UtcNow;
            }

            return workflowResult;
        }
    }
}


