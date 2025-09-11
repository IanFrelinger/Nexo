using System;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class DeploymentTarget
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
    }
}
