using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.CLI.Interactive
{
    /// <summary>
    /// Command completion functionality for command suggestion engine.
    /// </summary>
    public partial class CommandSuggestionEngine
    {
        public async Task<IEnumerable<string>> GetCompletionsAsync(string partialInput)
        {
            var tokens = partialInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (tokens.Length == 0)
            {
                return await GetTopLevelCommands();
            }
            
            if (tokens.Length == 1)
            {
                return await GetMatchingCommands(tokens[0]);
            }
            
            return await GetParameterCompletions(tokens);
        }
        
        private async Task<IEnumerable<string>> GetTopLevelCommands()
        {
            return _availableCommands.Keys.OrderBy(k => k);
        }
        
        private async Task<IEnumerable<string>> GetMatchingCommands(string partialCommand)
        {
            return _availableCommands.Keys
                .Where(cmd => cmd.StartsWith(partialCommand, StringComparison.OrdinalIgnoreCase))
                .OrderBy(cmd => cmd);
        }
        
        private async Task<IEnumerable<string>> GetParameterCompletions(string[] tokens)
        {
            // This would be enhanced to provide parameter-specific completions
            // For now, return empty list
            return new List<string>();
        }
    }
}
