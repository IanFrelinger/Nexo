using System.Collections.Generic;

namespace Nexo.Shared.Models
{
    /// <summary>
    /// Represents the status of a development environment.
    /// </summary>
    public class EnvironmentStatus
    {
        public bool IsReady { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
        public Dictionary<string, object> Resources { get; set; } = new Dictionary<string, object>();
    }
}