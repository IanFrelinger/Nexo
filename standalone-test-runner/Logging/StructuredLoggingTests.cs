using System;
using System.Collections.Generic;

namespace StandaloneTestRunner
{
    public partial class StructuredLoggingTests
    {
        private readonly bool _verbose;

        public StructuredLoggingTests(bool verbose = false)
        {
            _verbose = verbose;
        }

        public List<TestInfo> DiscoverStructuredLoggingTests()
        {
            return new List<TestInfo>
            {
                new TestInfo(
                    "logging-structured",
                    "Structured Logging",
                    "Tests structured logging with parameters and scopes",
                    "Logging",
                    "Medium",
                    1,
                    3,
                    new[] { "logging", "structured", "parameters" }
                ),
                new TestInfo(
                    "logging-scopes",
                    "Logging Scopes",
                    "Tests logging scopes and context propagation",
                    "Logging",
                    "Medium",
                    1,
                    3,
                    new[] { "logging", "scopes", "context" }
                ),
                new TestInfo(
                    "logging-parameters",
                    "Logging Parameters",
                    "Tests parameterized logging with structured data",
                    "Logging",
                    "Medium",
                    1,
                    3,
                    new[] { "logging", "parameters", "structured-data" }
                ),
                new TestInfo(
                    "logging-correlation",
                    "Logging Correlation",
                    "Tests correlation IDs and request tracing",
                    "Logging",
                    "High",
                    2,
                    5,
                    new[] { "logging", "correlation", "tracing" }
                ),
                new TestInfo(
                    "logging-formatters",
                    "Logging Formatters",
                    "Tests different log message formatters",
                    "Logging",
                    "Medium",
                    1,
                    3,
                    new[] { "logging", "formatters", "formatting" }
                )
            };
        }
    }
}
