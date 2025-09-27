using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.AI.AzureOpenAI.Providers
{
    public partial class TextGenerationProvider
    {
        private readonly ILogger<TextGenerationProvider> _logger;

        public TextGenerationProvider(ILogger<TextGenerationProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ModelResponse> GenerateTextAsync(TextGenerationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Generating text with prompt: {PromptLength} characters", request.Prompt.Length);

                // Simulate Azure OpenAI text generation
                await Task.Delay(100, cancellationToken);

                var response = new ModelResponse
                {
                    Success = true,
                    GeneratedText = GenerateTextContent(request.Prompt),
                    TokensUsed = EstimateTokens(request.Prompt),
                    Model = request.Model ?? "gpt-3.5-turbo",
                    FinishReason = "stop"
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating text");
                return new ModelResponse
                {
                    Success = false,
                    Error = $"Text generation failed: {ex.Message}"
                };
            }
        }

        private string GenerateTextContent(string prompt)
        {
            // Simulate text generation based on prompt
            if (prompt.Contains("story"))
            {
                return "Once upon a time, in a land far away, there lived a brave knight who embarked on a quest to save the kingdom from an ancient dragon.";
            }
            else if (prompt.Contains("explain"))
            {
                return "This is a comprehensive explanation of the topic you've requested. The concept involves multiple interconnected components that work together to achieve the desired outcome.";
            }
            else
            {
                return "This is a generated response based on your prompt. The content has been created using advanced language models to provide relevant and coherent text.";
            }
        }

        private int EstimateTokens(string text)
        {
            // Rough estimation: 1 token ≈ 4 characters
            return (int)Math.Ceiling(text.Length / 4.0);
        }
    }
}
