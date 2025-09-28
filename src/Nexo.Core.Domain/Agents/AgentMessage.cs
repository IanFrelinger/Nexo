using Nexo.Core.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Agents
{
    /// <summary>
    /// Represents a message sent between agents
    /// </summary>
    public sealed class AgentMessage
    {
        public string Id { get; }
        public AgentId SenderId { get; }
        public AgentId RecipientId { get; }
        public MessageType Type { get; }
        public string Subject { get; }
        public string Content { get; }
        public IReadOnlyDictionary<string, object> Metadata { get; }
        public MessagePriority Priority { get; }
        public DateTimeOffset Timestamp { get; }
        public DateTimeOffset? ExpiresAt { get; }
        public bool RequiresResponse { get; }
        public string? ResponseToMessageId { get; }

        public AgentMessage(
            string id,
            AgentId senderId,
            AgentId recipientId,
            MessageType type,
            string subject,
            string content,
            MessagePriority priority = MessagePriority.Normal,
            IDictionary<string, object>? metadata = null,
            DateTimeOffset? expiresAt = null,
            bool requiresResponse = false,
            string? responseToMessageId = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            SenderId = senderId ?? throw new ArgumentNullException(nameof(senderId));
            RecipientId = recipientId ?? throw new ArgumentNullException(nameof(recipientId));
            Type = type;
            Subject = subject ?? throw new ArgumentNullException(nameof(subject));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Priority = priority;
            Metadata = (metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>()).AsReadOnly();
            Timestamp = DateTimeOffset.UtcNow;
            ExpiresAt = expiresAt;
            RequiresResponse = requiresResponse;
            ResponseToMessageId = responseToMessageId;
        }

        /// <summary>
        /// Create a request message
        /// </summary>
        public static AgentMessage CreateRequest(
            string id,
            AgentId senderId,
            AgentId recipientId,
            string subject,
            string content,
            MessagePriority priority = MessagePriority.Normal,
            IDictionary<string, object>? metadata = null,
            DateTimeOffset? expiresAt = null)
        {
            return new AgentMessage(
                id,
                senderId,
                recipientId,
                MessageType.Request,
                subject,
                content,
                priority,
                metadata,
                expiresAt,
                requiresResponse: true);
        }

        /// <summary>
        /// Create a response message
        /// </summary>
        public static AgentMessage CreateResponse(
            string id,
            AgentId senderId,
            AgentId recipientId,
            string subject,
            string content,
            string originalMessageId,
            MessagePriority priority = MessagePriority.Normal,
            IDictionary<string, object>? metadata = null)
        {
            return new AgentMessage(
                id,
                senderId,
                recipientId,
                MessageType.Response,
                subject,
                content,
                priority,
                metadata,
                responseToMessageId: originalMessageId);
        }

        /// <summary>
        /// Create a notification message
        /// </summary>
        public static AgentMessage CreateNotification(
            string id,
            AgentId senderId,
            AgentId recipientId,
            string subject,
            string content,
            MessagePriority priority = MessagePriority.Normal,
            IDictionary<string, object>? metadata = null)
        {
            return new AgentMessage(
                id,
                senderId,
                recipientId,
                MessageType.Notification,
                subject,
                content,
                priority,
                metadata);
        }

        /// <summary>
        /// Create a broadcast message
        /// </summary>
        public static AgentMessage CreateBroadcast(
            string id,
            AgentId senderId,
            string subject,
            string content,
            MessagePriority priority = MessagePriority.Normal,
            IDictionary<string, object>? metadata = null)
        {
            return new AgentMessage(
                id,
                senderId,
                AgentId.Broadcast, // Special ID for broadcast
                MessageType.Broadcast,
                subject,
                content,
                priority,
                metadata);
        }

        /// <summary>
        /// Create a status update message
        /// </summary>
        public static AgentMessage CreateStatusUpdate(
            string id,
            AgentId senderId,
            AgentId recipientId,
            string status,
            string details,
            MessagePriority priority = MessagePriority.Low,
            IDictionary<string, object>? metadata = null)
        {
            var fullMetadata = new Dictionary<string, object>(metadata ?? new Dictionary<string, object>())
            {
                ["status"] = status,
                ["details"] = details
            };

            return new AgentMessage(
                id,
                senderId,
                recipientId,
                MessageType.StatusUpdate,
                $"Status Update: {status}",
                details,
                priority,
                fullMetadata);
        }

        /// <summary>
        /// Create a task assignment message
        /// </summary>
        public static AgentMessage CreateTaskAssignment(
            string id,
            AgentId senderId,
            AgentId recipientId,
            string taskId,
            string taskName,
            string taskDescription,
            MessagePriority priority = MessagePriority.High,
            IDictionary<string, object>? metadata = null)
        {
            var fullMetadata = new Dictionary<string, object>(metadata ?? new Dictionary<string, object>())
            {
                ["taskId"] = taskId,
                ["taskName"] = taskName,
                ["taskDescription"] = taskDescription
            };

            return new AgentMessage(
                id,
                senderId,
                recipientId,
                MessageType.TaskAssignment,
                $"Task Assignment: {taskName}",
                taskDescription,
                priority,
                fullMetadata,
                requiresResponse: true);
        }

        /// <summary>
        /// Create a capability request message
        /// </summary>
        public static AgentMessage CreateCapabilityRequest(
            string id,
            AgentId senderId,
            AgentId recipientId,
            string capabilityName,
            string reason,
            MessagePriority priority = MessagePriority.Normal,
            IDictionary<string, object>? metadata = null)
        {
            var fullMetadata = new Dictionary<string, object>(metadata ?? new Dictionary<string, object>())
            {
                ["capabilityName"] = capabilityName,
                ["reason"] = reason
            };

            return new AgentMessage(
                id,
                senderId,
                recipientId,
                MessageType.CapabilityRequest,
                $"Capability Request: {capabilityName}",
                reason,
                priority,
                fullMetadata,
                requiresResponse: true);
        }

        /// <summary>
        /// Get a metadata value
        /// </summary>
        public T? GetMetadata<T>(string key)
        {
            if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }

        /// <summary>
        /// Check if message has expired
        /// </summary>
        public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;

        /// <summary>
        /// Check if message is urgent
        /// </summary>
        public bool IsUrgent => Priority == MessagePriority.Critical || 
                               (ExpiresAt.HasValue && DateTimeOffset.UtcNow.AddMinutes(5) > ExpiresAt.Value);

        /// <summary>
        /// Check if this is a broadcast message
        /// </summary>
        public bool IsBroadcast => RecipientId.Equals(AgentId.Broadcast);

        /// <summary>
        /// Check if this is a response message
        /// </summary>
        public bool IsResponse => !string.IsNullOrEmpty(ResponseToMessageId);
    }

    /// <summary>
    /// Types of messages that can be sent between agents
    /// </summary>
    public enum MessageType
    {
        Request,
        Response,
        Notification,
        Broadcast,
        StatusUpdate,
        TaskAssignment,
        CapabilityRequest,
        Error,
        Warning,
        Info
    }

    /// <summary>
    /// Priority levels for messages
    /// </summary>
    public enum MessagePriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }
}
