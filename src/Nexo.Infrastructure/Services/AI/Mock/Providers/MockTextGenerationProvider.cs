using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.AI.Mock.Providers
{
    public class MockTextGenerationProvider
    {
        private readonly ILogger<MockTextGenerationProvider> _logger;

        public MockTextGenerationProvider(ILogger<MockTextGenerationProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TextGenerationResult> GenerateTextAsync(TextGenerationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Generating mock text for prompt: {Prompt}", request.Prompt);

                // Simulate processing time
                await Task.Delay(100, cancellationToken);

                var generatedText = GenerateMockText(request.Prompt, request.MaxTokens);
                
                return new TextGenerationResult
                {
                    GeneratedText = generatedText,
                    TokensUsed = generatedText.Split(' ').Length,
                    FinishReason = "stop",
                    ModelUsed = request.Model ?? "mock-model",
                    Success = true,
                    Message = "Text generated successfully"
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Text generation was cancelled");
                return new TextGenerationResult
                {
                    Success = false,
                    Message = "Text generation was cancelled",
                    Errors = { "Operation was cancelled" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating mock text");
                return new TextGenerationResult
                {
                    Success = false,
                    Message = $"Text generation failed: {ex.Message}",
                    Errors = { ex.Message }
                };
            }
        }

        private string GenerateMockText(string prompt, int maxTokens)
        {
            var responses = new[]
            {
                "This is a mock response to your request.",
                "Here's some generated text based on your prompt.",
                "Mock AI has processed your request and generated this response.",
                "This is a simulated text generation result.",
                "Your prompt has been processed by the mock AI system."
            };

            var random = new Random();
            var baseResponse = responses[random.Next(responses.Length)];
            
            // Add some context based on the prompt
            if (prompt.ToLower().Contains("code"))
            {
                baseResponse += " The generated code follows best practices and includes proper error handling.";
            }
            else if (prompt.ToLower().Contains("story"))
            {
                baseResponse += " The story continues with an engaging narrative that captures the reader's attention.";
            }
            else if (prompt.ToLower().Contains("explain"))
            {
                baseResponse += " This explanation provides a clear and comprehensive understanding of the topic.";
            }

            // Truncate if too long
            if (baseResponse.Length > maxTokens * 4) // Rough estimate: 4 chars per token
            {
                baseResponse = baseResponse.Substring(0, maxTokens * 4) + "...";
            }

            return baseResponse;
        }
    }
}
