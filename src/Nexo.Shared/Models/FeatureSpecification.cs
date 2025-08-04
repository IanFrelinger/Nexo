using System;
using System.Collections.Generic;
using Nexo.Core.Application.Enums;
using Nexo.Core.Application.Interfaces;

namespace Nexo.Shared.Models
{
    /// <summary>
    /// Represents a specification for implementing a feature.
    /// </summary>
    public class FeatureSpecification
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public List<string> ImplementationSteps { get; set; } = new List<string>();
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }
}