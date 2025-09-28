using Nexo.Core.Domain.Entities;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Input for calculating project metrics
    /// </summary>
    public class CalculateProjectMetricsInput
    {
        public string ProjectId { get; set; } = string.Empty;
        public Nexo.Core.Domain.Entities.Project Project { get; set; } = ProjectFactory.CreateDefaultProject();
        public bool IncludeDetailedMetrics { get; set; } = true;
    }
}
