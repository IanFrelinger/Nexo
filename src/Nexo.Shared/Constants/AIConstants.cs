namespace Nexo.Shared
{
    /// <summary>
    /// Default AI configuration.
    /// </summary>
    public static class AIConstants
    {
        /// <summary>
        /// Default AI model name.
        /// </summary>
        public const string DefaultAIModel = "gpt-3.5-turbo";
        
        /// <summary>
        /// Default AI temperature.
        /// </summary>
        public const double DefaultAITemperature = 0.7;
        
        /// <summary>
        /// Default AI max tokens.
        /// </summary>
        public const int DefaultAIMaxTokens = 1000;
        
        /// <summary>
        /// Default AI timeout in seconds.
        /// </summary>
        public const int DefaultAITimeoutSeconds = 30;
        
        /// <summary>
        /// Default AI retry attempts.
        /// </summary>
        public const int DefaultAIRetryAttempts = 3;
    }
}
