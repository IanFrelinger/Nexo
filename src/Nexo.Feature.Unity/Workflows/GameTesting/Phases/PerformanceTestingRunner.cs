using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.Workflows.GameTesting.Phases
{
    public partial class PerformanceTestingRunner
    {
        private readonly IPerformanceTester _performanceTester;
        private readonly ILogger _logger;

        public PerformanceTestingRunner(IPerformanceTester performanceTester, ILogger logger)
        {
            _performanceTester = performanceTester ?? throw new ArgumentNullException(nameof(performanceTester));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<object> RunAsync(GameTestingWorkflowRequest request)
        {
            _logger.LogInformation("Phase 2: Running performance tests");
            var perfRequest = new PerformanceTestRequest
            {
                ProjectPath = request.ProjectPath,
                TestScenes = request.TestScenes,
                TargetFrameRate = request.TargetFrameRate,
                TestDuration = request.TestDuration
            };
            return await _performanceTester.RunPerformanceTestsAsync(perfRequest);
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.Workflows.GameTesting.Phases
{
    public partial class PerformanceTestingRunner
    {
        private readonly IPerformanceTester _performanceTester;
        private readonly ILogger _logger;

        public PerformanceTestingRunner(IPerformanceTester performanceTester, ILogger logger)
        {
            _performanceTester = performanceTester ?? throw new ArgumentNullException(nameof(performanceTester));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<object> RunAsync(GameTestingWorkflowRequest request)
        {
            _logger.LogInformation("Phase 2: Running performance tests");
            var perfRequest = new PerformanceTestRequest
            {
                ProjectPath = request.ProjectPath,
                TestScenes = request.TestScenes,
                TargetFrameRate = request.TargetFrameRate,
                TestDuration = request.TestDuration
            };
            return await _performanceTester.RunPerformanceTestsAsync(perfRequest);
        }
    }
}


