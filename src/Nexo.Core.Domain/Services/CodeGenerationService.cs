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
    /// Service for generating typed C# code using Roslyn
    /// </summary>
    public class CodeGenerationService : ICodeGenerationService
    {
        private readonly ILogger<CodeGenerationService> _logger;
        private readonly Dictionary<string, MetadataReference> _references;

        public CodeGenerationService(ILogger<CodeGenerationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _references = new Dictionary<string, MetadataReference>();
            InitializeReferences();
        }

        public async Task<CodeGenerationResult> GenerateTypedCodeAsync(
            string className,
            string namespaceName,
            List<PropertyDefinition> properties,
            List<MethodDefinition> methods,
            CodeGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating typed code for class {ClassName} in namespace {Namespace}", 
                    className, namespaceName);

                options ??= new CodeGenerationOptions();

                var sourceCode = GenerateSourceCode(className, namespaceName, properties, methods, options);
                var compilation = CreateCompilation(sourceCode, options);
                var diagnostics = compilation.GetDiagnostics();

                var result = new CodeGenerationResult
                {
                    Success = true,
                    ClassName = className,
                    Namespace = namespaceName,
                    SourceCode = sourceCode,
                    Compilation = compilation,
                    Diagnostics = diagnostics.ToList(),
                    HasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error),
                    HasWarnings = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning)
                };

                if (result.HasErrors)
                {
                    result.Success = false;
                    result.ErrorMessage = string.Join("\n", diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(d => d.ToString()));
                }

                _logger.LogDebug("Generated code for {ClassName}: Success={Success}, Errors={ErrorCount}, Warnings={WarningCount}", 
                    className, result.Success, 
                    diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error),
                    diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning));

                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate typed code for class {ClassName}", className);
                return new CodeGenerationResult
                {
                    Success = false,
                    ClassName = className,
                    Namespace = namespaceName,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<CodeCompilationResult> CompileCodeAsync(
            string sourceCode,
            string assemblyName,
            CodeCompilationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Compiling code for assembly {AssemblyName}", assemblyName);

                options ??= new CodeCompilationOptions();

                var compilation = CreateCompilation(sourceCode, new CodeGenerationOptions
                {
                    AssemblyName = assemblyName,
                    TargetFramework = options.TargetFramework,
                    OutputKind = options.OutputKind
                });

                var diagnostics = compilation.GetDiagnostics();
                var hasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

                if (hasErrors)
                {
                    return new CodeCompilationResult
                    {
                        Success = false,
                        AssemblyName = assemblyName,
                        Diagnostics = diagnostics.ToList(),
                        ErrorMessage = string.Join("\n", diagnostics
                            .Where(d => d.Severity == DiagnosticSeverity.Error)
                            .Select(d => d.ToString()))
                    };
                }

                // Emit assembly
                using var ms = new MemoryStream();
                var emitResult = compilation.Emit(ms);

                if (!emitResult.Success)
                {
                    return new CodeCompilationResult
                    {
                        Success = false,
                        AssemblyName = assemblyName,
                        Diagnostics = emitResult.Diagnostics.ToList(),
                        ErrorMessage = string.Join("\n", emitResult.Diagnostics.Select(d => d.ToString()))
                    };
                }

                var assemblyBytes = ms.ToArray();

                return await Task.FromResult(new CodeCompilationResult
                {
                    Success = true,
                    AssemblyName = assemblyName,
                    AssemblyBytes = assemblyBytes,
                    Diagnostics = diagnostics.ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compile code for assembly {AssemblyName}", assemblyName);
                return new CodeCompilationResult
                {
                    Success = false,
                    AssemblyName = assemblyName,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<CodeExecutionResult> ExecuteCodeAsync(
            string sourceCode,
            string methodName,
            object[]? parameters = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Executing code with method {MethodName}", methodName);

                var compilationResult = await CompileCodeAsync(sourceCode, "TempAssembly", cancellationToken: cancellationToken);
                if (!compilationResult.Success)
                {
                    return new CodeExecutionResult
                    {
                        Success = false,
                        ErrorMessage = compilationResult.ErrorMessage
                    };
                }

                // Load assembly and execute method
                var assembly = System.Reflection.Assembly.Load(compilationResult.AssemblyBytes!);
                var type = assembly.GetTypes().FirstOrDefault();
                if (type == null)
                {
                    return new CodeExecutionResult
                    {
                        Success = false,
                        ErrorMessage = "No types found in compiled assembly"
                    };
                }

                var method = type.GetMethod(methodName);
                if (method == null)
                {
                    return new CodeExecutionResult
                    {
                        Success = false,
                        ErrorMessage = $"Method {methodName} not found in compiled assembly"
                    };
                }

                var instance = Activator.CreateInstance(type);
                var result = method.Invoke(instance, parameters);

                return await Task.FromResult(new CodeExecutionResult
                {
                    Success = true,
                    Result = result,
                    ExecutionTime = TimeSpan.Zero // Could be measured if needed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute code with method {MethodName}", methodName);
                return new CodeExecutionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<bool> ValidateCodeAsync(
            string sourceCode,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var compilation = CreateCompilation(sourceCode, new CodeGenerationOptions());
                var diagnostics = compilation.GetDiagnostics();
                var hasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

                _logger.LogDebug("Code validation result: HasErrors={HasErrors}, ErrorCount={ErrorCount}", 
                    hasErrors, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));

                return await Task.FromResult(!hasErrors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate code");
                return false;
            }
        }

        private string GenerateSourceCode(
            string className,
            string namespaceName,
            List<PropertyDefinition> properties,
            List<MethodDefinition> methods,
            CodeGenerationOptions options)
        {
            var usings = string.Join("\n", options.Usings.Select(u => $"using {u};"));
            var propertiesCode = string.Join("\n", properties.Select(GenerateProperty));
            var methodsCode = string.Join("\n", methods.Select(GenerateMethod));

            return $@"{usings}

namespace {namespaceName}
{{
    public class {className}
    {{
{propertiesCode}
{methodsCode}
    }}
}}";
        }

        private string GenerateProperty(PropertyDefinition property)
        {
            var getter = property.HasGetter ? "get; " : "";
            var setter = property.HasSetter ? "set; " : "";
            var accessModifier = property.AccessModifier ?? "public";

            return $"        {accessModifier} {property.Type} {property.Name} {{ {getter}{setter}}}";
        }

        private string GenerateMethod(MethodDefinition method)
        {
            var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"));
            var accessModifier = method.AccessModifier ?? "public";
            var returnType = method.ReturnType ?? "void";
            var body = method.Body ?? "        { }";

            return $@"        {accessModifier} {returnType} {method.Name}({parameters})
        {{
{body}
        }}";
        }

        private CSharpCompilation CreateCompilation(string sourceCode, CodeGenerationOptions options)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var compilationOptions = new CSharpCompilationOptions(options.OutputKind)
                .WithOptimizationLevel(options.OptimizationLevel)
                .WithPlatform(options.Platform);

            return CSharpCompilation.Create(
                options.AssemblyName,
                new[] { syntaxTree },
                _references.Values,
                compilationOptions);
        }

        private void InitializeReferences()
        {
            // Add basic .NET references
            var assemblies = new[]
            {
                typeof(object).Assembly,
                typeof(Console).Assembly,
                typeof(System.Collections.Generic.List<>).Assembly,
                typeof(System.Linq.Enumerable).Assembly
            };

            foreach (var assembly in assemblies)
            {
                _references[assembly.FullName!] = MetadataReference.CreateFromFile(assembly.Location);
            }
        }
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
