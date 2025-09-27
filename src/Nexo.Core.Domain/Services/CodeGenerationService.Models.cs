using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Data models and DTOs for code generation
    /// </summary>
    public partial class CodeGenerationService
    {
        // Data models are defined here for code generation functionality
    }

    /// <summary>
    /// Property definition for code generation
    /// </summary>
    public class PropertyDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? AccessModifier { get; set; }
        public bool HasGetter { get; set; } = true;
        public bool HasSetter { get; set; } = true;
        public object? DefaultValue { get; set; }
    }

    /// <summary>
    /// Method definition for code generation
    /// </summary>
    public class MethodDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string? ReturnType { get; set; }
        public string? AccessModifier { get; set; }
        public List<ParameterDefinition> Parameters { get; set; } = new();
        public string? Body { get; set; }
    }

    /// <summary>
    /// Parameter definition for code generation
    /// </summary>
    public class ParameterDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public object? DefaultValue { get; set; }
    }

    /// <summary>
    /// Code generation options
    /// </summary>
    public class CodeGenerationOptions
    {
        public string AssemblyName { get; set; } = "GeneratedAssembly";
        public string TargetFramework { get; set; } = "net8.0";
        public OutputKind OutputKind { get; set; } = OutputKind.DynamicallyLinkedLibrary;
        public OptimizationLevel OptimizationLevel { get; set; } = OptimizationLevel.Release;
        public Platform Platform { get; set; } = Platform.AnyCpu;
        public List<string> Usings { get; set; } = new()
        {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Threading.Tasks"
        };
    }

    /// <summary>
    /// Code compilation options
    /// </summary>
    public class CodeCompilationOptions
    {
        public OutputKind OutputKind { get; set; } = OutputKind.DynamicallyLinkedLibrary;
        public string TargetFramework { get; set; } = "net8.0";
        public OptimizationLevel OptimizationLevel { get; set; } = OptimizationLevel.Release;
    }

    /// <summary>
    /// Code generation result
    /// </summary>
    public class CodeGenerationResult
    {
        public bool Success { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public CSharpCompilation? Compilation { get; set; }
        public List<Diagnostic> Diagnostics { get; set; } = new();
        public bool HasErrors { get; set; }
        public bool HasWarnings { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Code compilation result
    /// </summary>
    public class CodeCompilationResult
    {
        public bool Success { get; set; }
        public string AssemblyName { get; set; } = string.Empty;
        public byte[]? AssemblyBytes { get; set; }
        public List<Diagnostic> Diagnostics { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Code execution result
    /// </summary>
    public class CodeExecutionResult
    {
        public bool Success { get; set; }
        public object? Result { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Interface for code generation service
    /// </summary>
    public interface ICodeGenerationService
    {
        Task<CodeGenerationResult> GenerateTypedCodeAsync(
            string className,
            string namespaceName,
            List<PropertyDefinition> properties,
            List<MethodDefinition> methods,
            CodeGenerationOptions? options = null,
            CancellationToken cancellationToken = default);

        Task<CodeCompilationResult> CompileCodeAsync(
            string sourceCode,
            string assemblyName,
            CodeCompilationOptions? options = null,
            CancellationToken cancellationToken = default);

        Task<CodeExecutionResult> ExecuteCodeAsync(
            string sourceCode,
            string methodName,
            object[]? parameters = null,
            CancellationToken cancellationToken = default);

        Task<bool> ValidateCodeAsync(
            string sourceCode,
            CancellationToken cancellationToken = default);
    }
}
