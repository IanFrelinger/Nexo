using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Interface for real-time monitoring of test execution.
    /// </summary>
    public interface ITestMonitoringService
    {
        /// <summary>
        /// Starts monitoring a test execution.
        /// </summary>
        /// <param name="runId">Unique identifier for the test run.</param>
        /// <param name="environment">Environment being tested.</param>
        /// <param name="project">Project being tested.</param>
        /// <returns>Test execution monitor.</returns>
        TestExecutionMonitor StartMonitoring(string runId, string environment, string project);

        /// <summary>
        /// Gets the monitor for a specific test run.
        /// </summary>
        /// <param name="runId">Unique identifier for the test run.</param>
        /// <returns>Test execution monitor if found.</returns>
        TestExecutionMonitor GetMonitor(string runId);

        /// <summary>
        /// Gets all active monitors.
        /// </summary>
        /// <returns>List of active test execution monitors.</returns>
        List<TestExecutionMonitor> GetActiveMonitors();

        /// <summary>
        /// Stops monitoring a test execution and stores the results.
        /// </summary>
        /// <param name="runId">Unique identifier for the test run.</param>
        /// <returns>Task representing the async operation.</returns>
        Task StopMonitoringAsync(string runId);

        /// <summary>
        /// Gets real-time statistics for all active test executions.
        /// </summary>
        /// <returns>Real-time test execution statistics.</returns>
        TestExecutionStatistics GetRealTimeStatistics();

        /// <summary>
        /// Gets performance alerts for slow or problematic test executions.
        /// </summary>
        /// <returns>List of performance alerts.</returns>
        List<PerformanceAlert> GetPerformanceAlerts();
    }
}