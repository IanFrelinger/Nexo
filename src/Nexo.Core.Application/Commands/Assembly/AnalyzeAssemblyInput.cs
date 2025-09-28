using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Commands.Assembly
{
    /// <summary>
    /// Input for analyzing an assembly
    /// </summary>
    public class AnalyzeAssemblyInput
    {
        public string AssemblyPath { get; set; } = string.Empty;
        public bool IncludePrivateMembers { get; set; } = false;
        public bool IncludeInternalMembers { get; set; } = false;
        public bool IncludeNestedTypes { get; set; } = true;
        public bool IncludeGenerics { get; set; } = true;
        public bool IncludeResources { get; set; } = true;
        public bool IncludeCustomAttributes { get; set; } = true;
        public bool IncludeDependencies { get; set; } = true;
        public bool IncludeSecurityAnalysis { get; set; } = true;
        public bool IncludePerformanceAnalysis { get; set; } = false;
        public bool IncludeCompatibilityAnalysis { get; set; } = false;
        public Dictionary<string, object> AdditionalOptions { get; set; } = new Dictionary<string, object>();
    }
}
