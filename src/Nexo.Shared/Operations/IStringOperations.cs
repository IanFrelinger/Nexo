using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Shared.Operations
{
    /// <summary>
    /// Interface for string operations (replaces string methods)
    /// </summary>
    public interface IStringOperations
    {
        /// <summary>
        /// Checks if a string is null or empty
        /// </summary>
        Task<bool> IsNullOrEmptyAsync(string value);
        
        /// <summary>
        /// Checks if a string is null or whitespace
        /// </summary>
        Task<bool> IsNullOrWhiteSpaceAsync(string value);
        
        /// <summary>
        /// Trims whitespace from a string
        /// </summary>
        Task<string> TrimAsync(string value);
        
        /// <summary>
        /// Converts string to uppercase
        /// </summary>
        Task<string> ToUpperAsync(string value);
        
        /// <summary>
        /// Converts string to lowercase
        /// </summary>
        Task<string> ToLowerAsync(string value);
        
        /// <summary>
        /// Splits a string by delimiter
        /// </summary>
        Task<IReadOnlyList<string>> SplitAsync(string value, string delimiter);
        
        /// <summary>
        /// Joins strings with a delimiter
        /// </summary>
        Task<string> JoinAsync(IReadOnlyList<string> values, string delimiter);
        
        /// <summary>
        /// Replaces occurrences in a string
        /// </summary>
        Task<string> ReplaceAsync(string value, string oldValue, string newValue);
        
        /// <summary>
        /// Checks if string contains a substring
        /// </summary>
        Task<bool> ContainsAsync(string value, string substring);
        
        /// <summary>
        /// Checks if string starts with a substring
        /// </summary>
        Task<bool> StartsWithAsync(string value, string substring);
        
        /// <summary>
        /// Checks if string ends with a substring
        /// </summary>
        Task<bool> EndsWithAsync(string value, string substring);
        
        /// <summary>
        /// Gets substring from a string
        /// </summary>
        Task<string> SubstringAsync(string value, int startIndex, int length = -1);
    }
}
