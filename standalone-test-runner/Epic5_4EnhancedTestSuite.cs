using System;
using System.Collections.Generic;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Orchestrator for Enhanced Epic 5.4 test suite that delegates to phase-specific providers.
    /// </summary>
    public partial class Epic5_4EnhancedTestSuite
    {
        private readonly bool _verbose;
        private readonly IEpic54Phase1RealImplementationTests _phase1;
        private readonly IEpic54Phase2DomainValidationTests _phase2;
        private readonly IEpic54Phase3ErrorHandlingTests _phase3;
        private readonly IEpic54Phase4SecurityTests _phase4;
        private readonly IEpic54Phase5PerformanceTests _phase5;

        public Epic5_4EnhancedTestSuite(
            IEpic54Phase1RealImplementationTests phase1,
            IEpic54Phase2DomainValidationTests phase2,
            IEpic54Phase3ErrorHandlingTests phase3,
            IEpic54Phase4SecurityTests phase4,
            IEpic54Phase5PerformanceTests phase5,
            bool verbose = false)
        {
            _phase1 = phase1 ?? throw new ArgumentNullException(nameof(phase1));
            _phase2 = phase2 ?? throw new ArgumentNullException(nameof(phase2));
            _phase3 = phase3 ?? throw new ArgumentNullException(nameof(phase3));
            _phase4 = phase4 ?? throw new ArgumentNullException(nameof(phase4));
            _phase5 = phase5 ?? throw new ArgumentNullException(nameof(phase5));
            _verbose = verbose;
        }

        /// <summary>
        /// Discovers all enhanced Epic 5.4 tests organized by phase.
        /// </summary>
        public List<TestInfo> DiscoverEnhancedEpic5_4Tests()
        {
            var enhancedTests = new List<TestInfo>();

            enhancedTests.AddRange(_phase1.GetTests());
            enhancedTests.AddRange(_phase2.GetTests());
            enhancedTests.AddRange(_phase3.GetTests());
            enhancedTests.AddRange(_phase4.GetTests());
            enhancedTests.AddRange(_phase5.GetTests());

            if (_verbose)
            {
                Console.WriteLine($"Discovered {enhancedTests.Count} enhanced Epic 5.4 tests");
            }

            return enhancedTests;
        }
    }

    public interface IEpic54Phase1RealImplementationTests { List<TestInfo> GetTests(); }
    public interface IEpic54Phase2DomainValidationTests { List<TestInfo> GetTests(); }
    public interface IEpic54Phase3ErrorHandlingTests { List<TestInfo> GetTests(); }
    public interface IEpic54Phase4SecurityTests { List<TestInfo> GetTests(); }
    public interface IEpic54Phase5PerformanceTests { List<TestInfo> GetTests(); }
}
