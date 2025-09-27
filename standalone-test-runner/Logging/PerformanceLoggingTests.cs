using System;
using System.Collections.Generic;

namespace StandaloneTestRunner
{
    public partial class PerformanceLoggingTests
    {
        private readonly bool _verbose;

        public PerformanceLoggingTests(bool verbose = false)
        {
            _verbose = verbose;
        }

        public List<TestInfo> DiscoverPerformanceLoggingTests()
        {
            return new List<TestInfo>
            {
                new TestInfo(
                    "logging-performance",
                    "Logging Performance",
                    "Tests logging performance and throughput",
                    "Logging",
                    "High",
                    3,
                    8,
                    new[] { "logging", "performance", "throughput" }
                ),
                new TestInfo(
                    "logging-async",
                    "Async Logging",
                    "Tests asynchronous logging operations",
                    "Logging",
                    "High",
                    2,
                    5,
                    new[] { "logging", "async", "operations" }
                ),
                new TestInfo(
                    "logging-buffering",
                    "Logging Buffering",
                    "Tests log message buffering and batching",
                    "Logging",
                    "Medium",
                    2,
                    5,
                    new[] { "logging", "buffering", "batching" }
                ),
                new TestInfo(
                    "logging-memory-usage",
                    "Logging Memory Usage",
                    "Tests memory usage and garbage collection impact",
                    "Logging",
                    "Medium",
                    2,
                    5,
                    new[] { "logging", "memory", "gc" }
                ),
                new TestInfo(
                    "logging-throughput",
                    "Logging Throughput",
                    "Tests high-volume logging throughput",
                    "Logging",
                    "High",
                    3,
                    8,
                    new[] { "logging", "throughput", "high-volume" }
                )
            };
        }
    }
}
