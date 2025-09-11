using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Represents configuration for system integration
    /// </summary>
    public class IntegrationConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IntegrationType Type { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
        public Dictionary<string, string> Parameters { get; set; } = new();
        public AuthenticationType AuthenticationType { get; set; }
        public string AuthenticationToken { get; set; } = string.Empty;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public int RetryCount { get; set; } = 3;
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum IntegrationType
    {
        REST,
        GraphQL,
        SOAP,
        MessageQueue,
        Database,
        FileSystem,
        WebSocket
    }

    public enum AuthenticationType
    {
        None,
        Basic,
        Bearer,
        ApiKey,
        OAuth2,
        Certificate
    }
}
