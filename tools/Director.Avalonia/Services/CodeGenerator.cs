using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Director.Avalonia.Services;

/// <summary>
/// Code generator that integrates with Nexo's feature factory for natural language to code conversion
/// </summary>
public class CodeGenerator
{
    private readonly ILogger<CodeGenerator> _logger;
    private readonly Dictionary<IntentType, Func<ParsedPrompt, string, string, Task<string>>> _generators;

    public CodeGenerator(ILogger<CodeGenerator> logger)
    {
        _logger = logger;
        _generators = new Dictionary<IntentType, Func<ParsedPrompt, string, string, Task<string>>>
        {
            { IntentType.Add, GenerateAddCodeAsync },
            { IntentType.Modify, GenerateModifyCodeAsync },
            { IntentType.Remove, GenerateRemoveCodeAsync },
            { IntentType.Fix, GenerateFixCodeAsync },
            { IntentType.Optimize, GenerateOptimizeCodeAsync }
        };
    }

    /// <summary>
    /// Generates code based on a parsed prompt
    /// </summary>
    public async Task<string> GenerateCodeAsync(ParsedPrompt parsedPrompt, string targetClass, string targetMethod)
    {
        try
        {
            _logger.LogInformation($"Generating code for intent: {parsedPrompt.Intent}");

            if (_generators.TryGetValue(parsedPrompt.Intent, out var generator))
            {
                return await generator(parsedPrompt, targetClass, targetMethod);
            }
            else
            {
                return await GenerateDefaultCodeAsync(parsedPrompt, targetClass, targetMethod);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to generate code for intent: {parsedPrompt.Intent}");
            return GenerateErrorCode(ex.Message);
        }
    }

    private async Task<string> GenerateAddCodeAsync(ParsedPrompt prompt, string targetClass, string targetMethod)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine("using Avalonia.Controls;");
        sb.AppendLine("using Avalonia.Media;");
        sb.AppendLine();

        sb.AppendLine($"public class {targetClass}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly ILogger _logger;");
        sb.AppendLine();

        // Add constructor
        sb.AppendLine($"    public {targetClass}(ILogger logger)");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger = logger;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Add the method
        sb.AppendLine($"    public async Task {targetMethod}()");
        sb.AppendLine("    {");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        
        // Generate method body based on prompt
        var methodBody = await GenerateMethodBodyAsync(prompt);
        sb.AppendLine(methodBody);
        
        sb.AppendLine("        }");
        sb.AppendLine("        catch (Exception ex)");
        sb.AppendLine("        {");
        sb.AppendLine("            _logger.LogError(ex, \"Error in {targetMethod}\");");
        sb.AppendLine("            throw;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        await Task.CompletedTask; // Ensure async method
        return sb.ToString();
    }

    private async Task<string> GenerateModifyCodeAsync(ParsedPrompt prompt, string targetClass, string targetMethod)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine();

        sb.AppendLine($"public class {targetClass}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly ILogger _logger;");
        sb.AppendLine();

        // Add constructor
        sb.AppendLine($"    public {targetClass}(ILogger logger)");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger = logger;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Add the modified method
        sb.AppendLine($"    public async Task {targetMethod}()");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger.LogInformation(\"Starting modified {targetMethod}\");");
        
        // Generate modified method body
        var methodBody = await GenerateModifiedMethodBodyAsync(prompt);
        sb.AppendLine(methodBody);
        
        sb.AppendLine("        _logger.LogInformation(\"Completed modified {targetMethod}\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        await Task.CompletedTask; // Ensure async method
        return sb.ToString();
    }

    private async Task<string> GenerateRemoveCodeAsync(ParsedPrompt prompt, string targetClass, string targetMethod)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine();

        sb.AppendLine($"public class {targetClass}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly ILogger _logger;");
        sb.AppendLine();

        // Add constructor
        sb.AppendLine($"    public {targetClass}(ILogger logger)");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger = logger;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Add the method with removal logic
        sb.AppendLine($"    public async Task {targetMethod}()");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger.LogInformation(\"Removing functionality from {targetMethod}\");");
        
        // Generate removal method body
        var methodBody = await GenerateRemovalMethodBodyAsync(prompt);
        sb.AppendLine(methodBody);
        
        sb.AppendLine("        _logger.LogInformation(\"Removal completed for {targetMethod}\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        await Task.CompletedTask; // Ensure async method
        return sb.ToString();
    }

    private async Task<string> GenerateFixCodeAsync(ParsedPrompt prompt, string targetClass, string targetMethod)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine();

        sb.AppendLine($"public class {targetClass}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly ILogger _logger;");
        sb.AppendLine();

        // Add constructor
        sb.AppendLine($"    public {targetClass}(ILogger logger)");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger = logger;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Add the fixed method
        sb.AppendLine($"    public async Task {targetMethod}()");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger.LogInformation(\"Starting fixed {targetMethod}\");");
        
        // Generate fixed method body
        var methodBody = await GenerateFixedMethodBodyAsync(prompt);
        sb.AppendLine(methodBody);
        
        sb.AppendLine("        _logger.LogInformation(\"Fixed {targetMethod} completed successfully\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        await Task.CompletedTask; // Ensure async method
        return sb.ToString();
    }

    private async Task<string> GenerateOptimizeCodeAsync(ParsedPrompt prompt, string targetClass, string targetMethod)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine("using System.Diagnostics;");
        sb.AppendLine();

        sb.AppendLine($"public class {targetClass}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly ILogger _logger;");
        sb.AppendLine();

        // Add constructor
        sb.AppendLine($"    public {targetClass}(ILogger logger)");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger = logger;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Add the optimized method
        sb.AppendLine($"    public async Task {targetMethod}()");
        sb.AppendLine("    {");
        sb.AppendLine("        var stopwatch = Stopwatch.StartNew();");
        sb.AppendLine("        _logger.LogInformation(\"Starting optimized {targetMethod}\");");
        
        // Generate optimized method body
        var methodBody = await GenerateOptimizedMethodBodyAsync(prompt);
        sb.AppendLine(methodBody);
        
        sb.AppendLine("        stopwatch.Stop();");
        sb.AppendLine("        _logger.LogInformation(\"Optimized {targetMethod} completed in {stopwatch.ElapsedMilliseconds}ms\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        await Task.CompletedTask; // Ensure async method
        return sb.ToString();
    }

    private async Task<string> GenerateDefaultCodeAsync(ParsedPrompt prompt, string targetClass, string targetMethod)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine();

        sb.AppendLine($"public class {targetClass}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly ILogger _logger;");
        sb.AppendLine();

        // Add constructor
        sb.AppendLine($"    public {targetClass}(ILogger logger)");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger = logger;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Add the method
        sb.AppendLine($"    public async Task {targetMethod}()");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger.LogInformation(\"Executing {targetMethod}\");");
        sb.AppendLine("        // Generated code based on prompt");
        sb.AppendLine("        await Task.Delay(100); // Simulate work");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        await Task.CompletedTask; // Ensure async method
        return sb.ToString();
    }

