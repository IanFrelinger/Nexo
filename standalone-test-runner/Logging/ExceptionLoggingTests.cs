using System;
using System.Collections.Generic;

namespace StandaloneTestRunner
{
    public partial class ExceptionLoggingTests
    {
        private readonly bool _verbose;

        public ExceptionLoggingTests(bool verbose = false)
        {
            _verbose = verbose;
        }

        public List<TestInfo> DiscoverExceptionLoggingTests()
        {
            return new List<TestInfo>
            {
                new TestInfo(
                    "logging-exception",
                    "Exception Logging",
                    "Tests exception logging and error handling",
                    "Logging",
                    "High",
                    1,
                    3,
                    new[] { "logging", "exception", "error-handling" }
                ),
                new TestInfo(
                    "logging-error-context",
                    "Error Context Logging",
                    "Tests error context and stack trace logging",
                    "Logging",
                    "High",
                    1,
                    3,
                    new[] { "logging", "error-context", "stack-trace" }
                ),
                new TestInfo(
                    "logging-critical-errors",
                    "Critical Error Logging",
                    "Tests critical error logging and alerting",
                    "Logging",
                    "Critical",
                    2,
                    5,
                    new[] { "logging", "critical", "alerting" }
                ),
                new TestInfo(
                    "logging-exception-filters",
                    "Exception Filter Logging",
                    "Tests exception filtering and categorization",
                    "Logging",
                    "Medium",
                    1,
                    3,
                    new[] { "logging", "exception-filters", "categorization" }
                ),
                new TestInfo(
                    "logging-error-aggregation",
                    "Error Aggregation Logging",
                    "Tests error aggregation and reporting",
                    "Logging",
                    "Medium",
                    2,
                    5,
                    new[] { "logging", "error-aggregation", "reporting" }
                )
            };
        }
    }
}
