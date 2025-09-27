using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Test aggregator that takes a collection of tests and iterates through each one.
    /// Provides structured test execution with comprehensive reporting.
    /// Handles test discovery, execution, filtering, and result aggregation.
    /// </summary>
    public partial class TestAggregator
    {
        private readonly bool _forceTimeout;
        private readonly int _heartbeatInterval;
        private readonly int _processTimeout;
        private readonly bool _verbose;
        private readonly List<TestInfo> _tests;

        public TestAggregator(bool forceTimeout = false, int heartbeatInterval = 2, int processTimeout = 1, bool verbose = false)
        {
            _forceTimeout = forceTimeout;
            _heartbeatInterval = heartbeatInterval;
            _processTimeout = processTimeout;
            _verbose = verbose;
            _tests = new List<TestInfo>();
        }
    }

    /// <summary>
    /// Result of test aggregation execution.
    /// </summary>
    public record TestAggregationResult(
        int TotalTests,
        int PassedTests,
        int FailedTests,
        int SkippedTests,
        TimeSpan TotalDuration,
        TimeSpan TotalExecutionTime,
        double AverageDuration,
        List<TestResult> TestResults,
        List<TestInfo> Tests,
        TestAggregationMetrics Metrics
    );

    /// <summary>
    /// Metrics for test aggregation.
    /// </summary>
    public record TestAggregationMetrics(
        int TotalTests,
        int PassedTests,
        int FailedTests,
        int SkippedTests,
        TimeSpan TotalDuration,
        int SlowTests,
        int FastTests,
        Dictionary<string, int> TestsByCategory,
        Dictionary<string, int> TestsByPriority
    );
}
