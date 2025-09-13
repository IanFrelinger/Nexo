using Nexo.Core.Domain.Common;
using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Models.Extensions
{
    /// <summary>
    /// Represents the result of an AI-generated extension compilation.
    /// </summary>
    public class ExtensionGenerationResult : BaseResult
    {
        public string RequestId { get; set; } = string.Empty;
        public string GeneratedCode { get; set; } = string.Empty;
        public byte[]? CompiledAssembly { get; set; }
        public string AssemblyPath { get; set; } = string.Empty;
        public TimeSpan GenerationTime { get; set; }
        public TimeSpan CompilationTime { get; set; }
        public TimeSpan TotalTime => GenerationTime + CompilationTime;
        public List<CompilationError> CompilationErrors { get; set; } = new();
        public List<CompilationWarning> SyntaxWarnings { get; set; } = new();
        public ExtensionRequest OriginalRequest { get; set; } = new();
        public bool IsCompiled => CompiledAssembly != null && CompiledAssembly.Length > 0;
        public bool HasCompilationErrors => CompilationErrors.Count > 0;
        public bool HasSyntaxWarnings => SyntaxWarnings.Count > 0;

        public new bool IsSuccess
        {
            get => !HasCompilationErrors && base.IsSuccess;
            set => base.IsSuccess = value;
        }

        public void AddCompilationError(string message, int line, int column, string errorCode = "")
        {
            CompilationErrors.Add(new CompilationError
            {
                Message = message,
                Line = line,
                Column = column,
                Id = errorCode
            });
        }

        public void AddSyntaxWarning(string message, int line, int column, string warningCode = "")
        {
            SyntaxWarnings.Add(new CompilationWarning
            {
                Message = message,
                Line = line,
                Column = column,
                Id = warningCode
            });
        }
    }

}
