using System;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class HealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object> Details { get; set; } = new();
    }
}
