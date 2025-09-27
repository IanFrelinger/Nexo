using System;
using System.Collections.Generic;

namespace Nexo.Core.Contracts
{
    /// <summary>
    /// Represents a plugin manifest that declares plugin metadata and capabilities.
    /// </summary>
    public partial class PluginManifest
    {
        /// <summary>
        /// Gets or sets the plugin name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plugin version.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of capabilities this plugin implements.
        /// </summary>
        public List<string> Capabilities { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the minimum required Nexo version.
        /// </summary>
        public string MinimalNexoVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plugin description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plugin author.
        /// </summary>
        public string Author { get; set; } = string.Empty;
    }
}
