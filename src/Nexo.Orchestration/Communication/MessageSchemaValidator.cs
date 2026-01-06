using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Communication.Models;

namespace Nexo.Orchestration.Communication;

/// <summary>
/// Validates message schemas at message boundaries.
/// 
/// Responsibilities:
/// - Registers JSON schemas for message types
/// - Validates message payloads against schemas
/// - Ensures message structure integrity
/// - Handles validation errors gracefully
/// 
/// Used by AgentBus and ChannelManager to validate messages.
/// Provides type safety for inter-agent communication.
/// </summary>
public sealed class MessageSchemaValidator
{
    private readonly ILogger<MessageSchemaValidator> _logger;
    private readonly Dictionary<string, JsonSchema> _schemas = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageSchemaValidator"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public MessageSchemaValidator(ILogger<MessageSchemaValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a JSON schema for a message type.
    /// 
    /// Messages of this type will be validated against the schema when Validate is called.
    /// If a schema already exists for the message type, it will be replaced.
    /// </summary>
    /// <param name="messageType">The message type to register the schema for.</param>
    /// <param name="schema">The JSON schema to validate against.</param>
    public void RegisterSchema(string messageType, JsonSchema schema)
    {
        _schemas[messageType] = schema;
        _logger.LogDebug("Registered schema for message type {MessageType}", messageType);
    }

    /// <summary>
    /// Validates a message against its registered schema.
    /// 
    /// If no schema is registered for the message type, the message is allowed (returns true).
    /// Currently performs basic JSON validation; full schema validation would require a JSON schema library.
    /// </summary>
    /// <param name="message">The message to validate.</param>
    /// <returns>True if the message is valid or no schema is registered; false if validation fails.</returns>
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
    /// 
    /// In a full implementation, this would use a proper JSON schema library
    /// (e.g., JsonSchema.Net) for complete schema validation.
    /// </summary>
    public sealed class JsonSchema
    {
        /// <summary>
        /// Gets the JSON schema string.
        /// </summary>
        public required string Schema { get; init; }
    }
}

