using System;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class DeploymentStatus
    {
        public string State { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
