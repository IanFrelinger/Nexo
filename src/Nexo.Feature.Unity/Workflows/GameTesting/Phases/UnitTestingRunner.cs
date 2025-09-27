using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.Workflows.GameTesting.Phases
{
    public class UnitTestingRunner
    {
        private readonly IUnityTestRunner _testRunner;
        private readonly ILogger _logger;

        public UnitTestingRunner(IUnityTestRunner testRunner, ILogger logger)
        {
            _testRunner = testRunner ?? throw new ArgumentNullException(nameof(testRunner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<object> RunAsync(GameTestingWorkflowRequest request)
        {
            _logger.LogInformation("Phase 1: Running Unity unit tests");
            return await _testRunner.RunUnityTestsAsync(request.ProjectPath);
        }
    }
}


