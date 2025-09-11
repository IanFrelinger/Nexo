using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexo.Core.Domain.Entities.FeatureFactory.Integration
{
    /// <summary>
    /// Represents an integration endpoint for system integration
    /// </summary>
    public class IntegrationEndpoint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public EndpointType Type { get; set; } = EndpointType.REST;
        public string Method { get; set; } = "GET";
        public List<EndpointHeader> Headers { get; set; } = new();
        public List<EndpointParameter> Parameters { get; set; } = new();
        public EndpointAuthentication Authentication { get; set; } = new();
        public EndpointConfiguration Configuration { get; set; } = new();
        public EndpointStatus Status { get; set; } = EndpointStatus.Available;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastAccessedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents an endpoint header
    /// </summary>
    public class EndpointHeader
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents an endpoint parameter
    /// </summary>
    public class EndpointParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ParameterLocation Location { get; set; } = ParameterLocation.Query;
        public bool IsRequired { get; set; } = true;
        public string DefaultValue { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents endpoint authentication
    /// </summary>
    public class EndpointAuthentication
    {
        public AuthenticationType Type { get; set; } = AuthenticationType.None;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public bool IsEncrypted { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents endpoint configuration
    /// </summary>
    public class EndpointConfiguration
    {
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
        public bool EnableLogging { get; set; } = true;
        public bool EnableCaching { get; set; } = false;
        public int CacheTimeoutSeconds { get; set; } = 300;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of integration endpoints
    /// </summary>
    public enum EndpointType
    {
        REST,
        GraphQL,
        SOAP,
        gRPC,
        WebSocket,
        SignalR,
        Database,
        MessageQueue
    }

    /// <summary>
    /// Status of integration endpoints
    /// </summary>
    public enum EndpointStatus
    {
        Available,
        Busy,
        Maintenance,
        Unavailable,
        Error
    }

    /// <summary>
    /// Parameter locations
    /// </summary>
    public enum ParameterLocation
    {
        Query,
        Path,
        Header,
        Body,
        Form
    }

    /// <summary>
    /// Authentication types
    /// </summary>
    public enum AuthenticationType
    {
        None,
        Basic,
        Bearer,
        ApiKey,
        OAuth2,
        ClientCredentials
    }
}
