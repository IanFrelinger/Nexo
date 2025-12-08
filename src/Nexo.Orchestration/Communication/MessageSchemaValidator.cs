using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Communication.Models;

namespace Nexo.Orchestration.Communication;

/// <summary>
/// Validates message schemas at message boundaries.
/// </summary>
public sealed class MessageSchemaValidator
{
    private readonly ILogger<MessageSchemaValidator> _logger;
    private readonly Dictionary<string, JsonSchema> _schemas = new();

    public MessageSchemaValidator(ILogger<MessageSchemaValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a schema for a message type.
    /// </summary>
    public void RegisterSchema(string messageType, JsonSchema schema)
    {
        _schemas[messageType] = schema;
        _logger.LogDebug("Registered schema for message type {MessageType}", messageType);
    }

    /// <summary>
    /// Validates a message against its schema.
    /// </summary>
    public bool Validate(AgentMessage message)
    {
        if (message == null)
        {
            return false;
        }

        if (!_schemas.TryGetValue(message.MessageType, out var schema))
        {
            // No schema registered - allow the message
            _logger.LogDebug("No schema registered for message type {MessageType}, allowing message", message.MessageType);
            return true;
        }

        try
        {
            // Basic validation - check if payload exists and is valid JSON
            if (message.Payload.HasValue)
            {
                // In a full implementation, we would validate against the JSON schema
                // For now, just check that it's valid JSON
                var jsonString = message.Payload.Value.GetRawText();
                JsonDocument.Parse(jsonString);
                return true;
            }

            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Message {MessageId} failed schema validation", message.MessageId);
            return false;
        }
    }

    /// <summary>
    /// Represents a JSON schema (simplified for now).
    /// </summary>
    public sealed class JsonSchema
    {
        public required string Schema { get; init; }
    }
}

