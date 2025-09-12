using System;
using System.Collections.Generic;

namespace Nexo.Feature.AI.Models.RAG
{
    /// <summary>
    /// Metadata for documentation ingestion
    /// </summary>
    public class DocumentationMetadata
    {
        public string Source { get; set; } = string.Empty;
        public string DocumentationType { get; set; } = string.Empty; // Language, Framework, API, Migration, etc.
        public string Version { get; set; } = string.Empty; // C# version or .NET version
        public string Runtime { get; set; } = string.Empty; // .NET Framework, .NET Core, .NET 5+, etc.
        public string[] Categories { get; set; } = Array.Empty<string>();
        public string[] Tags { get; set; } = Array.Empty<string>();
        public Dictionary<string, object> AdditionalMetadata { get; set; } = new Dictionary<string, object>();
    }
}
