using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Domain.Composition;

namespace Nexo.Core.Application.Services.Extensions
{
    /// <summary>
    /// AI-powered syntax fixer for generated extension code
    /// </summary>
    public class AISyntaxFixer : IAISyntaxFixer
    {
        private readonly ILogger<AISyntaxFixer> _logger;
        private readonly ICSharpSyntaxValidator _syntaxValidator;

        public AISyntaxFixer(ILogger<AISyntaxFixer> logger, ICSharpSyntaxValidator syntaxValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _syntaxValidator = syntaxValidator ?? throw new ArgumentNullException(nameof(syntaxValidator));
        }

        /// <summary>
        /// Attempts to fix syntax errors in the provided C# code
        /// </summary>
        public async Task<SyntaxFixResult> FixSyntaxAsync(string code)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));

            _logger.LogDebug("Starting syntax fixing for code of length {CodeLength}", code.Length);

            var result = new SyntaxFixResult
            {
                FixedCode = code,
                MaxFixAttempts = 3
            };

            try
            {
                // First, validate the code to see if it needs fixing
                var validationResult = await _syntaxValidator.ValidateAsync(code);
                result.OriginalValidationResult = validationResult;

                if (validationResult.IsValid)
                {
                    _logger.LogDebug("Code is already valid, no fixes needed");
                    result.IsSuccess = true;
                    return result;
                }

                // Attempt to fix the code
                var fixedCode = await AttemptSyntaxFixes(code, validationResult, result);

                // Validate the fixed code
                var finalValidationResult = await _syntaxValidator.ValidateAsync(fixedCode);
                result.FinalValidationResult = finalValidationResult;
                result.FixedCode = fixedCode;

                // Determine if the fixing was successful
                result.IsSuccess = finalValidationResult.IsValid;

                _logger.LogDebug("Syntax fixing completed. Success: {IsSuccess}, Fixes: {FixCount}, Attempts: {Attempts}", 
                    result.IsSuccess, result.FixesApplied.Count, result.FixAttempts);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during syntax fixing");
                result.IsSuccess = false;
                result.Errors.Add($"Syntax fixing failed: {ex.Message}");
                result.FixesApplied.Add($"Exception occurred: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Attempts to fix syntax errors using various strategies
        /// </summary>
        private async Task<string> AttemptSyntaxFixes(string code, ValidationResult validationResult, SyntaxFixResult result)
        {
            var fixedCode = code;
            var maxAttempts = result.MaxFixAttempts;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                result.FixAttempts = attempt;
                _logger.LogDebug("Fix attempt {Attempt} of {MaxAttempts}", attempt, maxAttempts);

                var previousCode = fixedCode;
                fixedCode = await ApplySyntaxFixes(fixedCode, validationResult, result);

                if (fixedCode == previousCode)
                {
                    _logger.LogDebug("No changes made in attempt {Attempt}, stopping", attempt);
                    break;
                }

                // Validate the fixed code
                var newValidationResult = await _syntaxValidator.ValidateAsync(fixedCode);
                if (newValidationResult.IsValid)
                {
                    _logger.LogDebug("Code is now valid after attempt {Attempt}", attempt);
                    break;
                }

                // Update validation result for next iteration
                validationResult = newValidationResult;
            }

            return fixedCode;
        }

        /// <summary>
        /// Applies various syntax fixes to the code
        /// </summary>
        private Task<string> ApplySyntaxFixes(string code, ValidationResult validationResult, SyntaxFixResult result)
        {
            var fixedCode = code;

            // Apply fixes based on error types
            foreach (var error in validationResult.Errors)
            {
                var originalCode = fixedCode;
                fixedCode = ApplyFixForError(fixedCode, error, result);
                
                if (fixedCode != originalCode)
                {
                    _logger.LogDebug("Applied fix for error: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
                }
            }

            // Apply general fixes
            fixedCode = ApplyGeneralFixes(fixedCode, result);

            return Task.FromResult(fixedCode);
        }

        /// <summary>
        /// Applies a specific fix based on the error type
        /// </summary>
        private string ApplyFixForError(string code, ValidationError error, SyntaxFixResult result)
        {
            var fixedCode = code;

            switch (error.Code)
            {
                case "CS1002": // ; expected
                    fixedCode = FixMissingSemicolon(fixedCode, result);
                    break;
                case "CS1026": // ) expected
                    fixedCode = FixMissingParenthesis(fixedCode, result);
                    break;
                case "CS1022": // } expected
                    fixedCode = FixMissingBrace(fixedCode, result);
                    break;
                case "CS1024": // ; expected
                    fixedCode = FixMissingSemicolon(fixedCode, result);
                    break;
                case "CS1513": // } expected
                    fixedCode = FixMissingBrace(fixedCode, result);
                    break;
                case "CS1514": // { expected
                    fixedCode = FixMissingOpeningBrace(fixedCode, result);
                    break;
                case "EMPTY_CODE":
                    result.FixesApplied.Add("Cannot fix empty code - requires content");
                    result.Errors.Add("Empty code cannot be automatically fixed");
                    break;
                default:
                    result.FixesApplied.Add($"Unfixable error: {error.Code} - {error.Message}");
                    break;
            }

            return fixedCode;
        }

        /// <summary>
        /// Fixes missing semicolons
        /// </summary>
        private string FixMissingSemicolon(string code, SyntaxFixResult result)
        {
            var lines = code.Split('\n');
            var fixedLines = new List<string>();
            bool fixApplied = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Add semicolon to lines that need it
                if (ShouldHaveSemicolon(trimmedLine) && !trimmedLine.EndsWith(";") && !trimmedLine.EndsWith("{") && !trimmedLine.EndsWith("}"))
                {
                    fixedLines.Add(line + ";");
                    fixApplied = true;
                }
                else
                {
                    fixedLines.Add(line);
                }
            }

            if (fixApplied)
            {
                result.FixesApplied.Add("Added missing semicolons");
            }

            return string.Join("\n", fixedLines);
        }

        /// <summary>
        /// Fixes missing closing parentheses
        /// </summary>
        private string FixMissingParenthesis(string code, SyntaxFixResult result)
        {
            var openCount = code.Count(c => c == '(');
            var closeCount = code.Count(c => c == ')');
            
            if (openCount > closeCount)
            {
                var missingCount = openCount - closeCount;
                var fixedCode = code + new string(')', missingCount);
                result.FixesApplied.Add($"Added {missingCount} missing closing parentheses");
                return fixedCode;
            }

            return code;
        }

        /// <summary>
        /// Fixes missing closing braces
        /// </summary>
        private string FixMissingBrace(string code, SyntaxFixResult result)
        {
            var openCount = code.Count(c => c == '{');
            var closeCount = code.Count(c => c == '}');
            
            if (openCount > closeCount)
            {
                var missingCount = openCount - closeCount;
                var fixedCode = code + new string('}', missingCount);
                result.FixesApplied.Add($"Added {missingCount} missing closing braces");
                return fixedCode;
            }

            return code;
        }

        /// <summary>
        /// Fixes missing opening braces
        /// </summary>
        private string FixMissingOpeningBrace(string code, SyntaxFixResult result)
        {
            // This is more complex and would require parsing the AST
            // For now, we'll just add a basic fix
            result.FixesApplied.Add("Missing opening brace - requires manual intervention");
            return code;
        }

        /// <summary>
        /// Applies general fixes that don't depend on specific error codes
        /// </summary>
        private string ApplyGeneralFixes(string code, SyntaxFixResult result)
        {
            var fixedCode = code;

            // Fix common issues
            fixedCode = FixCommonIssues(fixedCode, result);

            return fixedCode;
        }

        /// <summary>
        /// Fixes common syntax issues
        /// </summary>
        private string FixCommonIssues(string code, SyntaxFixResult result)
        {
            var fixedCode = code;
            bool fixApplied = false;

            // Fix double semicolons
            if (fixedCode.Contains(";;"))
            {
                fixedCode = fixedCode.Replace(";;", ";");
                fixApplied = true;
            }

            // Fix missing using statements
            if (!fixedCode.Contains("using System;") && (fixedCode.Contains("Console.WriteLine") || fixedCode.Contains("System.")))
            {
                fixedCode = "using System;\n\n" + fixedCode;
                fixApplied = true;
            }

            // Fix common string issues
            if (fixedCode.Contains("\"\"\"") && !fixedCode.Contains("raw string"))
            {
                // This is a complex fix that would require more sophisticated parsing
                result.FixesApplied.Add("Complex string syntax detected - may need manual review");
            }

            if (fixApplied)
            {
                result.FixesApplied.Add("Applied general syntax fixes");
            }

            return fixedCode;
        }

        /// <summary>
        /// Determines if a line should have a semicolon
        /// </summary>
        private bool ShouldHaveSemicolon(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            if (line.StartsWith("//") || line.StartsWith("/*") || line.StartsWith("*"))
                return false;

            if (line.EndsWith("{") || line.EndsWith("}") || line.EndsWith(";"))
                return false;

            if (line.Contains("if ") || line.Contains("for ") || line.Contains("while ") || line.Contains("foreach "))
                return false;

            if (line.Contains("class ") || line.Contains("interface ") || line.Contains("enum "))
                return false;

            if (line.Contains("public ") || line.Contains("private ") || line.Contains("protected ") || line.Contains("internal "))
                return false;

            if (line.StartsWith("using "))
                return true;

            // If it looks like a statement, it probably needs a semicolon
            return line.Contains("=") || line.Contains("return ") || line.Contains("Console.") || line.Contains("var ");
        }
    }
}
