using System;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class Repository
    {
        public string Name { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public Dictionary<string, object> Methods { get; set; } = new();
    }
}
