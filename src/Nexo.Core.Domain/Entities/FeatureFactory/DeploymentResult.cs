using System;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class DeploymentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DeploymentId { get; set; } = string.Empty;
    }
}
