using System;
using System.Collections.Generic;

namespace StandaloneTestRunner
{
    public class PerformancePhaseTests
    {
        private readonly bool _verbose;

        public PerformancePhaseTests(bool verbose = false)
        {
            _verbose = verbose;
        }

        public List<TestInfo> DiscoverPerformanceTests()
        {
            return new List<TestInfo>
            {
                new TestInfo(
                    "epic5_4-performance-load-testing",
                    "Load Testing Performance",
                    "Tests application load testing and performance under load",
                    "Performance",
                    "High",
                    5,
                    15,
                    new[] { "epic5_4", "performance", "load-testing" }
                ),
                new TestInfo(
                    "epic5_4-performance-stress-testing",
                    "Stress Testing Performance",
                    "Tests application stress testing and breaking points",
                    "Performance",
                    "High",
                    5,
                    15,
                    new[] { "epic5_4", "performance", "stress-testing" }
                ),
                new TestInfo(
                    "epic5_4-performance-scalability",
                    "Scalability Performance",
                    "Tests application scalability and resource utilization",
                    "Performance",
                    "High",
                    3,
                    10,
                    new[] { "epic5_4", "performance", "scalability" }
                ),
                new TestInfo(
                    "epic5_4-performance-memory-usage",
                    "Memory Usage Performance",
                    "Tests memory usage and optimization",
                    "Performance",
                    "Medium",
                    2,
                    8,
                    new[] { "epic5_4", "performance", "memory-usage" }
                ),
                new TestInfo(
                    "epic5_4-performance-response-time",
                    "Response Time Performance",
                    "Tests response time and latency optimization",
                    "Performance",
                    "Medium",
                    2,
                    8,
                    new[] { "epic5_4", "performance", "response-time" }
                )
            };
        }
    }
}
