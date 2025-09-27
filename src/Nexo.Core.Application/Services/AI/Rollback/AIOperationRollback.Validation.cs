using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Rollback
{
    /// <summary>
    /// Validation and utility methods for AIOperationRollback.
    /// </summary>
    public partial class AIOperationRollback
    {
        private bool IsCheckpointValid(Checkpoint checkpoint)
        {
            // Simulate checkpoint validation
            return DateTime.UtcNow - checkpoint.CreatedAt < TimeSpan.FromHours(24);
        }

        private List<string> GenerateValidationRecommendations(List<ValidationIssue> issues)
        {
            var recommendations = new List<string>();

            if (issues.Any(i => i.Type == ValidationIssueType.SnapshotAge))
            {
                recommendations.Add("Consider creating a fresh snapshot for better rollback reliability");
            }

            if (issues.Any(i => i.Type == ValidationIssueType.DependencyInvalid))
            {
                recommendations.Add("Review and update dependencies before attempting rollback");
            }

            if (issues.Any(i => i.Severity == ValidationSeverity.Critical))
            {
                recommendations.Add("Address critical issues before proceeding with rollback");
            }

            return recommendations;
        }
    }
}
