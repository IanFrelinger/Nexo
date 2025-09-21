using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Extensions
{
    /// <summary>
    /// Adapter that implements the new ICompilationGate interface using the existing CSharpSyntaxValidator.
    /// </summary>
    public class CompilationGateAdapter : ICompilationGate
    {
        private readonly ILogger<CompilationGateAdapter> _logger;
        private readonly ICSharpSyntaxValidator _syntaxValidator;
        private readonly IAISyntaxFixer? _syntaxFixer;

        public CompilationGateAdapter(
            ILogger<CompilationGateAdapter> logger,
            ICSharpSyntaxValidator syntaxValidator,
            IAISyntaxFixer? syntaxFixer = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _syntaxValidator = syntaxValidator ?? throw new ArgumentNullException(nameof(syntaxValidator));
            _syntaxFixer = syntaxFixer;
        }

        public async ValueTask<ValidationResult> ValidateAsync(string sourceCode, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Validating source code compilation");

                // Use the existing syntax validator
                var result = await _syntaxValidator.ValidateSyntaxAsync(sourceCode, "temp");

                var findings = new List<string>();

                // Add compilation errors
                foreach (var error in result.CompilationErrors)
                {
                    findings.Add($"Error: {error.Message} (Line {error.Line}, Column {error.Column})");
                }

                // Add syntax warnings
                foreach (var warning in result.SyntaxWarnings)
                {
                    findings.Add($"Warning: {warning.Message} (Line {warning.Line}, Column {warning.Column})");
                }

                // If there are errors and we have a syntax fixer, try to fix them
                if (result.HasCompilationErrors && _syntaxFixer != null)
                {
                    _logger.LogInformation("Attempting to fix syntax errors using AI");
                    
                    try
                    {
                        var fixResult = await _syntaxFixer.FixSyntaxAsync(sourceCode);
                        
                        if (fixResult.IsSuccess)
                        {
                            findings.Add($"AI fixed {fixResult.FixesApplied.Count} syntax issues");
                            
                            // Re-validate the fixed code
                            var fixedResult = await _syntaxValidator.ValidateSyntaxAsync(fixResult.FixedCode, "temp");
                            
                            if (!fixedResult.HasCompilationErrors)
                            {
                                findings.Add("All syntax errors were successfully fixed");
                                return new ValidationResult(true, "CSharpSyntaxValidator", findings);
                            }
                            else
                            {
                                findings.Add("AI fixes were applied but some errors remain");
                            }
                        }
                        else
                        {
                            findings.Add($"AI syntax fixing failed: {string.Join(", ", fixResult.Errors)}");
                        }
                    }
                    catch (Exception fixEx)
                    {
                        _logger.LogWarning(fixEx, "AI syntax fixing encountered an error");
                        findings.Add($"AI syntax fixing failed: {fixEx.Message}");
                    }
                }

                bool passed = !result.HasCompilationErrors;
                string gateName = "CSharpSyntaxValidator";

                if (passed)
                {
                    _logger.LogInformation("Source code validation passed");
                }
                else
                {
                    _logger.LogWarning("Source code validation failed with {ErrorCount} errors", result.CompilationErrors.Count);
                }

                return new ValidationResult(passed, gateName, findings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during source code validation");
                
                var findings = new List<string> { $"Validation error: {ex.Message}" };
                return new ValidationResult(false, "CSharpSyntaxValidator", findings);
            }
        }
    }
}
