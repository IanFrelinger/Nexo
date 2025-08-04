using System.Collections.Generic;

namespace Nexo.Feature.Template.Models
{
    public class TemplateResult
    {
        public bool Success { get; set; }
        public List<string> GeneratedFiles { get; set; }
        public string OutputPath { get; set; }
    }
} 