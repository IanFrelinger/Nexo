using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class ApplicationModel
    {
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public List<string> Properties { get; set; } = new();
        public List<string> Methods { get; set; } = new();
        public string BaseClass { get; set; } = string.Empty;
        public Dictionary<string, object> Attributes { get; set; } = new();
    }
}
