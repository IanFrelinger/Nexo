using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.AI.AzureOpenAI.Providers
{
    public class ChatCompletionProvider
    {
        private readonly ILogger<ChatCompletionProvider> _logger;

        public ChatCompletionProvider(ILogger<ChatCompletionProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ChatCompletionResponse> CompleteChatAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Completing chat with {MessageCount} messages", request.Messages.Count);

                // Simulate Azure OpenAI chat completion
                await Task.Delay(300, cancellationToken);

                var response = new ChatCompletionResponse
                {
                    Success = true,
                    Choices = GenerateChatChoices(request),
                    Model = request.Model ?? "gpt-3.5-turbo",
                    Usage = new ChatUsage
                    {
                        PromptTokens = EstimateTokens(request.Messages),
                        CompletionTokens = EstimateCompletionTokens(request.Messages),
                        TotalTokens = EstimateTotalTokens(request.Messages)
                    }
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing chat");
                return new ChatCompletionResponse
                {
                    Success = false,
                    Error = $"Chat completion failed: {ex.Message}"
                };
            }
        }

        private List<ChatChoice> GenerateChatChoices(ChatCompletionRequest request)
        {
            var choices = new List<ChatChoice>();
            
            // Generate a response based on the last user message
            var lastMessage = request.Messages[request.Messages.Count - 1];
            var response = GenerateChatResponse(lastMessage.Content);
            
            choices.Add(new ChatChoice
            {
                Index = 0,
                Message = new ChatMessage
                {
                    Role = "assistant",
                    Content = response
                },
                FinishReason = "stop"
            });
            
            return choices;
        }

        private string GenerateChatResponse(string userMessage)
        {
            if (userMessage.Contains("hello") || userMessage.Contains("hi"))
            {
                return "Hello! How can I assist you today?";
            }
            else if (userMessage.Contains("help"))
            {
                return "I'm here to help! I can assist with various tasks including text generation, code writing, analysis, and answering questions. What would you like to know?";
            }
            else if (userMessage.Contains("code"))
            {
                return "I'd be happy to help you with code! I can generate code in various programming languages, debug existing code, explain code concepts, and provide best practices. What specific coding task do you need help with?";
            }
            else
            {
                return "I understand your message. I'm an AI assistant designed to help with a wide range of tasks. Could you please provide more details about what you'd like me to help you with?";
            }
        }

        private int EstimateTokens(List<ChatMessage> messages)
        {
            var totalText = string.Join(" ", messages.ConvertAll(m => m.Content));
            return (int)Math.Ceiling(totalText.Length / 4.0);
        }

        private int EstimateCompletionTokens(List<ChatMessage> messages)
        {
            // Estimate completion tokens (typically shorter than prompt)
            return EstimateTokens(messages) / 3;
        }

        private int EstimateTotalTokens(List<ChatMessage> messages)
        {
            return EstimateTokens(messages) + EstimateCompletionTokens(messages);
        }
    }
}
