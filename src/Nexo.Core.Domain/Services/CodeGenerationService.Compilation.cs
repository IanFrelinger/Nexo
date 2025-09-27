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
    /// Code compilation functionality
    /// </summary>
    public partial class CodeGenerationService
    {
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
    }
}
