using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Models;

namespace Nexo.Feature.Factory.Testing.Progress
{
    /// <summary>
    /// Console-based progress reporter that provides real-time feedback during test execution.
    /// </summary>
    public sealed class ConsoleProgressReporter : IProgressReporter
    {
        private readonly ILogger<ConsoleProgressReporter> _logger;
        private readonly bool _verbose;
        private readonly object _lock = new object();
        private int _totalTests;
        private int _completedTests;
        private DateTimeOffset _startTime;
        private readonly List<TestProgressInfo> _testProgress = new();

        /// <summary>
        /// Initializes a new instance of the ConsoleProgressReporter class.
        /// </summary>
        public ConsoleProgressReporter(ILogger<ConsoleProgressReporter> logger, bool verbose = false)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _verbose = verbose;
        }

        /// <summary>
        /// Reports the start of test execution.
        /// </summary>
        public void ReportTestExecutionStart(int totalTests)
        {
            lock (_lock)
            {
                _totalTests = totalTests;
                _completedTests = 0;
                _startTime = DateTimeOffset.UtcNow;
                _testProgress.Clear();

                Console.WriteLine();
                Console.WriteLine("🚀 Starting Test Execution");
                Console.WriteLine("==========================");
                Console.WriteLine($"Total Tests: {totalTests}");
                Console.WriteLine($"Start Time: {_startTime:HH:mm:ss}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Reports the start of a specific test.
        /// </summary>
        public void ReportTestStart(string testId, string testName, int testIndex)
        {
            lock (_lock)
            {
                var progressInfo = new TestProgressInfo
                {
                    TestId = testId,
                    TestName = testName,
                    TestIndex = testIndex,
                    StartTime = DateTimeOffset.UtcNow,
                    Status = TestStatus.Running
                };

                _testProgress.Add(progressInfo);

                if (_verbose)
                {
                    Console.WriteLine($"▶️  [{testIndex + 1}/{_totalTests}] Starting: {testName}");
                }
                else
                {
                    UpdateProgressBar();
                }
            }
        }

        /// <summary>
        /// Reports the completion of a specific test.
        /// </summary>
        public void ReportTestComplete(string testId, string testName, bool result, TimeSpan duration, int testIndex)
        {
            lock (_lock)
            {
                _completedTests++;
                var progressInfo = _testProgress.FirstOrDefault(t => t.TestId == testId);
                if (progressInfo != null)
                {
                    progressInfo.Status = result ? TestStatus.Passed : TestStatus.Failed;
                    progressInfo.Duration = duration;
                    progressInfo.EndTime = DateTimeOffset.UtcNow;
                }

                var statusIcon = result ? "✅" : "❌";
                var statusText = result ? "PASSED" : "FAILED";
                var durationText = FormatDuration(duration);

                if (_verbose)
                {
                    Console.WriteLine($"{statusIcon} [{testIndex + 1}/{_totalTests}] {statusText}: {testName} ({durationText})");
                }
                else
                {
                    UpdateProgressBar();
                }

                // Show summary every 10 tests or at the end
                if (_completedTests % 10 == 0 || _completedTests == _totalTests)
                {
                    ShowProgressSummary();
                }
            }
        }

        /// <summary>
        /// Reports test execution progress.
        /// </summary>
        public void ReportProgress(int completedTests, int totalTests, TimeSpan elapsedTime, TimeSpan estimatedRemaining)
        {
            lock (_lock)
            {
                if (!_verbose)
                {
                    UpdateProgressBar();
                }
            }
        }

        /// <summary>
        /// Reports test execution completion.
        /// </summary>
        public void ReportTestExecutionComplete(TestExecutionSummary summary)
        {
            lock (_lock)
            {
                var endTime = DateTimeOffset.UtcNow;
                var totalDuration = endTime - _startTime;

                Console.WriteLine();
                Console.WriteLine("🏁 Test Execution Complete");
                Console.WriteLine("==========================");
                Console.WriteLine($"Total Duration: {FormatDuration(totalDuration)}");
                Console.WriteLine($"Success Rate: {summary.SuccessRate:F1}% ({summary.SuccessfulCommandCount}/{summary.TotalCommandCount})");
                Console.WriteLine($"Overall Status: {(summary.IsSuccess ? "✅ PASSED" : "❌ FAILED")}");

                // Show detailed results
                ShowDetailedResults(summary);

                // Show performance metrics
                ShowPerformanceMetrics(summary);

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Reports an error during test execution.
        /// </summary>
        public void ReportError(string testId, string error)
        {
            lock (_lock)
            {
                Console.WriteLine($"❌ Error in {testId}: {error}");
            }
        }

        /// <summary>
        /// Reports a warning during test execution.
        /// </summary>
        public void ReportWarning(string testId, string warning)
        {
            lock (_lock)
            {
                Console.WriteLine($"⚠️  Warning in {testId}: {warning}");
            }
        }

        /// <summary>
        /// Reports coverage information.
        /// </summary>
        public void ReportCoverage(TestCoverageInfo coverage)
        {
            lock (_lock)
            {
                Console.WriteLine();
                Console.WriteLine("📊 Test Coverage Report");
                Console.WriteLine("=======================");
                Console.WriteLine($"Overall Coverage: {coverage.OverallCoverage:F1}%");
                Console.WriteLine($"Line Coverage: {coverage.LineCoverage:F1}% ({coverage.CoveredLines}/{coverage.TotalLines})");
                Console.WriteLine($"Branch Coverage: {coverage.BranchCoverage:F1}% ({coverage.CoveredBranches}/{coverage.TotalBranches})");
                Console.WriteLine($"Method Coverage: {coverage.MethodCoverage:F1}% ({coverage.CoveredMethods}/{coverage.TotalMethods})");
                Console.WriteLine($"Class Coverage: {coverage.ClassCoverage:F1}% ({coverage.CoveredClasses}/{coverage.TotalClasses})");
                Console.WriteLine();

                // Show top files by coverage
                var topFiles = coverage.FileCoverage
                    .OrderByDescending(f => f.Value.LineCoverage)
                    .Take(5)
                    .ToList();

                if (topFiles.Any())
                {
                    Console.WriteLine("Top Files by Coverage:");
                    foreach (var file in topFiles)
                    {
                        Console.WriteLine($"  • {Path.GetFileName(file.Key)}: {file.Value.LineCoverage:F1}%");
                    }
                    Console.WriteLine();
                }

                // Show files with low coverage
                var lowCoverageFiles = coverage.FileCoverage
                    .Where(f => f.Value.LineCoverage < 80.0)
                    .OrderBy(f => f.Value.LineCoverage)
                    .Take(5)
                    .ToList();

                if (lowCoverageFiles.Any())
                {
                    Console.WriteLine("Files with Low Coverage (< 80%):");
                    foreach (var file in lowCoverageFiles)
                    {
                        Console.WriteLine($"  • {Path.GetFileName(file.Key)}: {file.Value.LineCoverage:F1}%");
                    }
                    Console.WriteLine();
                }
            }
        }

        private void UpdateProgressBar()
        {
            if (_totalTests == 0) return;

            var progress = (double)_completedTests / _totalTests;
            var progressBarLength = 30;
            var filledLength = (int)(progress * progressBarLength);
            var bar = new string('█', filledLength) + new string('░', progressBarLength - filledLength);
            var percentage = progress * 100;

            var elapsed = DateTimeOffset.UtcNow - _startTime;
            var estimatedTotal = _completedTests > 0 ? TimeSpan.FromTicks(elapsed.Ticks * _totalTests / _completedTests) : TimeSpan.Zero;
            var estimatedRemaining = estimatedTotal - elapsed;

            Console.Write($"\rProgress: [{bar}] {percentage:F1}% ({_completedTests}/{_totalTests}) | Elapsed: {FormatDuration(elapsed)} | ETA: {FormatDuration(estimatedRemaining)}");
        }

        private void ShowProgressSummary()
        {
            var passed = _testProgress.Count(t => t.Status == TestStatus.Passed);
            var failed = _testProgress.Count(t => t.Status == TestStatus.Failed);
            var running = _testProgress.Count(t => t.Status == TestStatus.Running);

            Console.WriteLine();
            Console.WriteLine($"📈 Progress Summary: ✅ {passed} passed, ❌ {failed} failed, ▶️  {running} running");
            Console.WriteLine();
        }

        private void ShowDetailedResults(TestExecutionSummary summary)
        {
            var passedTests = summary.CommandResults.Values.Where(r => r.ExecutionResult.IsSuccess).ToList();
            var failedTests = summary.CommandResults.Values.Where(r => !r.ExecutionResult.IsSuccess).ToList();

            if (failedTests.Any())
            {
                Console.WriteLine("❌ Failed Tests:");
                foreach (var test in failedTests)
                {
                    var duration = FormatDuration(test.ExecutionResult.Duration);
                    Console.WriteLine($"  • {test.Command.Name}: {test.ExecutionResult.ErrorMessage} ({duration})");
                }
                Console.WriteLine();
            }

            if (passedTests.Any())
            {
                Console.WriteLine("✅ Passed Tests:");
                foreach (var test in passedTests.Take(5)) // Show first 5 passed tests
                {
                    var duration = FormatDuration(test.ExecutionResult.Duration);
                    Console.WriteLine($"  • {test.Command.Name} ({duration})");
                }
                if (passedTests.Count > 5)
                {
                    Console.WriteLine($"  ... and {passedTests.Count - 5} more");
                }
                Console.WriteLine();
            }
        }

        private void ShowPerformanceMetrics(TestExecutionSummary summary)
        {
            var totalDuration = summary.EndTime - summary.StartTime;
            var avgTestDuration = summary.CommandResults.Values.Any() 
                ? TimeSpan.FromTicks((long)summary.CommandResults.Values.Average(r => r.ExecutionResult.Duration.Ticks))
                : TimeSpan.Zero;

            Console.WriteLine("⚡ Performance Metrics:");
            Console.WriteLine($"  • Total Duration: {FormatDuration(totalDuration)}");
            Console.WriteLine($"  • Average Test Duration: {FormatDuration(avgTestDuration)}");
            Console.WriteLine($"  • Tests per Second: {summary.TotalCommandCount / totalDuration.TotalSeconds:F2}");
            Console.WriteLine();
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalSeconds < 1)
                return $"{duration.TotalMilliseconds:F0}ms";
            if (duration.TotalMinutes < 1)
                return $"{duration.TotalSeconds:F1}s";
            if (duration.TotalHours < 1)
                return $"{duration.TotalMinutes:F1}m";
            return $"{duration.TotalHours:F1}h";
        }

        private class TestProgressInfo
        {
            public string TestId { get; set; } = string.Empty;
            public string TestName { get; set; } = string.Empty;
            public int TestIndex { get; set; }
            public DateTimeOffset StartTime { get; set; }
            public DateTimeOffset? EndTime { get; set; }
            public TimeSpan Duration { get; set; }
            public TestStatus Status { get; set; }
        }

        private enum TestStatus
        {
            Running,
            Passed,
            Failed
        }
    }
}
