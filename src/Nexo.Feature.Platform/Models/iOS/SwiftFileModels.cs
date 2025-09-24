using System;
using System.Collections.Generic;
using Nexo.Feature.Platform.Enums;

namespace Nexo.Feature.Platform.Models
{
    /// <summary>
    /// Swift source code file.
    /// </summary>
    public class SwiftFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public SwiftFileType FileType { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// SwiftUI view file.
    /// </summary>
    public class SwiftUIFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public SwiftUIViewType ViewType { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Core Data model file.
    /// </summary>
    public class CoreDataFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<CoreDataEntity> Entities { get; set; } = new List<CoreDataEntity>();
        public List<CoreDataRelationship> Relationships { get; set; } = new List<CoreDataRelationship>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Metal shader file.
    /// </summary>
    public class MetalFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public MetalShaderType ShaderType { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
