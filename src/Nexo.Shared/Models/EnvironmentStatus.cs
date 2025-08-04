using System.Collections.Generic;

namespace Nexo.Shared.Models
{
    /// <summary>
    /// Represents the status of a development environment.
    /// </summary>
    public class EnvironmentStatus
    {
        public bool IsReady { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string StatusMessage { get; set; }
        public Dictionary<string, object> Resources { get; set; } = new Dictionary<string, object>();
    }
}