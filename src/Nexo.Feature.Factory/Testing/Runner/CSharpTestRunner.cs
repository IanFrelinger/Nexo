using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Attributes;
using Nexo.Feature.Factory.Testing.Models;
using Nexo.Feature.Factory.Testing.Progress;
using Nexo.Feature.Factory.Testing.Coverage;
using Nexo.Feature.Factory.Testing.Timeout;

namespace Nexo.Feature.Factory.Testing.Runner
{
    /// <summary>
    /// C#-based test runner that provides better control over test execution and timeout handling.
    /// </summary>
    public sealed partial class CSharpTestRunner : ITestRunner
    {
        private readonly ILogger<CSharpTestRunner> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IProgressReporter _progressReporter;
        private readonly ITestCoverageAnalyzer _coverageAnalyzer;
        private readonly ITimeoutManager _timeoutManager;
        private readonly List<TestInfo> _discoveredTests = new();

        /// <summary>
        /// Gets the name of the test runner.
        /// </summary>
        public string Name => "C# Test Runner";

        /// <summary>
        /// Gets the version of the test runner.
        /// </summary>
        public string Version => "1.0.0";

        /// <summary>
        /// Initializes a new instance of the CSharpTestRunner class.
        /// </summary>
        public CSharpTestRunner(
            ILogger<CSharpTestRunner> logger, 
            IServiceProvider serviceProvider,
            IProgressReporter? progressReporter = null,
            ITestCoverageAnalyzer? coverageAnalyzer = null,
            ITimeoutManager? timeoutManager = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _progressReporter = progressReporter ?? new ConsoleProgressReporter(
                serviceProvider.GetRequiredService<ILogger<ConsoleProgressReporter>>());
            _coverageAnalyzer = coverageAnalyzer ?? new ReflectionBasedCoverageAnalyzer(
                serviceProvider.GetRequiredService<ILogger<ReflectionBasedCoverageAnalyzer>>());
            _timeoutManager = timeoutManager ?? new RobustTimeoutManager(
                serviceProvider.GetRequiredService<ILogger<RobustTimeoutManager>>());
        }

    }
}
