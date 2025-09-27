using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.AI.Mock.Providers
{
    public partial class MockChatCompletionProvider
    {
        private readonly ILogger<MockChatCompletionProvider> _logger;

        public MockChatCompletionProvider(ILogger<MockChatCompletionProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ChatCompletionResult> GetChatCompletionAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Generating mock chat completion for {MessageCount} messages", request.Messages.Count);

                // Simulate processing time
                await Task.Delay(300, cancellationToken);

                var response = GenerateMockResponse(request.Messages);
                var choice = new ChatChoice
                {
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        Content = response
                    },
                    FinishReason = "stop",
                    Index = 0
                };

                return new ChatCompletionResult
                {
                    Choices = new List<ChatChoice> { choice },
                    TokensUsed = response.Split(' ').Length,
                    ModelUsed = request.Model ?? "mock-chat-model",
                    Success = true,
                    Message = "Chat completion generated successfully"
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Chat completion was cancelled");
                return new ChatCompletionResult
                {
                    Success = false,
                    Message = "Chat completion was cancelled",
                    Errors = { "Operation was cancelled" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating mock chat completion");
                return new ChatCompletionResult
                {
                    Success = false,
                    Message = $"Chat completion failed: {ex.Message}",
                    Errors = { ex.Message }
                };
            }
        }

        private string GenerateMockResponse(List<ChatMessage> messages)
        {
            if (!messages.Any())
            {
                return "Hello! How can I help you today?";
            }

            var lastMessage = messages.Last().Content.ToLower();
            
            // Generate contextual responses based on the last message
            if (lastMessage.Contains("hello") || lastMessage.Contains("hi"))
            {
                return "Hello! I'm a mock AI assistant. How can I help you today?";
            }
            else if (lastMessage.Contains("code") || lastMessage.Contains("programming"))
            {
                return "I'd be happy to help with coding! Here's a mock code example:\n\n```csharp\npublic partial class Example\n{\n    public void DoSomething()\n    {\n        Console.WriteLine(\"Hello, World!\");\n    }\n}\n```";
            }
            else if (lastMessage.Contains("explain") || lastMessage.Contains("what is"))
            {
                return "I'd be glad to explain! Based on your question, here's a mock explanation that covers the key concepts and provides a comprehensive understanding of the topic.";
            }
            else if (lastMessage.Contains("help"))
            {
                return "I'm here to help! I can assist with various tasks including:\n- Code generation and review\n- Problem solving\n- Explanations and tutorials\n- General questions\n\nWhat would you like to work on?";
            }
            else if (lastMessage.Contains("thank"))
            {
                return "You're welcome! I'm glad I could help. Feel free to ask if you have any other questions!";
            }
            else
            {
                return "That's an interesting question! Here's a mock response that addresses your query with relevant information and suggestions.";
            }
        }
    }
}
