using System.Collections.Generic;

namespace Nexo.Feature.Template.Models
{
    public class TemplateContext
    {
        public string ProjectName { get; set; }
        public string Framework { get; set; }
        public Dictionary<string, object> Variables { get; set; }
    }
} 