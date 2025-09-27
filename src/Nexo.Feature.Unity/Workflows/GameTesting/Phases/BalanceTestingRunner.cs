using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.Workflows.GameTesting.Phases
{
    public partial class BalanceTestingRunner
    {
        private readonly IBalanceTester _balanceTester;
        private readonly ILogger _logger;

        public BalanceTestingRunner(IBalanceTester balanceTester, ILogger logger)
        {
            _balanceTester = balanceTester ?? throw new ArgumentNullException(nameof(balanceTester));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<object> RunAsync(GameTestingWorkflowRequest request)
        {
            _logger.LogInformation("Phase 4: Running balance tests");
            var balRequest = new BalanceTestRequest
            {
                ProjectPath = request.ProjectPath,
                BalanceScenarios = request.BalanceScenarios,
                SimulationCount = request.BalanceSimulationCount
            };
            return await _balanceTester.RunBalanceTestsAsync(balRequest);
        }
    }
}


