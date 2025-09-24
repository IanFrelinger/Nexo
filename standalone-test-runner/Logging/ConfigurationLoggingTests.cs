using System;
using System.Collections.Generic;

namespace StandaloneTestRunner
{
    public class ConfigurationLoggingTests
    {
        private readonly bool _verbose;

        public ConfigurationLoggingTests(bool verbose = false)
        {
            _verbose = verbose;
        }

        public List<TestInfo> DiscoverConfigurationLoggingTests()
        {
            return new List<TestInfo>
            {
                new TestInfo(
                    "logging-configuration",
                    "Logging Configuration",
                    "Tests logging configuration and setup",
                    "Logging",
                    "High",
                    2,
                    5,
                    new[] { "logging", "configuration", "setup" }
                ),
                new TestInfo(
                    "logging-providers",
                    "Logging Providers",
                    "Tests different logging providers and sinks",
                    "Logging",
                    "Medium",
                    2,
                    5,
                    new[] { "logging", "providers", "sinks" }
                ),
                new TestInfo(
                    "logging-filters",
                    "Logging Filters",
                    "Tests logging filters and level configuration",
                    "Logging",
                    "Medium",
                    1,
                    3,
                    new[] { "logging", "filters", "level-configuration" }
                ),
                new TestInfo(
                    "logging-environments",
                    "Logging Environments",
                    "Tests logging configuration across environments",
                    "Logging",
                    "Medium",
                    2,
                    5,
                    new[] { "logging", "environments", "configuration" }
                ),
                new TestInfo(
                    "logging-dynamic-config",
                    "Dynamic Logging Configuration",
                    "Tests dynamic logging configuration changes",
                    "Logging",
                    "Medium",
                    2,
                    5,
                    new[] { "logging", "dynamic", "configuration-changes" }
                )
            };
        }
    }
}
