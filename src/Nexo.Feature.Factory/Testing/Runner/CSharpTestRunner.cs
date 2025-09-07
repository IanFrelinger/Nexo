using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Attributes;
using Nexo.Feature.Factory.Testing.Models;
using Nexo.Feature.Factory.Testing.Progress;
using Nexo.Feature.Factory.Testing.Coverage;
using Nexo.Feature.Factory.Testing.Timeout;
using Nexo.Feature.Factory.Testing.Commands;

namespace Nexo.Feature.Factory.Testing.Runner
{
    /// <summary>
    /// C#-based test runner that provides better control over test execution and timeout handling.
    /// </summary>
    public sealed class CSharpTestRunner : ITestRunner
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

        /// <summary>
        /// Runs all tests with the specified configuration.
        /// </summary>
        public async Task<TestExecutionSummary> RunAllTestsAsync(TestConfiguration configuration, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting C# test runner execution");

            var filter = new TestFilter();
            return await RunFilteredTestsAsync(configuration, filter, cancellationToken);
        }

        /// <summary>
        /// Runs tests matching the specified filter.
        /// </summary>
        public async Task<TestExecutionSummary> RunFilteredTestsAsync(TestConfiguration configuration, TestFilter filter, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Running filtered tests with C# test runner");

            var startTime = DateTimeOffset.UtcNow;
            var results = new Dictionary<string, TestCommandResult>();
            var sharedData = new Dictionary<string, object>();

            try
            {
                // Discover tests if not already done
                if (!_discoveredTests.Any())
                {
                    await DiscoverTestsAsync(cancellationToken);
                }

                // Filter tests
                var testsToRun = _discoveredTests.Where(filter.Matches).ToList();
                _logger.LogInformation("Found {TestCount} tests matching filter", testsToRun.Count);

                // Report test execution start
                _progressReporter.ReportTestExecutionStart(testsToRun.Count);

                // Calculate overall timeout
                var totalEstimatedDuration = TimeSpan.FromTicks(testsToRun.Sum(t => t.EstimatedDuration.Ticks));
                var overallTimeout = TimeSpan.FromMinutes(Math.Max(10, totalEstimatedDuration.TotalMinutes * 2));
                _logger.LogInformation("Overall test execution timeout: {OverallTimeout}", overallTimeout);

                // Create overall timeout cancellation token
                using var overallTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                overallTimeoutCts.CancelAfter(overallTimeout);

                // Execute tests with progress reporting
                for (int i = 0; i < testsToRun.Count; i++)
                {
                    var testInfo = testsToRun[i];
                    
                    // Report test start
                    _progressReporter.ReportTestStart(testInfo.TestId, testInfo.DisplayName, i);

                    var result = await ExecuteTestAsync(testInfo, configuration, sharedData, overallTimeoutCts.Token);
                    results[testInfo.TestId] = result;

                    // Report test completion
                    _progressReporter.ReportTestComplete(testInfo.TestId, testInfo.DisplayName, 
                        result.ExecutionResult.IsSuccess, result.ExecutionResult.Duration, i);

                    // Report progress
                    var elapsed = DateTimeOffset.UtcNow - startTime;
                    var estimatedRemaining = i > 0 ? TimeSpan.FromTicks(elapsed.Ticks * (testsToRun.Count - i) / i) : TimeSpan.Zero;
                    _progressReporter.ReportProgress(i + 1, testsToRun.Count, elapsed, estimatedRemaining);

                    // Add test results to shared data for dependent tests
                    sharedData[testInfo.TestId] = result;

                    if (!result.ExecutionResult.IsSuccess && testInfo.Priority == TestPriority.Critical)
                    {
                        _logger.LogError("Critical test failed: {TestId}, stopping execution", testInfo.TestId);
                        _progressReporter.ReportError(testInfo.TestId, "Critical test failed, stopping execution");
                        break;
                    }
                }

                var endTime = DateTimeOffset.UtcNow;
                var summary = new TestExecutionSummary(
                    startTime,
                    endTime,
                    results,
                    sharedData
                );

                // Report test execution completion
                _progressReporter.ReportTestExecutionComplete(summary);

                // Analyze and report coverage if enabled
                if (configuration.EnablePerformanceMonitoring)
                {
                    await AnalyzeAndReportCoverageAsync(configuration, cancellationToken);
                }

                _logger.LogInformation("C# test execution completed: {SuccessCount}/{TotalCount} successful", 
                    summary.SuccessfulCommandCount, summary.TotalCommandCount);

                return summary;
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                var endTime = DateTimeOffset.UtcNow;
                var totalDuration = endTime - startTime;
                var isTimeout = totalDuration >= TimeSpan.FromMinutes(10);
                var errorMessage = isTimeout 
                    ? $"C# test execution timed out after {totalDuration.TotalMinutes:F1} minutes"
                    : "C# test execution was cancelled";
                
                _logger.LogError(ex, "C# test execution failed: {Error}", errorMessage);
                
                var summary = new TestExecutionSummary(
                    startTime,
                    endTime,
                    results,
                    sharedData,
                    errorMessage
                );

                _progressReporter.ReportTestExecutionComplete(summary);
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "C# test execution failed");
                var endTime = DateTimeOffset.UtcNow;
                
                var summary = new TestExecutionSummary(
                    startTime,
                    endTime,
                    results,
                    sharedData,
                    ex.Message
                );

                _progressReporter.ReportTestExecutionComplete(summary);
                return summary;
            }
        }

        /// <summary>
        /// Discovers all available tests.
        /// </summary>
        public async Task<IEnumerable<TestInfo>> DiscoverTestsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Discovering C# tests");

            _discoveredTests.Clear();

            try
            {
                // Get all assemblies in the current domain
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !a.FullName?.StartsWith("System.") == true)
                    .ToList();

                foreach (var assembly in assemblies)
                {
                    await DiscoverTestsInAssemblyAsync(assembly, cancellationToken);
                }

                _logger.LogInformation("Discovered {TestCount} C# tests", _discoveredTests.Count);
                return _discoveredTests.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to discover C# tests");
                return _discoveredTests.ToList();
            }
        }

        /// <summary>
        /// Validates the test runner configuration.
        /// </summary>
        public async Task<TestValidationResult> ValidateConfigurationAsync(TestConfiguration configuration)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            try
            {
                if (configuration == null)
                {
                    errors.Add("Test configuration is null");
                    return new TestValidationResult(false, errors, warnings, TimeSpan.Zero);
                }

                // Validate timeout configuration
                if (configuration.DefaultTimeout <= TimeSpan.Zero)
                {
                    errors.Add("Default timeout must be greater than zero");
                }

                if (configuration.AiConnectivityTimeout <= TimeSpan.Zero)
                {
                    errors.Add("AI connectivity timeout must be greater than zero");
                }

                if (configuration.DomainAnalysisTimeout <= TimeSpan.Zero)
                {
                    errors.Add("Domain analysis timeout must be greater than zero");
                }

                if (configuration.CodeGenerationTimeout <= TimeSpan.Zero)
                {
                    errors.Add("Code generation timeout must be greater than zero");
                }

                if (configuration.EndToEndTimeout <= TimeSpan.Zero)
                {
                    errors.Add("End-to-end timeout must be greater than zero");
                }

                if (configuration.PerformanceTimeout <= TimeSpan.Zero)
                {
                    errors.Add("Performance timeout must be greater than zero");
                }

                // Validate output directory
                if (string.IsNullOrWhiteSpace(configuration.OutputDirectory))
                {
                    errors.Add("Output directory cannot be empty");
                }

                // Check if we can discover tests
                var discoveredTests = await DiscoverTestsAsync();
                if (!discoveredTests.Any())
                {
                    warnings.Add("No tests were discovered");
                }

                return new TestValidationResult(errors.Count == 0, errors, warnings, TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                errors.Add($"Validation error: {ex.Message}");
                return new TestValidationResult(false, errors, warnings, TimeSpan.Zero);
            }
        }

        private async Task DiscoverTestsInAssemblyAsync(Assembly assembly, CancellationToken cancellationToken)
        {
            try
            {
                var testClasses = assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                    .ToList();

                foreach (var testClass in testClasses)
                {
                    await DiscoverTestsInClassAsync(testClass, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to discover tests in assembly: {AssemblyName}", assembly.FullName);
            }
        }

        private Task DiscoverTestsInClassAsync(Type testClass, CancellationToken cancellationToken)
        {
            try
            {
                var testMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
                    .ToList();

                foreach (var method in testMethods)
                {
                    var testInfo = CreateTestInfo(method, testClass);
                    if (testInfo != null)
                    {
                        _discoveredTests.Add(testInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to discover tests in class: {ClassName}", testClass.Name);
            }
            
            return Task.CompletedTask;
        }

        private TestInfo? CreateTestInfo(MethodInfo method, Type testClass)
        {
            try
            {
                var testAttribute = method.GetCustomAttribute<TestAttribute>();
                if (testAttribute == null)
                    return null;

                var testClassAttribute = testClass.GetCustomAttribute<TestClassAttribute>();
                var category = GetTestCategory(testAttribute);
                var priority = testAttribute.Priority;
                var estimatedDuration = TimeSpan.FromSeconds(testAttribute.EstimatedDurationSeconds);
                var timeout = TimeSpan.FromSeconds(testAttribute.TimeoutSeconds);
                var dependencies = testAttribute.Dependencies.ToList();
                var tags = testAttribute.Tags.ToList();
                var isEnabled = testAttribute.IsEnabled && (testClassAttribute?.IsEnabled ?? true);
                var description = !string.IsNullOrEmpty(testAttribute.Description) 
                    ? testAttribute.Description 
                    : testClassAttribute?.Description ?? string.Empty;

                var testId = $"{testClass.Name}.{method.Name}";
                var displayName = !string.IsNullOrEmpty(testAttribute.DisplayName) 
                    ? testAttribute.DisplayName 
                    : $"{testClass.Name}.{method.Name}";

                return new TestInfo(
                    testId,
                    displayName,
                    method,
                    testClass,
                    category,
                    priority,
                    estimatedDuration,
                    timeout,
                    dependencies,
                    tags,
                    isEnabled,
                    description
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create test info for method: {MethodName}", method.Name);
                return null;
            }
        }

        private TestCategory GetTestCategory(TestAttribute testAttribute)
        {
            return testAttribute switch
            {
                AiConnectivityTestAttribute => TestCategory.Integration,
                DomainAnalysisTestAttribute => TestCategory.Functional,
                CodeGenerationTestAttribute => TestCategory.Functional,
                EndToEndTestAttribute => TestCategory.E2E,
                PerformanceTestAttribute => TestCategory.Performance,
                ValidationTestAttribute => TestCategory.Unit,
                _ => TestCategory.Functional
            };
        }

        private async Task<TestCommandResult> ExecuteTestAsync(TestInfo testInfo, TestConfiguration configuration, Dictionary<string, object> sharedData, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            var artifacts = new List<TestArtifact>();
            var outputData = new Dictionary<string, object>();

            try
            {
                _logger.LogInformation("Executing C# test: {TestId} with timeout: {Timeout}", 
                    testInfo.TestId, testInfo.Timeout);

                // Create test instance
                var testInstance = Activator.CreateInstance(testInfo.TestClass);
                if (testInstance == null)
                {
                    throw new InvalidOperationException($"Failed to create instance of {testInfo.TestClass.Name}");
                }

                // Inject services if the test class has a constructor that takes IServiceProvider
                var constructor = testInfo.TestClass.GetConstructor(new[] { typeof(IServiceProvider) });
                if (constructor != null)
                {
                    testInstance = Activator.CreateInstance(testInfo.TestClass, _serviceProvider);
                }

                // Run setup methods
                if (testInstance != null)
                {
                    await RunSetupMethodsAsync(testInstance, testInfo.TestClass, cancellationToken);
                }

                // Execute the test method with robust timeout monitoring
                var testExecutionTask = testInstance != null 
                    ? ExecuteTestMethodAsync(testInstance, testInfo.Method, outputData, artifacts, cancellationToken)
                    : Task.FromResult(false);
                var timeoutResult = await _timeoutManager.MonitorTestExecutionAsync(
                    testInfo.TestId,
                    testExecutionTask,
                    testInfo.Timeout,
                    cancellationToken);

                // Run cleanup methods
                if (testInstance != null)
                {
                    await RunCleanupMethodsAsync(testInstance, testInfo.TestClass, cancellationToken);
                }

                var endTime = DateTimeOffset.UtcNow;
                var duration = endTime - startTime;

                _logger.LogInformation("C# test {TestId} completed: {IsSuccess} ({Duration}ms)", 
                    testInfo.TestId, timeoutResult.IsSuccess, duration.TotalMilliseconds);

                // Add timeout information to output data
                outputData["TimeoutOccurred"] = timeoutResult.IsTimeout;
                outputData["ForceCancelled"] = timeoutResult.IsForceCancelled;
                outputData["CancellationReason"] = timeoutResult.CancellationReason ?? "Unknown";
                outputData["ActualDuration"] = duration;
                outputData["TimeoutDuration"] = testInfo.Timeout;

                var command = new SimpleTestCommand(
                    testInfo.TestId, 
                    testInfo.DisplayName, 
                    testInfo.Description,
                    testInfo.Category, 
                    testInfo.Priority, 
                    testInfo.EstimatedDuration,
                    logger: _logger);
                
                var validationResult = new TestValidationResult(true, new List<string>(), new List<string>(), TimeSpan.Zero);
                var executionResult = new Nexo.Feature.Factory.Testing.Models.TestExecutionResult(
                    timeoutResult.IsSuccess,
                    duration,
                    timeoutResult.ErrorMessage,
                    outputData,
                    new TestPerformanceMetrics(0, 0, 0, TimeSpan.Zero, artifacts.Count, 0),
                    artifacts
                );
                var cleanupResult = new TestCleanupResult(true, TimeSpan.Zero, null, 0);
                
                return new TestCommandResult(
                    command,
                    validationResult,
                    executionResult,
                    cleanupResult,
                    timeoutResult.IsSuccess
                );
            }
            catch (Exception ex)
            {
                var endTime = DateTimeOffset.UtcNow;
                var duration = endTime - startTime;
                _logger.LogError(ex, "C# test {TestId} failed", testInfo.TestId);
                
                // Add error information to output data
                outputData["Exception"] = ex.Message;
                outputData["ActualDuration"] = duration;
                outputData["TimeoutDuration"] = testInfo.Timeout;
                
                var command = new SimpleTestCommand(
                    testInfo.TestId, 
                    testInfo.DisplayName, 
                    testInfo.Description,
                    testInfo.Category, 
                    testInfo.Priority, 
                    testInfo.EstimatedDuration,
                    logger: _logger);
                
                var validationResult = new TestValidationResult(false, new[] { ex.Message }, new List<string>(), TimeSpan.Zero);
                var executionResult = new Nexo.Feature.Factory.Testing.Models.TestExecutionResult(
                    false,
                    duration,
                    ex.Message,
                    outputData,
                    new TestPerformanceMetrics(0, 0, 0, TimeSpan.Zero, artifacts.Count, 0),
                    artifacts
                );
                var cleanupResult = new TestCleanupResult(true, TimeSpan.Zero, null, 0);
                
                return new TestCommandResult(
                    command,
                    validationResult,
                    executionResult,
                    cleanupResult,
                    false
                );
            }
        }

        private async Task RunSetupMethodsAsync(object testInstance, Type testClass, CancellationToken cancellationToken)
        {
            var setupMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestSetupAttribute>() != null)
                .ToList();

            foreach (var method in setupMethods)
            {
                try
                {
                    var result = method.Invoke(testInstance, null);
                    if (result is Task task)
                    {
                        await task;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Setup method {MethodName} failed", method.Name);
                }
            }
        }

        private async Task RunCleanupMethodsAsync(object testInstance, Type testClass, CancellationToken cancellationToken)
        {
            var cleanupMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestCleanupAttribute>() != null)
                .ToList();

            foreach (var method in cleanupMethods)
            {
                try
                {
                    var result = method.Invoke(testInstance, null);
                    if (result is Task task)
                    {
                        await task;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cleanup method {MethodName} failed", method.Name);
                }
            }
        }

        private async Task<bool> ExecuteTestMethodAsync(object testInstance, MethodInfo method, Dictionary<string, object> outputData, List<TestArtifact> artifacts, CancellationToken cancellationToken)
        {
            try
            {
                var result = method.Invoke(testInstance, null);
                
                if (result is Task<bool> boolTask)
                {
                    return await boolTask;
                }
                else if (result is Task task)
                {
                    await task;
                    return true;
                }
                else if (result is bool boolResult)
                {
                    return boolResult;
                }
                else
                {
                    return true; // Assume success if no return value
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test method {MethodName} threw an exception", method.Name);
                outputData["Exception"] = ex.Message;
                return false;
            }
        }

        private async Task AnalyzeAndReportCoverageAsync(TestConfiguration configuration, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Analyzing test coverage...");

                // Get source assemblies from the current domain
                var sourceAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && 
                               !a.FullName?.StartsWith("System.") == true &&
                               !a.FullName?.StartsWith("Microsoft.") == true &&
                               !a.FullName?.StartsWith("Nexo.Feature.Factory.Testing") == true)
                    .Select(a => a.Location)
                    .Where(File.Exists)
                    .ToList();

                // Get test assemblies
                var testAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && a.FullName?.Contains("Test") == true)
                    .Select(a => a.Location)
                    .Where(File.Exists)
                    .ToList();

                if (sourceAssemblies.Any() && testAssemblies.Any())
                {
                    var coverage = await _coverageAnalyzer.AnalyzeCoverageAsync(
                        sourceAssemblies, testAssemblies, cancellationToken);

                    _progressReporter.ReportCoverage(coverage);

                    // Generate coverage reports if output directory is specified
                    if (!string.IsNullOrEmpty(configuration.OutputDirectory))
                    {
                        var outputDir = configuration.OutputDirectory;
                        Directory.CreateDirectory(outputDir);

                        // Generate multiple report formats
                        await _coverageAnalyzer.GenerateCoverageReportAsync(
                            coverage, Path.Combine(outputDir, "coverage.html"), 
                            CoverageReportFormat.Html, cancellationToken);

                        await _coverageAnalyzer.GenerateCoverageReportAsync(
                            coverage, Path.Combine(outputDir, "coverage.json"), 
                            CoverageReportFormat.Json, cancellationToken);

                        await _coverageAnalyzer.GenerateCoverageReportAsync(
                            coverage, Path.Combine(outputDir, "coverage.md"), 
                            CoverageReportFormat.Markdown, cancellationToken);

                        _logger.LogInformation("Coverage reports generated in: {OutputDirectory}", outputDir);
                    }
                }
                else
                {
                    _logger.LogWarning("No source or test assemblies found for coverage analysis");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze test coverage");
                _progressReporter.ReportWarning("Coverage Analysis", $"Failed to analyze coverage: {ex.Message}");
            }
        }
    }
}
