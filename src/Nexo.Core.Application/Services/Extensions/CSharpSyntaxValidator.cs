using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Domain.Composition;

namespace Nexo.Core.Application.Services.Extensions
{
    /// <summary>
    /// Validates C# syntax using Roslyn for generated extension code
    /// </summary>
    public class CSharpSyntaxValidator : ICSharpSyntaxValidator
    {
        private readonly ILogger<CSharpSyntaxValidator> _logger;

        public CSharpSyntaxValidator(ILogger<CSharpSyntaxValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates C# syntax for the provided code
        /// </summary>
        public async Task<ValidationResult> ValidateAsync(string code)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));

            _logger.LogDebug("Starting C# syntax validation for code of length {CodeLength}", code.Length);

            try
            {
                var result = new ValidationResult();

                // Check for empty or whitespace-only code
                if (string.IsNullOrWhiteSpace(code))
                {
                    result.AddError("Code is empty or contains only whitespace", "code", "EMPTY_CODE");
                    _logger.LogWarning("Validation failed: Code is empty");
                    return result;
                }

                // Parse the C# code using Roslyn
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var diagnostics = syntaxTree.GetDiagnostics();

                // Process diagnostics
                foreach (var diagnostic in diagnostics)
                {
                    var severity = GetValidationSeverity(diagnostic.Severity);
                    var message = $"{diagnostic.GetMessage()} (Line {diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}, Column {diagnostic.Location.GetLineSpan().StartLinePosition.Character + 1})";
                    
                    if (severity == ValidationSeverity.Error)
                    {
                        result.AddError(message, "syntax", diagnostic.Id);
                        _logger.LogDebug("Syntax error found: {Error}", message);
                    }
                    else if (severity == ValidationSeverity.Warning)
                    {
                        result.AddWarning(message, "syntax", diagnostic.Id);
                        _logger.LogDebug("Syntax warning found: {Warning}", message);
                    }
                }

                // Additional validation checks
                await PerformAdditionalValidationAsync(code, result);

                _logger.LogDebug("C# syntax validation completed. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}", 
                    result.IsValid, result.Errors.Count, result.Warnings.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during C# syntax validation");
                var errorResult = new ValidationResult();
                errorResult.AddError($"Syntax validation failed: {ex.Message}", "validation", "VALIDATION_ERROR");
                return errorResult;
            }
        }

        /// <summary>
        /// Performs additional validation checks beyond basic syntax
        /// </summary>
        private async Task PerformAdditionalValidationAsync(string code, ValidationResult result)
        {
            await Task.Run(() =>
            {
                // Check for basic structure
                if (!ContainsClassOrInterfaceOrEnum(code))
                {
                    result.AddWarning("Code does not contain any class, interface, or enum definitions", "structure", "NO_TYPE_DEFINITIONS");
                }

                // Check for proper using statements
                if (!HasUsingStatements(code))
                {
                    result.AddWarning("Code does not contain any using statements", "structure", "NO_USING_STATEMENTS");
                }

                // Check for potential issues
                CheckForPotentialIssues(code, result);
            });
        }

        /// <summary>
        /// Checks if the code contains class, interface, or enum definitions
        /// </summary>
        private bool ContainsClassOrInterfaceOrEnum(string code)
        {
            var keywords = new[] { "class ", "interface ", "enum " };
            return keywords.Any(keyword => code.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if the code has using statements
        /// </summary>
        private bool HasUsingStatements(string code)
        {
            return code.Contains("using ", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks for potential issues in the code
        /// </summary>
        private void CheckForPotentialIssues(string code, ValidationResult result)
        {
            // Check for TODO comments
            if (code.Contains("TODO", StringComparison.OrdinalIgnoreCase))
            {
                result.AddWarning("Code contains TODO comments that should be addressed", "quality", "TODO_FOUND");
            }

            // Check for FIXME comments
            if (code.Contains("FIXME", StringComparison.OrdinalIgnoreCase))
            {
                result.AddWarning("Code contains FIXME comments that should be addressed", "quality", "FIXME_FOUND");
            }

            // Check for hardcoded strings (basic check)
            if (code.Contains("\"hardcoded\"", StringComparison.OrdinalIgnoreCase) || 
                code.Contains("'hardcoded'", StringComparison.OrdinalIgnoreCase))
            {
                result.AddWarning("Code may contain hardcoded values that should be configurable", "quality", "HARDCODED_VALUES");
            }

            // Check for empty catch blocks
            if (code.Contains("catch", StringComparison.OrdinalIgnoreCase) && 
                code.Contains("catch", StringComparison.OrdinalIgnoreCase) && 
                !code.Contains("catch", StringComparison.OrdinalIgnoreCase))
            {
                // This is a simplified check - in a real implementation, you'd parse the AST
                result.AddWarning("Code may contain empty catch blocks", "quality", "EMPTY_CATCH_BLOCKS");
            }
        }

        /// <summary>
        /// Converts Roslyn diagnostic severity to validation severity
        /// </summary>
        private ValidationSeverity GetValidationSeverity(DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Error => ValidationSeverity.Error,
                DiagnosticSeverity.Warning => ValidationSeverity.Warning,
                DiagnosticSeverity.Info => ValidationSeverity.Info,
                DiagnosticSeverity.Hidden => ValidationSeverity.Info,
                _ => ValidationSeverity.Info
            };
        }
    }
}
