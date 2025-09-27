using System;
using System.Collections.Generic;

namespace Nexo.Infrastructure.Policy
{
    /// <summary>
    /// Policy manifest structure
    /// </summary>
    public class PolicyManifest
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public List<string> Includes { get; set; } = new List<string>();
        public List<string> Overrides { get; set; } = new List<string>();
    }
}
