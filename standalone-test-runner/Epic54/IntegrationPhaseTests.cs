using System;
using System.Collections.Generic;

namespace StandaloneTestRunner
{
    public partial class IntegrationPhaseTests
    {
        private readonly bool _verbose;

        public IntegrationPhaseTests(bool verbose = false)
        {
            _verbose = verbose;
        }

        public List<TestInfo> DiscoverIntegrationTests()
        {
            return new List<TestInfo>
            {
                new TestInfo(
                    "epic5_4-integration-api-connectivity",
                    "API Connectivity Integration",
                    "Tests API connectivity and communication",
                    "Integration",
                    "Critical",
                    3,
                    8,
                    new[] { "epic5_4", "integration", "api", "connectivity" }
                ),
                new TestInfo(
                    "epic5_4-integration-database-connectivity",
                    "Database Connectivity Integration",
                    "Tests database connectivity and operations",
                    "Integration",
                    "Critical",
                    3,
                    8,
                    new[] { "epic5_4", "integration", "database", "connectivity" }
                ),
                new TestInfo(
                    "epic5_4-integration-message-queue",
                    "Message Queue Integration",
                    "Tests message queue integration and processing",
                    "Integration",
                    "High",
                    2,
                    6,
                    new[] { "epic5_4", "integration", "message-queue" }
                ),
                new TestInfo(
                    "epic5_4-integration-external-services",
                    "External Services Integration",
                    "Tests integration with external services",
                    "Integration",
                    "High",
                    2,
                    6,
                    new[] { "epic5_4", "integration", "external-services" }
                ),
                new TestInfo(
                    "epic5_4-integration-data-synchronization",
                    "Data Synchronization Integration",
                    "Tests data synchronization between systems",
                    "Integration",
                    "High",
                    2,
                    6,
                    new[] { "epic5_4", "integration", "data-sync" }
                )
            };
        }
    }
}
