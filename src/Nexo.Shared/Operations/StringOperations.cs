using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Shared.Operations
{
    /// <summary>
    /// Default implementation of string operations
    /// </summary>
    public class StringOperations : IStringOperations
    {
        public async Task<bool> IsNullOrEmptyAsync(string value)
        {
            await Task.CompletedTask;
            return string.IsNullOrEmpty(value);
        }
        
        public async Task<bool> IsNullOrWhiteSpaceAsync(string value)
        {
            await Task.CompletedTask;
            return string.IsNullOrWhiteSpace(value);
        }
        
        public async Task<string> TrimAsync(string value)
        {
            await Task.CompletedTask;
            return value?.Trim() ?? string.Empty;
        }
        
        public async Task<string> ToUpperAsync(string value)
        {
            await Task.CompletedTask;
            return value?.ToUpper() ?? string.Empty;
        }
        
        public async Task<string> ToLowerAsync(string value)
        {
            await Task.CompletedTask;
            return value?.ToLower() ?? string.Empty;
        }
        
        public async Task<IReadOnlyList<string>> SplitAsync(string value, string delimiter)
        {
            await Task.CompletedTask;
            if (string.IsNullOrEmpty(value))
                return new List<string>();
            
            return value.Split(new[] { delimiter }, StringSplitOptions.None);
        }
        
        public async Task<string> JoinAsync(IReadOnlyList<string> values, string delimiter)
        {
            await Task.CompletedTask;
            if (values == null || values.Count == 0)
                return string.Empty;
            
            return string.Join(delimiter, values);
        }
        
        public async Task<string> ReplaceAsync(string value, string oldValue, string newValue)
        {
            await Task.CompletedTask;
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            
            return value.Replace(oldValue, newValue);
        }
        
        public async Task<bool> ContainsAsync(string value, string substring)
        {
            await Task.CompletedTask;
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(substring))
                return false;
            
            return value.Contains(substring);
        }
        
        public async Task<bool> StartsWithAsync(string value, string substring)
        {
            await Task.CompletedTask;
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(substring))
                return false;
            
            return value.StartsWith(substring);
        }
        
        public async Task<bool> EndsWithAsync(string value, string substring)
        {
            await Task.CompletedTask;
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(substring))
                return false;
            
            return value.EndsWith(substring);
        }
        
        public async Task<string> SubstringAsync(string value, int startIndex, int length = -1)
        {
            await Task.CompletedTask;
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            
            if (length == -1)
                return value.Substring(startIndex);
            
            return value.Substring(startIndex, length);
        }
    }
}
