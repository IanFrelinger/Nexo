using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nexo.Infrastructure.Commands.Chat.Utilities
{
    /// <summary>
    /// Handles code highlighting functionality
    /// </summary>
    public partial class CodeHighlighter
    {
        /// <summary>
        /// Highlights code blocks in the response
        /// </summary>
        public string HighlightCodeBlocks(string text)
        {
            // Simple code block highlighting using Spectre.Console markup
            var codeBlockPattern = @"```(\w+)?\n(.*?)```";
            var matches = Regex.Matches(text, codeBlockPattern, RegexOptions.Singleline);

            foreach (Match match in matches.Cast<Match>().Reverse())
            {
                var language = match.Groups[1].Value;
                var code = match.Groups[2].Value.Trim();
                
                var highlightedCode = $"[dim]```{language}[/]\n[bold]{code}[/]\n[dim]```[/]";
                text = text.Substring(0, match.Index) + highlightedCode + text.Substring(match.Index + match.Length);
            }

            return text;
        }
    }
}
