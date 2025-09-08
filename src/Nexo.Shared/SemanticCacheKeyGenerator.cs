using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Nexo.Shared
{
    /// <summary>
    /// Utility for generating semantic cache keys for AI model requests.
    /// </summary>
    public static class SemanticCacheKeyGenerator
    {
        /// <summary>
        /// Generates a semantic cache key from input, context, and model parameters.
        /// </summary>
        /// <param name="input">The main input (e.g., code or text).</param>
        /// <param name="context">Optional context dictionary.</param>
        /// <param name="modelParameters">Optional model parameters dictionary.</param>
        /// <returns>A SHA256-based hex string cache key.</returns>
        public static string Generate(
            string input,
            IDictionary<string, object>? context = null,
            IDictionary<string, object>? modelParameters = null)
        {
            // Normalize input: trim, lowercase, remove all whitespace
            var normalizedInput = Normalize(input);
            var sb = new StringBuilder();
            sb.Append(normalizedInput);

            if (context != null)
            {
                foreach (var kvp in context.OrderBy(k => k.Key))
                {
                    sb.Append($"|ctx:{kvp.Key}={kvp.Value}");
                }
            }
            if (modelParameters != null)
            {
                foreach (var kvp in modelParameters.OrderBy(k => k.Key))
                {
                    sb.Append($"|param:{kvp.Key}={kvp.Value}");
                }
            }
            var combined = sb.ToString();
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            // Remove all whitespace and lowercase
            var chars = input.Where(c => !char.IsWhiteSpace(c)).ToArray();
            return new string(chars).ToLowerInvariant();
        }
    }
} 