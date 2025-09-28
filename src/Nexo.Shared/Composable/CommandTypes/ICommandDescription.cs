using System;

namespace Nexo.Shared.Composable.CommandTypes
{
    /// <summary>
    /// Interface for command descriptions
    /// </summary>
    public interface ICommandDescription
    {
        string Summary { get; }
        string DetailedDescription { get; }
        string[] Tags { get; }
        string[] Examples { get; }
    }
    
    /// <summary>
    /// Default implementation of command description
    /// </summary>
    public class CommandDescription : ICommandDescription
    {
        public string Summary { get; }
        public string DetailedDescription { get; }
        public string[] Tags { get; }
        public string[] Examples { get; }
        
        public CommandDescription(
            string summary, 
            string? detailedDescription = null, 
            string[]? tags = null, 
            string[]? examples = null)
        {
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            DetailedDescription = detailedDescription ?? summary;
            Tags = tags ?? Array.Empty<string>();
            Examples = examples ?? Array.Empty<string>();
        }
        
        public override string ToString() => Summary;
    }
}
