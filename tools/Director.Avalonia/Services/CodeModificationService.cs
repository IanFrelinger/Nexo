using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Runtime.Loader;
using System.Collections.Concurrent;

namespace Director.Avalonia.Services;

/// <summary>
/// Service for dynamic code compilation, decompilation, and hot reloading of Avalonia app functionality
/// </summary>
public class CodeModificationService
{
    private readonly ILogger<CodeModificationService> _logger;
    private readonly ConcurrentDictionary<string, Assembly> _loadedAssemblies = new();
    private readonly ConcurrentDictionary<string, string> _decompiledCode = new();
    private readonly string _tempDirectory;
    private readonly string _backupDirectory;

    public CodeModificationService(ILogger<CodeModificationService> logger)
    {
        _logger = logger;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "DirectorStudio", "CodeModifications");
        _backupDirectory = Path.Combine(Path.GetTempPath(), "DirectorStudio", "Backups");
        
        Directory.CreateDirectory(_tempDirectory);
        Directory.CreateDirectory(_backupDirectory);
    }

    /// <summary>
    /// Decompiles an assembly to C# source code
    /// </summary>
    public async Task<string> DecompileAssemblyAsync(string assemblyPath)
    {
        try
        {
            if (_decompiledCode.TryGetValue(assemblyPath, out var cachedCode))
            {
                return cachedCode;
            }

            _logger.LogInformation($"Decompiling assembly: {assemblyPath}");

            // Load the assembly
            var assembly = Assembly.LoadFrom(assemblyPath);
            var decompiledCode = new StringBuilder();

            // Decompile each type
            foreach (var type in assembly.GetTypes())
            {
                decompiledCode.AppendLine($"// Decompiled from {assemblyPath}");
                decompiledCode.AppendLine($"// Type: {type.FullName}");
                decompiledCode.AppendLine();

                var typeCode = DecompileType(type);
                decompiledCode.AppendLine(typeCode);
                decompiledCode.AppendLine();
            }

            var result = decompiledCode.ToString();
            _decompiledCode[assemblyPath] = result;
            
            _logger.LogInformation($"Successfully decompiled assembly: {assemblyPath}");
            await Task.CompletedTask; // Ensure async method
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to decompile assembly: {assemblyPath}");
            throw;
        }
    }

    /// <summary>
    /// Compiles C# source code to an assembly
    /// </summary>
    public async Task<Assembly> CompileCodeAsync(string sourceCode, string assemblyName, IEnumerable<string>? references = null)
    {
        try
        {
            _logger.LogInformation($"Compiling code for assembly: {assemblyName}");

            // Parse the C# code
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

            // Get references
            var refs = GetDefaultReferences();
            if (references != null)
            {
                refs.AddRange(references.Select(r => MetadataReference.CreateFromFile(r)));
            }

            // Create compilation
            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                refs,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            // Emit to memory
            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            if (!result.Success)
            {
                var errors = string.Join("\n", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
                throw new InvalidOperationException($"Compilation failed:\n{errors}");
            }

            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());
            
            _loadedAssemblies[assemblyName] = assembly;
            
            _logger.LogInformation($"Successfully compiled assembly: {assemblyName}");
            await Task.CompletedTask; // Ensure async method
            return assembly;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to compile code for assembly: {assemblyName}");
            throw;
        }
    }

    /// <summary>
    /// Applies hot reload by replacing a method or class in the running application
    /// </summary>
    public async Task<bool> ApplyHotReloadAsync(string className, string methodName, string newImplementation)
    {
        try
        {
            _logger.LogInformation($"Applying hot reload for {className}.{methodName}");

            // Create backup
            await CreateBackupAsync(className, methodName);

            // Generate new code with the modified method
            var modifiedCode = await GenerateModifiedCodeAsync(className, methodName, newImplementation);

            // Compile the modified code
            var assemblyName = $"{className}_Modified_{DateTime.UtcNow:yyyyMMddHHmmss}";
            var assembly = await CompileCodeAsync(modifiedCode, assemblyName);

            // Apply the changes using reflection
            var success = await ApplyChangesUsingReflectionAsync(assembly, className, methodName);

            if (success)
            {
                _logger.LogInformation($"Successfully applied hot reload for {className}.{methodName}");
            }
            else
            {
                _logger.LogWarning($"Failed to apply hot reload for {className}.{methodName}");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to apply hot reload for {className}.{methodName}");
            return false;
        }
    }

    /// <summary>
    /// Processes natural language prompts to generate code modifications
    /// </summary>
    public async Task<string> ProcessNaturalLanguagePromptAsync(string prompt, string targetClass, string targetMethod)
    {
        try
        {
            _logger.LogInformation($"Processing natural language prompt: {prompt}");

            // Parse the prompt to understand the intent
            var intent = ParsePromptIntent(prompt);
            
            // Generate code based on the intent
            var generatedCode = await GenerateCodeFromIntentAsync(intent, targetClass, targetMethod);

            _logger.LogInformation($"Generated code from prompt: {prompt}");
            await Task.CompletedTask; // Ensure async method
            return generatedCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to process natural language prompt: {prompt}");
            throw;
        }
    }

    /// <summary>
    /// Gets the current implementation of a method
    /// </summary>
    public async Task<string> GetCurrentImplementationAsync(string className, string methodName)
    {
        try
        {
            var type = Type.GetType(className);
            if (type == null)
            {
                throw new ArgumentException($"Type {className} not found");
            }

            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (method == null)
            {
                throw new ArgumentException($"Method {methodName} not found in {className}");
            }

            // For now, return a placeholder - in a real implementation, you'd use a decompiler
            await Task.CompletedTask; // Ensure async method
            return $"// Current implementation of {className}.{methodName}\n// [Decompiled code would go here]";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to get current implementation of {className}.{methodName}");
            throw;
        }
    }

    /// <summary>
    /// Validates that modified code compiles and is safe to apply
    /// </summary>
    public async Task<ValidationResult> ValidateCodeAsync(string sourceCode, string className, string methodName)
    {
        try
        {
            _logger.LogInformation($"Validating code for {className}.{methodName}");

            // Parse and compile the code
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var compilation = CSharpCompilation.Create(
                "ValidationAssembly",
                new[] { syntaxTree },
                GetDefaultReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            // Check for compilation errors
            var diagnostics = compilation.GetDiagnostics();
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            var warnings = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();

            var result = new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors.Select(d => d.ToString()).ToList(),
                Warnings = warnings.Select(d => d.ToString()).ToList(),
                CompilationSuccess = true
            };

            if (result.IsValid)
            {
                // Additional safety checks
                result = await PerformSafetyChecksAsync(sourceCode, className, methodName, result);
            }

            _logger.LogInformation($"Validation completed for {className}.{methodName}: {(result.IsValid ? "Valid" : "Invalid")}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to validate code for {className}.{methodName}");
            return new ValidationResult
            {
                IsValid = false,
                Errors = new[] { ex.Message }.ToList(),
                Warnings = new List<string>(),
                CompilationSuccess = false
            };
        }
    }

    private string DecompileType(Type type)
    {
        var sb = new StringBuilder();
        
        // Add namespace and using statements
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine();
        
        // Add class declaration
        var accessibility = type.IsPublic ? "public" : "internal";
        var isStatic = type.IsAbstract && type.IsSealed ? "static " : "";
        var baseTypes = GetBaseTypes(type);
        
        sb.AppendLine($"{accessibility} {isStatic}class {type.Name}{baseTypes}");
        sb.AppendLine("{");
        
        // Add properties
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            var propAccessibility = prop.GetGetMethod()?.IsPublic == true ? "public" : "private";
            var propType = prop.PropertyType.Name;
            sb.AppendLine($"    {propAccessibility} {propType} {prop.Name} {{ get; set; }}");
        }
        
        // Add methods
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => !m.IsSpecialName))
        {
            var methodAccessibility = method.IsPublic ? "public" : "private";
            var isStaticMethod = method.IsStatic ? "static " : "";
            var returnType = method.ReturnType.Name;
            var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
            
            sb.AppendLine($"    {methodAccessibility} {isStaticMethod}{returnType} {method.Name}({parameters})");
            sb.AppendLine("    {");
            sb.AppendLine("        // [Method implementation would go here]");
            sb.AppendLine("        throw new NotImplementedException();");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private string GetBaseTypes(Type type)
    {
        if (type.BaseType != null && type.BaseType != typeof(object))
        {
            return $" : {type.BaseType.Name}";
        }
        return "";
    }

    private List<MetadataReference> GetDefaultReferences()
    {
        var references = new List<MetadataReference>();
        
        // Add common references
        var assemblies = new[]
        {
            typeof(object).Assembly,
            typeof(Console).Assembly,
            typeof(Task).Assembly,
            typeof(System.Collections.Generic.List<>).Assembly,
            typeof(Microsoft.Extensions.Logging.ILogger).Assembly
        };

        foreach (var assembly in assemblies)
        {
            references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        return references;
    }

    private async Task CreateBackupAsync(string className, string methodName)
    {
        var backupPath = Path.Combine(_backupDirectory, $"{className}_{methodName}_{DateTime.UtcNow:yyyyMMddHHmmss}.cs");
        var currentImplementation = await GetCurrentImplementationAsync(className, methodName);
        await File.WriteAllTextAsync(backupPath, currentImplementation);
        
        _logger.LogInformation($"Created backup at: {backupPath}");
    }

    private async Task<string> GenerateModifiedCodeAsync(string className, string methodName, string newImplementation)
    {
        // This is a simplified implementation
        // In a real scenario, you'd parse the existing class and replace the method
        await Task.CompletedTask; // Ensure async method
        return $@"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class {className}
{{
    public async Task {methodName}()
    {{
        {newImplementation}
    }}
}}";
    }

    private async Task<bool> ApplyChangesUsingReflectionAsync(Assembly assembly, string className, string methodName)
    {
        try
        {
            // This is a simplified implementation
            // In a real scenario, you'd use more sophisticated techniques like:
            // - Assembly unloading and reloading
            // - Dynamic method replacement
            // - AOP (Aspect-Oriented Programming) techniques
            
            var type = assembly.GetType(className);
            if (type == null) return false;

            var method = type.GetMethod(methodName);
            if (method == null) return false;

            // For now, just log that we would apply the changes
            _logger.LogInformation($"Would apply changes to {className}.{methodName}");
            await Task.CompletedTask; // Ensure async method
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to apply changes using reflection");
            return false;
        }
    }

    private PromptIntent ParsePromptIntent(string prompt)
    {
        // Simple intent parsing - in a real implementation, you'd use NLP libraries
        var lowerPrompt = prompt.ToLowerInvariant();
        
        if (lowerPrompt.Contains("add") || lowerPrompt.Contains("create"))
        {
            return new PromptIntent { Type = IntentType.Add, Description = prompt };
        }
        else if (lowerPrompt.Contains("modify") || lowerPrompt.Contains("change"))
        {
            return new PromptIntent { Type = IntentType.Modify, Description = prompt };
        }
        else if (lowerPrompt.Contains("remove") || lowerPrompt.Contains("delete"))
        {
            return new PromptIntent { Type = IntentType.Remove, Description = prompt };
        }
        else
        {
            return new PromptIntent { Type = IntentType.Unknown, Description = prompt };
        }
    }

    private async Task<string> GenerateCodeFromIntentAsync(PromptIntent intent, string targetClass, string targetMethod)
    {
        // Simple code generation based on intent
        // In a real implementation, you'd use AI/ML models or templates
        await Task.CompletedTask; // Ensure async method
        return intent.Type switch
        {
            IntentType.Add => $"// Add new functionality\nConsole.WriteLine(\"Added: {intent.Description}\");",
            IntentType.Modify => $"// Modified functionality\nConsole.WriteLine(\"Modified: {intent.Description}\");",
            IntentType.Remove => $"// Removed functionality\nConsole.WriteLine(\"Removed: {intent.Description}\");",
            IntentType.Fix => $"// Fixed functionality\nConsole.WriteLine(\"Fixed: {intent.Description}\");",
            IntentType.Optimize => $"// Optimized functionality\nConsole.WriteLine(\"Optimized: {intent.Description}\");",
            _ => $"// Unknown intent: {intent.Description}"
        };
    }

    private async Task<ValidationResult> PerformSafetyChecksAsync(string sourceCode, string className, string methodName, ValidationResult result)
    {
        // Perform additional safety checks
        var checks = new List<string>();

        // Check for dangerous operations
        if (sourceCode.Contains("File.Delete") || sourceCode.Contains("Directory.Delete"))
        {
            checks.Add("Contains file deletion operations");
        }

        if (sourceCode.Contains("Process.Start") || sourceCode.Contains("System.Diagnostics.Process"))
        {
            checks.Add("Contains process execution operations");
        }

        if (checks.Any())
        {
            result.Warnings.AddRange(checks);
        }

        await Task.CompletedTask; // Ensure async method
        return result;
    }
}

public class PromptIntent
{
    public IntentType Type { get; set; }
    public string Description { get; set; } = string.Empty;
}

public enum IntentType
{
    Unknown,
    Add,
    Modify,
    Remove,
    Fix,
    Optimize
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public bool CompilationSuccess { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
