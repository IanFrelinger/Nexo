using System;
using System.Collections.Generic;

namespace StandaloneTestRunner
{
    public class MonitoringPhaseTests
    {
        private readonly bool _verbose;

        public MonitoringPhaseTests(bool verbose = false)
        {
            _verbose = verbose;
        }

        public List<TestInfo> DiscoverMonitoringTests()
        {
            return new List<TestInfo>
            {
                new TestInfo(
                    "epic5_4-monitoring-health-checks",
                    "Health Check Monitoring",
                    "Tests application health check monitoring",
                    "Monitoring",
                    "Critical",
                    2,
                    5,
                    new[] { "epic5_4", "monitoring", "health-checks" }
                ),
                new TestInfo(
                    "epic5_4-monitoring-performance-metrics",
                    "Performance Metrics Monitoring",
                    "Tests performance metrics collection and analysis",
                    "Monitoring",
                    "High",
                    2,
                    5,
                    new[] { "epic5_4", "monitoring", "performance-metrics" }
                ),
                new TestInfo(
                    "epic5_4-monitoring-alerting",
                    "Alerting System Monitoring",
                    "Tests alerting system functionality",
                    "Monitoring",
                    "High",
                    2,
                    5,
                    new[] { "epic5_4", "monitoring", "alerting" }
                ),
                new TestInfo(
                    "epic5_4-monitoring-logging",
                    "Logging System Monitoring",
                    "Tests logging system monitoring and analysis",
                    "Monitoring",
                    "High",
                    2,
                    5,
                    new[] { "epic5_4", "monitoring", "logging" }
                ),
                new TestInfo(
                    "epic5_4-monitoring-dashboard",
                    "Dashboard Monitoring",
                    "Tests monitoring dashboard functionality",
                    "Monitoring",
                    "Medium",
                    1,
                    3,
                    new[] { "epic5_4", "monitoring", "dashboard" }
                )
            };
        }
    }
}
