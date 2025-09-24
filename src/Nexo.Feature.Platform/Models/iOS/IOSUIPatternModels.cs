using System;
using System.Collections.Generic;

namespace Nexo.Feature.Platform.Models
{
    /// <summary>
    /// iOS UI pattern configuration.
    /// </summary>
    public class IOSUIPattern
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IOSUIPatternType PatternType { get; set; }
        public List<IOSUIComponent> Components { get; set; } = new List<IOSUIComponent>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// iOS UI component.
    /// </summary>
    public class IOSUIComponent
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public List<IOSUIComponent> Children { get; set; } = new List<IOSUIComponent>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// iOS performance optimization.
    /// </summary>
    public class IOSPerformanceOptimization
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IOSOptimizationType OptimizationType { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
