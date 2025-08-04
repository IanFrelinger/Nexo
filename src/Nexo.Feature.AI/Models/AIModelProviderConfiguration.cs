using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Configuration for AI model provider settings.
    /// </summary>
    public class AIModelProviderConfiguration
    {
        /// <summary>
        /// The name of the AI model provider.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "OpenAI";
        
        /// <summary>
        /// The API endpoint URL for the provider.
        /// </summary>
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = "https://api.openai.com/v1";
        
        /// <summary>
        /// The API key for authentication.
        /// </summary>
        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = "";
        
        /// <summary>
        /// The organization ID (if applicable).
        /// </summary>
        [JsonPropertyName("organizationId")]
        public string OrganizationId { get; set; }
        
        /// <summary>
        /// The project ID (if applicable).
        /// </summary>
        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; }
        
        /// <summary>
        /// The deployment name (if applicable).
        /// </summary>
        [JsonPropertyName("deploymentName")]
        public string DeploymentName { get; set; }
        
        /// <summary>
        /// The API version to use.
        /// </summary>
        [JsonPropertyName("apiVersion")]
        public string ApiVersion { get; set; } = "2024-02-15-preview";
        
        /// <summary>
        /// The maximum number of concurrent requests.
        /// </summary>
        [JsonPropertyName("maxConcurrentRequests")]
        public int MaxConcurrentRequests { get; set; } = 10;
        
        /// <summary>
        /// The rate limit in requests per minute.
        /// </summary>
        [JsonPropertyName("rateLimitPerMinute")]
        public int RateLimitPerMinute { get; set; } = 60;
        
        /// <summary>
        /// The rate limit in requests per hour.
        /// </summary>
        [JsonPropertyName("rateLimitPerHour")]
        public int RateLimitPerHour { get; set; } = 1000;
        
        /// <summary>
        /// Whether to enable request logging.
        /// </summary>
        [JsonPropertyName("enableRequestLogging")]
        public bool EnableRequestLogging { get; set; } = false;
        
        /// <summary>
        /// Whether to enable response logging.
        /// </summary>
        [JsonPropertyName("enableResponseLogging")]
        public bool EnableResponseLogging { get; set; } = false;
        
        /// <summary>
        /// Custom headers to include in requests.
        /// </summary>
        [JsonPropertyName("customHeaders")]
        public Dictionary<string, string> CustomHeaders { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Custom parameters for the provider.
        /// </summary>
        [JsonPropertyName("customParameters")]
        public Dictionary<string, object> CustomParameters { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// The timeout for API requests in seconds.
        /// </summary>
        [JsonPropertyName("requestTimeoutSeconds")]
        public int RequestTimeoutSeconds { get; set; } = 120;
        
        /// <summary>
        /// Whether to enable automatic retry on failure.
        /// </summary>
        [JsonPropertyName("enableAutoRetry")]
        public bool EnableAutoRetry { get; set; } = true;
        
        /// <summary>
        /// The maximum number of retry attempts.
        /// </summary>
        [JsonPropertyName("maxRetryAttempts")]
        public int MaxRetryAttempts { get; set; } = 3;
        
        /// <summary>
        /// The delay between retry attempts in seconds.
        /// </summary>
        [JsonPropertyName("retryDelaySeconds")]
        public int RetryDelaySeconds { get; set; } = 5;
        
        /// <summary>
        /// Whether to use exponential backoff for retries.
        /// </summary>
        [JsonPropertyName("useExponentialBackoff")]
        public bool UseExponentialBackoff { get; set; } = true;
    }
} 