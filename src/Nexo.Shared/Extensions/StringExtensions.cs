using System;

namespace Nexo.Shared.Extensions
{
    /// <summary>
    /// Extension methods for string operations
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Checks if a string is null or empty
        /// </summary>
        public static bool IsNullOrEmpty(this string? value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Checks if a string is null or whitespace
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Gets a default value if the string is null or empty
        /// </summary>
        public static string DefaultIfNullOrEmpty(this string? value, string defaultValue)
        {
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        /// <summary>
        /// Gets a default value if the string is null or whitespace
        /// </summary>
        public static string DefaultIfNullOrWhiteSpace(this string? value, string defaultValue)
        {
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }
    }
}
