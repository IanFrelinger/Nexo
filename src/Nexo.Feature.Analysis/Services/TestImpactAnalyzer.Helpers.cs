using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Helper methods and utilities
    /// </summary>
    public partial class TestImpactAnalyzer
    {
        private string GenerateReasoning(List<string> changedFiles, List<string> affectedTests, List<string> allTests)
        {
            var ratio = allTests.Count > 0 ? (double)affectedTests.Count / allTests.Count : 0.0;
            
            if (ratio < 0.1)
                return $"Smart selection: {affectedTests.Count} tests selected out of {allTests.Count} total ({(ratio * 100):F1}% reduction)";
            else if (ratio < 0.5)
                return $"Moderate selection: {affectedTests.Count} tests selected out of {allTests.Count} total ({(ratio * 100):F1}% of tests)";
            else
                return $"Broad selection: {affectedTests.Count} tests selected out of {allTests.Count} total ({(ratio * 100):F1}% of tests) - consider running all tests";
        }

        private List<string> GenerateWarnings(List<string> changedFiles, List<string> affectedTests, List<string> allTests)
        {
            var warnings = new List<string>();

            if (affectedTests.Count == 0 && changedFiles.Any())
            {
                warnings.Add("No tests found for changed files - consider running all tests");
            }

            if (affectedTests.Count > allTests.Count * 0.8)
            {
                warnings.Add("High percentage of tests selected - consider running all tests for efficiency");
            }

            return warnings;
        }
    }
}
