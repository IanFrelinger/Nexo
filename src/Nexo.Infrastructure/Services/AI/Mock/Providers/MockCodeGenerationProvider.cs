using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.AI.Mock.Providers
{
    public partial class MockCodeGenerationProvider
    {
        private readonly ILogger<MockCodeGenerationProvider> _logger;

        public MockCodeGenerationProvider(ILogger<MockCodeGenerationProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CodeGenerationResult> GenerateCodeAsync(CodeGenerationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Generating mock code for prompt: {Prompt}", request.Prompt);

                // Simulate processing time
                await Task.Delay(150, cancellationToken);

                var generatedCode = GenerateMockCode(request.Prompt, request.Language);
                
                return new CodeGenerationResult
                {
                    GeneratedCode = generatedCode,
                    Language = request.Language,
                    TokensUsed = generatedCode.Split('\n').Length * 10, // Rough estimate
                    FinishReason = "stop",
                    ModelUsed = request.Model ?? "mock-code-model",
                    Success = true,
                    Message = "Code generated successfully"
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Code generation was cancelled");
                return new CodeGenerationResult
                {
                    Success = false,
                    Message = "Code generation was cancelled",
                    Errors = { "Operation was cancelled" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating mock code");
                return new CodeGenerationResult
                {
                    Success = false,
                    Message = $"Code generation failed: {ex.Message}",
                    Errors = { ex.Message }
                };
            }
        }

        private string GenerateMockCode(string prompt, string language)
        {
            return language.ToLower() switch
            {
                "csharp" or "c#" => GenerateCSharpCode(prompt),
                "javascript" or "js" => GenerateJavaScriptCode(prompt),
                "python" => GeneratePythonCode(prompt),
                "java" => GenerateJavaCode(prompt),
                "typescript" or "ts" => GenerateTypeScriptCode(prompt),
                _ => GenerateGenericCode(prompt, language)
            };
        }

        private string GenerateCSharpCode(string prompt)
        {
            return $@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeneratedCode
{{
    public partial class {GetClassName(prompt)}
    {{
        public async Task<string> ProcessAsync()
        {{
            // Generated code for: {prompt}
            await Task.Delay(100);
            return ""Mock result"";
        }}
    }}
}}";
        }

        private string GenerateJavaScriptCode(string prompt)
        {
            return $@"// Generated code for: {prompt}
function processRequest() {{
    return new Promise((resolve) => {{
        setTimeout(() => {{
            resolve('Mock result');
        }}, 100);
    }});
}}

module.exports = {{ processRequest }};";
        }

        private string GeneratePythonCode(string prompt)
        {
            return $@"# Generated code for: {prompt}
import asyncio

async def process_request():
    # Implementation for: {prompt}
    await asyncio.sleep(0.1)
    return 'Mock result'

if __name__ == '__main__':
    result = asyncio.run(process_request())
    print(result)";
        }

        private string GenerateJavaCode(string prompt)
        {
            return $@"import java.util.concurrent.CompletableFuture;

public partial class {GetClassName(prompt)} {{
    public CompletableFuture<String> processAsync() {{
        return CompletableFuture.supplyAsync(() -> {{
            // Generated code for: {prompt}
            try {{
                Thread.sleep(100);
            }} catch (InterruptedException e) {{
                Thread.currentThread().interrupt();
            }}
            return ""Mock result"";
        }});
    }}
}}";
        }

        private string GenerateTypeScriptCode(string prompt)
        {
            return $@"// Generated code for: {prompt}
interface ProcessResult {{
    success: boolean;
    data: string;
}}

async function processRequest(): Promise<ProcessResult> {{
    // Implementation for: {prompt}
    await new Promise(resolve => setTimeout(resolve, 100));
    return {{
        success: true,
        data: 'Mock result'
    }};
}}

export {{ processRequest }};";
        }

        private string GenerateGenericCode(string prompt, string language)
        {
            return $@"// Generated {language} code for: {prompt}
// TODO: Implement the requested functionality
// This is a placeholder implementation";
        }

        private string GetClassName(string prompt)
        {
            var words = prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join("", words.Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower()));
        }
    }
}
