using System.Collections.Generic;

namespace Nexo.Feature.Template.Models
{
    public class TemplateDefinition
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
} 