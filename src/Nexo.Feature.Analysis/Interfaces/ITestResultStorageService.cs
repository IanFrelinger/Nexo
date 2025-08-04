using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Interface for storing and retrieving test result data for historical analysis.
    /// </summary>
    public interface ITestResultStorageService
    {
        /// <summary>
        /// Stores a test result aggregation for historical tracking.
        /// </summary>
        /// <param name="result">The test result to store.</param>
        /// <returns>Task representing the async operation.</returns>
        Task StoreTestResultAsync(TestResultAggregation result);

        /// <summary>
        /// Retrieves test results for a specific time range.
        /// </summary>
        /// <param name="startDate">Start date for the range.</param>
        /// <param name="endDate">End date for the range.</param>
        /// <param name="environment">Optional environment filter.</param>
        /// <param name="project">Optional project filter.</param>
        /// <returns>List of test results in the specified range.</returns>
        Task<List<TestResultAggregation>> GetTestResultsAsync(
            DateTime startDate, 
            DateTime endDate, 
            string environment = null, 
            string project = null);

        /// <summary>
        /// Gets test result trends over time.
        /// </summary>
        /// <param name="days">Number of days to analyze.</param>
        /// <param name="environment">Optional environment filter.</param>
        /// <returns>Test result trends.</returns>
        Task<TestResultTrends> GetTestResultTrendsAsync(int days = 30, string environment = null);

        /// <summary>
        /// Gets performance metrics for test execution.
        /// </summary>
        /// <param name="days">Number of days to analyze.</param>
        /// <param name="environment">Optional environment filter.</param>
        /// <returns>Performance metrics.</returns>
        Task<TestPerformanceMetrics> GetPerformanceMetricsAsync(int days = 30, string environment = null);

        /// <summary>
        /// Cleans up old test result files.
        /// </summary>
        /// <param name="daysToKeep">Number of days of data to keep.</param>
        /// <returns>Number of files cleaned up.</returns>
        Task<int> CleanupOldResultsAsync(int daysToKeep = 90);
    }
}