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
    /// Code generation functionality
    /// </summary>
    public partial class CodeGenerationService
    {
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
    }
}
