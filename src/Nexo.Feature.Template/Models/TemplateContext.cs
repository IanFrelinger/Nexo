using System.Collections.Generic;

namespace Nexo.Feature.Template.Models
{
    public class TemplateContext
    {
        public string ProjectName { get; set; } = string.Empty;
        public string Framework { get; set; } = string.Empty;
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
    }
} 