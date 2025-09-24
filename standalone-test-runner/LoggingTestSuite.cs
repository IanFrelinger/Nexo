using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Orchestrator for logging test suite that delegates to specialized test classes.
    /// </summary>
    public class LoggingTestSuite
    {
        private readonly bool _verbose;
        private readonly BasicLoggingTests _basicLoggingTests;
        private readonly StructuredLoggingTests _structuredLoggingTests;
        private readonly ExceptionLoggingTests _exceptionLoggingTests;
        private readonly PerformanceLoggingTests _performanceLoggingTests;
        private readonly ConfigurationLoggingTests _configurationLoggingTests;

        public LoggingTestSuite(bool verbose = false)
        {
            _verbose = verbose;
            _basicLoggingTests = new BasicLoggingTests(verbose);
            _structuredLoggingTests = new StructuredLoggingTests(verbose);
            _exceptionLoggingTests = new ExceptionLoggingTests(verbose);
            _performanceLoggingTests = new PerformanceLoggingTests(verbose);
            _configurationLoggingTests = new ConfigurationLoggingTests(verbose);
        }

        /// <summary>
        /// Discovers all logging-related tests by delegating to specialized test classes.
        /// </summary>
        public List<TestInfo> DiscoverLoggingTests()
        {
            var allTests = new List<TestInfo>();
            
            allTests.AddRange(_basicLoggingTests.DiscoverBasicLoggingTests());
            allTests.AddRange(_structuredLoggingTests.DiscoverStructuredLoggingTests());
            allTests.AddRange(_exceptionLoggingTests.DiscoverExceptionLoggingTests());
            allTests.AddRange(_performanceLoggingTests.DiscoverPerformanceLoggingTests());
            allTests.AddRange(_configurationLoggingTests.DiscoverConfigurationLoggingTests());
            
            if (_verbose)
            {
                Console.WriteLine($"Discovered {allTests.Count} logging tests across all categories");
            }
            
            return allTests;
        }
    }
}
