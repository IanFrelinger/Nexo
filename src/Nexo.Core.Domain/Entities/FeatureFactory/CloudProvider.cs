using System;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class CloudProvider
    {
        public string Name { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
    }
}
