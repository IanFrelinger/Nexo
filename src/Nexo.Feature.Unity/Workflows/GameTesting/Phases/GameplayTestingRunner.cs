using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.Workflows.GameTesting.Phases
{
    public class GameplayTestingRunner
    {
        private readonly IGameplayTester _gameplayTester;
        private readonly ILogger _logger;

        public GameplayTestingRunner(IGameplayTester gameplayTester, ILogger logger)
        {
            _gameplayTester = gameplayTester ?? throw new ArgumentNullException(nameof(gameplayTester));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<object> RunAsync(GameTestingWorkflowRequest request)
        {
            _logger.LogInformation("Phase 3: Running gameplay tests");
            var gpRequest = new GameplayTestRequest
            {
                ProjectPath = request.ProjectPath,
                TestScenarios = request.GameplayScenarios,
                PlayerProfiles = request.TestPlayerProfiles
            };
            return await _gameplayTester.RunGameplayTestsAsync(gpRequest);
        }
    }
}


