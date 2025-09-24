using System;
using System.Collections.Generic;

namespace Nexo.Demo.Tests.Support
{
    /// <summary>
    /// Manages environment variables for demo scenarios
    /// </summary>
    public static class EnvironmentManager
    {
        private static readonly Dictionary<string, string?> _originalEnv = new();

        /// <summary>
        /// Sets up environment variables for a demo scenario
        /// </summary>
        public static void SetupEnvironment(IDictionary<string, string>? env = null)
        {
            // Always clear Nexo-specific environment variables first to ensure test isolation
            var nexoSpecificVars = new[] { 
                "NEXO_AI_MODE", 
                "NEXO_PROVIDER", 
                "NEXO_POLICY_ENABLED", 
                "NEXO_POLICY_TRIGGER", 
                "NEXO_POLICY_STRICT", 
                "NEXO_AUDIT_ENABLED" 
            };
            
            foreach (var varName in nexoSpecificVars)
            {
                _originalEnv[varName] = Environment.GetEnvironmentVariable(varName);
                Environment.SetEnvironmentVariable(varName, null);
            }
            
            // Set environment variables if provided
            if (env != null)
            {
                foreach (var kvp in env)
                {
                    _originalEnv[kvp.Key] = Environment.GetEnvironmentVariable(kvp.Key);
                    Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Restores original environment variables
        /// </summary>
        public static void RestoreEnvironment()
        {
            foreach (var kvp in _originalEnv)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
            _originalEnv.Clear();
        }
    }
}
