using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class ApplicationController
    {
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public List<string> Methods { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public string BaseClass { get; set; } = string.Empty;
        public Dictionary<string, object> Attributes { get; set; } = new();
    }
}
