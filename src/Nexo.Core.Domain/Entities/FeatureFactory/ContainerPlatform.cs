using System;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class ContainerPlatform
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
    }
}
