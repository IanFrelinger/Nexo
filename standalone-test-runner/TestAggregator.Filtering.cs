using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Test filtering functionality for TestAggregator.
    /// Handles test filtering by category and priority.
    /// </summary>
    public partial class TestAggregator
    {
        /// <summary>
        /// Runs tests filtered by category.
        /// </summary>
        public async Task<TestAggregationResult> RunTestsByCategoryAsync(string category, bool progress = false)
        {
            var filteredTests = _tests.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (filteredTests.Count == 0)
            {
                throw new InvalidOperationException($"No tests found for category: {category}");
            }

            if (_verbose)
            {
                Console.WriteLine($"Running {filteredTests.Count} tests for category: {category}");
            }

            // Temporarily replace tests with filtered ones
            var originalTests = new List<TestInfo>(_tests);
            _tests.Clear();
            _tests.AddRange(filteredTests);

            try
            {
                return await RunAllTestsAsync(progress);
            }
            finally
            {
                // Restore original tests
                _tests.Clear();
                _tests.AddRange(originalTests);
            }
        }

        /// <summary>
        /// Runs tests filtered by priority.
        /// </summary>
        public async Task<TestAggregationResult> RunTestsByPriorityAsync(string priority, bool progress = false)
        {
            var filteredTests = _tests.Where(t => t.Priority.Equals(priority, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (filteredTests.Count == 0)
            {
                throw new InvalidOperationException($"No tests found for priority: {priority}");
            }

            if (_verbose)
            {
                Console.WriteLine($"Running {filteredTests.Count} tests for priority: {priority}");
            }

            // Temporarily replace tests with filtered ones
            var originalTests = new List<TestInfo>(_tests);
            _tests.Clear();
            _tests.AddRange(filteredTests);

            try
            {
                return await RunAllTestsAsync(progress);
            }
            finally
            {
                // Restore original tests
                _tests.Clear();
                _tests.AddRange(originalTests);
            }
        }
    }
}
