using System.Collections.Generic;

namespace Nexo.Feature.Template.Models
{
    public partial class TemplateDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
} 