using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Domain.Models.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Extensions
{
    public class CSharpSyntaxValidator : ICSharpSyntaxValidator
    {
        private readonly ILogger<CSharpSyntaxValidator> _logger;

        public CSharpSyntaxValidator(ILogger<CSharpSyntaxValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<ExtensionGenerationResult> ValidateSyntaxAsync(string code, string assemblyName)
        {
            var result = new ExtensionGenerationResult
            {
                GeneratedCode = code
            };
            result.IsSuccess = true; // Assume success unless errors are found

            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var diagnostics = syntaxTree.GetDiagnostics();

                foreach (var diagnostic in diagnostics)
                {
                    var line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1;
                    var column = diagnostic.Location.GetLineSpan().StartLinePosition.Character + 1;

                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        result.AddCompilationError(diagnostic.GetMessage(), line, column, diagnostic.Id);
                        result.IsSuccess = false;
                    }
                    else if (diagnostic.Severity == DiagnosticSeverity.Warning)
                    {
                        result.AddSyntaxWarning(diagnostic.GetMessage(), line, column, diagnostic.Id);
                    }
                }

                if (result.HasCompilationErrors)
                {
                    _logger.LogWarning("Syntax validation found errors for assembly: {AssemblyName}", assemblyName);
                }
                else if (result.HasSyntaxWarnings)
                {
                    _logger.LogInformation("Syntax validation found warnings for assembly: {AssemblyName}", assemblyName);
                }
                else
                {
                    _logger.LogInformation("Syntax validation passed for assembly: {AssemblyName}", assemblyName);
                }
            }
            catch (Exception ex)
            {
                result.AddCompilationError($"Unexpected error during syntax validation: {ex.Message}", 0, 0, "SYN0001");
                result.IsSuccess = false;
                _logger.LogError(ex, "Error during syntax validation for assembly: {AssemblyName}", assemblyName);
            }

            return Task.FromResult(result);
        }
    }
}