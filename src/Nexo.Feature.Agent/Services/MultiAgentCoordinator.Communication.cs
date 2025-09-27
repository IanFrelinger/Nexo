using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Agent-to-agent communication functionality
    /// </summary>
    public partial class MultiAgentCoordinator
    {
        /// <summary>
        /// Facilitates communication between agents asynchronously.
        /// </summary>
        /// <param name="request">The request object containing details of the communication, including sender, recipient, and message information.</param>
        /// <param name="cancellationToken">An optional token used to propagate notifications of task cancellation.</param>
        /// <returns>A task that represents the asynchronous operation, containing a result with details about the communication process.</returns>
        public async Task<AgentCommunicationResult> FacilitateCommunicationAsync(AgentCommunicationRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Facilitating communication from {SenderId} to {RecipientId}", 
                request.SenderAgentId, request.RecipientAgentId);

            try
            {
                // Validate agents exist
                if (!_registeredAgents.ContainsKey(request.SenderAgentId))
                {
                    throw new ArgumentException($"Sender agent {request.SenderAgentId} not found");
                }

                if (!_registeredAgents.ContainsKey(request.RecipientAgentId))
                {
                    throw new ArgumentException($"Recipient agent {request.RecipientAgentId} not found");
                }

                var sender = _registeredAgents[request.SenderAgentId];
                var recipient = _registeredAgents[request.RecipientAgentId];

                // Create communication context
                var context = new AgentCommunicationContext
                {
                    CommunicationId = Guid.NewGuid().ToString(),
                    SenderAgent = sender,
                    RecipientAgent = recipient,
                    Message = request.Message,
                    MessageType = request.MessageType,
                    Priority = request.Priority,
                    Timestamp = DateTime.UtcNow
                };

                // Process communication through recipient agent
                var aiRequest = new AiEnhancedAgentRequest
                {
                    Type = AgentRequestType.Communication,
                    Content = request.Message,
                    UseAi = true,
                    AiContext = new Dictionary<string, object>
                    {
                        ["senderAgent"] = sender.Name.Value,
                        ["senderRole"] = sender.Role.Value,
                        ["messageType"] = request.MessageType.ToString(),
                        ["priority"] = request.Priority.ToString()
                    }
                };

                var response = await recipient.ProcessAiRequestAsync(aiRequest, cancellationToken);

                return new AgentCommunicationResult
                {
                    CommunicationId = context.CommunicationId,
                    Success = response.Success,
                    Response = response.Content,
                    ProcessingTimeMs = response.AiProcessingTimeMs,
                    AiWasUsed = response.AiWasUsed,
                    AiModelUsed = response.AiModelUsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error facilitating agent communication");
                return new AgentCommunicationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
