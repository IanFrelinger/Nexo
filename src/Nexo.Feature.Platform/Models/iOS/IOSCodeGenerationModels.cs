using System;
using System.Collections.Generic;
using Nexo.Feature.Platform.Enums;

namespace Nexo.Feature.Platform.Models
{
    /// <summary>
    /// Result of iOS code generation process.
    /// </summary>
    public class IOSCodeGenerationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public IOSGeneratedCode GeneratedCode { get; set; } = new IOSGeneratedCode();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double GenerationScore { get; set; }
        public TimeSpan GenerationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Generated iOS code components.
    /// </summary>
    public class IOSGeneratedCode
    {
        public List<SwiftFile> SwiftFiles { get; set; } = new List<SwiftFile>();
        public List<SwiftUIFile> SwiftUIFiles { get; set; } = new List<SwiftUIFile>();
        public List<CoreDataFile> CoreDataFiles { get; set; } = new List<CoreDataFile>();
        public List<MetalFile> MetalFiles { get; set; } = new List<MetalFile>();
        public IOSAppConfiguration AppConfiguration { get; set; } = new IOSAppConfiguration();
        public List<IOSUIPattern> AppliedUIPatterns { get; set; } = new List<IOSUIPattern>();
        public List<IOSPerformanceOptimization> AppliedOptimizations { get; set; } = new List<IOSPerformanceOptimization>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
