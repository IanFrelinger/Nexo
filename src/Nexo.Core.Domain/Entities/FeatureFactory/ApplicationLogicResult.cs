using System;
using System.Collections.Generic;
using System.Text;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    /// <summary>
    /// Represents the result of application logic generation
    /// </summary>
    public class ApplicationLogicResult
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string GeneratedCode { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Framework { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Draft";
        public List<string> Tags { get; set; } = new List<string>();
        public string Version { get; set; } = "1.0.0";
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        public List<ApplicationController> Controllers { get; set; } = new();
        public List<ApplicationModel> Models { get; set; } = new();
        public List<ApplicationService> Services { get; set; } = new();
    }
}