    private async Task<string> GenerateMethodBodyAsync(ParsedPrompt prompt)
    {
        var sb = new StringBuilder();
        
        // Generate method body based on parameters
        foreach (var param in prompt.Parameters)
        {
            switch (param.Key.ToLowerInvariant())
            {
                case "color":
                    sb.AppendLine($"            // Setting color to {param.Value}");
                    sb.AppendLine($"            var color = Colors.{param.Value};");
                    break;
                case "size":
                    sb.AppendLine($"            // Setting size to {param.Value}");
                    sb.AppendLine($"            var size = {param.Value};");
                    break;
                case "text":
                    sb.AppendLine($"            // Setting text to '{param.Value}'");
                    sb.AppendLine($"            var text = \"{param.Value}\";");
                    break;
                case "timeout":
                    sb.AppendLine($"            // Setting timeout to {param.Value}ms");
                    sb.AppendLine($"            var timeout = TimeSpan.FromMilliseconds({param.Value});");
                    break;
            }
        }

        // Add default implementation
        sb.AppendLine("            _logger.LogInformation(\"Method executed successfully\");");
        sb.AppendLine("            await Task.CompletedTask;");

        await Task.CompletedTask; // Ensure async method
        return sb.ToString();
    }

    private async Task<string> GenerateModifiedMethodBodyAsync(ParsedPrompt prompt)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("            // Original implementation modified");
        
        // Generate modifications based on parameters
        foreach (var param in prompt.Parameters)
        {
            switch (param.Key.ToLowerInvariant())
            {
                case "color":
                    sb.AppendLine($"            // Modified color to {param.Value}");
                    break;
                case "size":
                    sb.AppendLine($"            // Modified size to {param.Value}");
                    break;
                case "text":
                    sb.AppendLine($"            // Modified text to '{param.Value}'");
                    break;
            }
        }

        sb.AppendLine("            await Task.CompletedTask;");

        await Task.CompletedTask; // Ensure async method
        return sb.ToString();
    }

    private async Task<string> GenerateRemovalMethodBodyAsync(ParsedPrompt prompt)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("            // Removing functionality as requested");
        sb.AppendLine("            _logger.LogWarning(\"Functionality removed from {targetMethod}\");");
        sb.AppendLine("            await Task.CompletedTask;");

        await Task.CompletedTask; // Ensure async method
        return sb.ToString();
    }

    private async Task<string> GenerateFixedMethodBodyAsync(ParsedPrompt prompt)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("            // Fixed implementation");
        sb.AppendLine("            try");
        sb.AppendLine("            {");
        sb.AppendLine("                // Fixed code here");
        sb.AppendLine("                await Task.CompletedTask;");
        sb.AppendLine("            }");
        sb.AppendLine("            catch (Exception ex)");
        sb.AppendLine("            {");
        sb.AppendLine("                _logger.LogError(ex, \"Error in fixed {targetMethod}\");");
        sb.AppendLine("                throw;");
        sb.AppendLine("            }");

        await Task.CompletedTask; // Ensure async method
        return sb.ToString();
    }

    private async Task<string> GenerateOptimizedMethodBodyAsync(ParsedPrompt prompt)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("            // Optimized implementation");
        sb.AppendLine("            var tasks = new List<Task>();");
        sb.AppendLine("            ");
        sb.AppendLine("            // Parallel execution for better performance");
        sb.AppendLine("            tasks.Add(Task.Run(() => { /* optimized work */ }));");
        sb.AppendLine("            ");
        sb.AppendLine("            await Task.WhenAll(tasks);");

        await Task.CompletedTask; // Ensure async method
        return sb.ToString();
    }

    private string GenerateErrorCode(string errorMessage)
    {
        return $@"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class ErrorClass
{{
    private readonly ILogger _logger;

    public ErrorClass(ILogger logger)
    {{
        _logger = logger;
    }}

    public async Task ErrorMethod()
    {{
        _logger.LogError(""Code generation failed: {errorMessage}"");
        throw new InvalidOperationException(""Code generation failed: {errorMessage}"");
    }}
}}";
    }
}
