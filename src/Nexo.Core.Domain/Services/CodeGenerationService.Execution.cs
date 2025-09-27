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
    /// Code execution functionality
    /// </summary>
    public partial class CodeGenerationService
    {
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
    }
}
