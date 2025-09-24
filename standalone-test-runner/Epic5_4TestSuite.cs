using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Orchestrator for Epic 5.4: Deployment & Integration test suite that delegates to specialized phase classes.
    /// </summary>
    public class Epic5_4TestSuite
    {
        private readonly bool _verbose;
        private readonly DeploymentPhaseTests _deploymentPhase;
        private readonly IntegrationPhaseTests _integrationPhase;
        private readonly MonitoringPhaseTests _monitoringPhase;
        private readonly SecurityPhaseTests _securityPhase;
        private readonly PerformancePhaseTests _performancePhase;

        public Epic5_4TestSuite(bool verbose = false)
        {
            _verbose = verbose;
            _deploymentPhase = new DeploymentPhaseTests(verbose);
            _integrationPhase = new IntegrationPhaseTests(verbose);
            _monitoringPhase = new MonitoringPhaseTests(verbose);
            _securityPhase = new SecurityPhaseTests(verbose);
            _performancePhase = new PerformancePhaseTests(verbose);
        }

        /// <summary>
        /// Discovers all Epic 5.4 related tests by delegating to specialized phase classes.
        /// </summary>
        public List<TestInfo> DiscoverEpic5_4Tests()
        {
            var allTests = new List<TestInfo>();
            
            allTests.AddRange(_deploymentPhase.DiscoverDeploymentTests());
            allTests.AddRange(_integrationPhase.DiscoverIntegrationTests());
            allTests.AddRange(_monitoringPhase.DiscoverMonitoringTests());
            allTests.AddRange(_securityPhase.DiscoverSecurityTests());
            allTests.AddRange(_performancePhase.DiscoverPerformanceTests());
            
            if (_verbose)
            {
                Console.WriteLine($"Discovered {allTests.Count} Epic 5.4 tests across all phases");
            }
            
            return allTests;
        }
    }
}
