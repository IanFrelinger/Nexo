using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Entities.AI;

namespace Nexo.Core.Domain.Common
{
    /// <summary>
    /// Base class for all domain requests following CQRS pattern
    /// </summary>
    public abstract class BaseRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RequestType { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Base class for code-related requests
    /// </summary>
    public abstract class CodeRequest : BaseRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Framework { get; set; } = string.Empty;
        public List<string> Criteria { get; set; } = new();
        public Dictionary<string, object> Requirements { get; set; } = new();
    }

    /// <summary>
    /// Base class for AI operation requests
    /// </summary>
    public abstract class AIOperationRequest : CodeRequest
    {
        public AIRequirements AIRequirements { get; set; } = new();
        public int MaxTokens { get; set; } = 1000;
        public double Temperature { get; set; } = 0.7;
        public string OperationType { get; set; } = string.Empty;
    }
}
