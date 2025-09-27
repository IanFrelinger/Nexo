using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Entities.Pipeline;
using System;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Pipeline
{
    /// <summary>
    /// Test generation functionality
    /// </summary>
    public partial class AITestingStep
    {
        private string GetModelPathForTesting(AIEngineType engineType)
        {
            return engineType switch
            {
                AIEngineType.LlamaWebAssembly => "models/codellama-7b-instruct.gguf",
                AIEngineType.LlamaNative => "models/codellama-13b-instruct.gguf",
                _ => "models/codellama-7b-instruct.gguf"
            };
        }

        private async Task<string> GenerateTestCodeAsync(IAIEngine aiEngine, TestingRequest request, PipelineContext context)
        {
            // Create a code generation request for test code
            var testPrompt = CreateTestPrompt(request, context);
            
            var codeGenRequest = new Nexo.Core.Domain.Entities.AI.CodeGenerationRequest
            {
                Prompt = testPrompt,
                Language = request.Language,
                MaxTokens = 2048,
                Temperature = 0.3
            };

            // Generate test code using the AI engine
            var testCodeResult = await aiEngine.GenerateCodeAsync(codeGenRequest);
            
            return testCodeResult.GeneratedCode;
        }

        private string CreateTestPrompt(TestingRequest request, PipelineContext context)
        {
            var prompt = $@"Generate comprehensive {request.TestType} tests for the following {request.Language} code:

```{request.Language.ToString().ToLower()}
{request.Code}
```

Requirements:
- Test all public methods and properties
- Include edge cases and boundary conditions
- Add proper test setup and teardown
- Use appropriate testing framework for {request.Language}
- Include meaningful test names and descriptions
- Add assertions for expected behavior
- Consider error handling scenarios

Context: {request.Context}
Platform: {context.EnvironmentProfile?.CurrentPlatform ?? Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Unknown}

Generate complete, runnable test code:";

            return prompt;
        }
    }
}
