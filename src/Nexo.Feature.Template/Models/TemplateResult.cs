using System.Collections.Generic;

namespace Nexo.Feature.Template.Models
{
    public class TemplateResult
    {
        public bool Success { get; set; }
        public List<string> GeneratedFiles { get; set; } = new List<string>();
        public string OutputPath { get; set; } = string.Empty;
    }
} 