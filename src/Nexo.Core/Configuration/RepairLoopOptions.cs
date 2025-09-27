using System.ComponentModel.DataAnnotations;

namespace Nexo.Core.Configuration
{
    /// <summary>
    /// Configuration options for the repair loop in pipeline execution.
    /// </summary>
    public partial class RepairLoopOptions
    {
        /// <summary>
        /// Maximum number of repair iterations to attempt before giving up.
        /// </summary>
        [Range(0, 10)]
        public int MaxRepairIterations { get; set; } = 2;

        /// <summary>
        /// Whether to enable canary deployment after successful repairs.
        /// </summary>
        public bool EnableCanaryDeployment { get; set; } = true;

        /// <summary>
        /// Whether to enable automatic rollback on canary failure.
        /// </summary>
        public bool EnableAutomaticRollback { get; set; } = true;

        /// <summary>
        /// Timeout in milliseconds for each repair attempt.
        /// </summary>
        [Range(1000, 300000)]
        public int RepairTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Timeout in milliseconds for canary deployment validation.
        /// </summary>
        [Range(1000, 600000)]
        public int CanaryTimeoutMs { get; set; } = 60000;
    }
}
