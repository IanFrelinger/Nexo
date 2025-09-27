using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Core.Contracts;

namespace Nexo.Core.Pipeline
{
    /// <summary>
    /// Helper methods for ExtensionPipeline.
    /// </summary>
    public sealed partial class ExtensionPipeline<TRequest, TArtifact>
    {
        /// <summary>
        /// Aggregates multiple policy results into a single outcome.
        /// </summary>
        /// <param name="policyResults">The policy results to aggregate</param>
        /// <returns>The aggregated policy outcome</returns>
        private static PolicyOutcome AggregatePolicyResults(IEnumerable<PolicyOutcome> policyResults)
        {
            var results = policyResults.ToList();
            if (!results.Any())
            {
                return new PolicyOutcome(true, "No Policies", new[] { "No policy gates configured" }, 1.0);
            }

            var allPassed = results.All(r => r.Passed);
            var averageScore = results.Average(r => r.Score);
            var allFindings = results.SelectMany(r => r.Findings).ToList();
            var policyNames = string.Join(", ", results.Select(r => r.PolicyPackName));

            return new PolicyOutcome(allPassed, policyNames, allFindings, averageScore);
        }
    }
}
