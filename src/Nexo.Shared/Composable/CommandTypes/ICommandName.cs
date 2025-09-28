using System;

namespace Nexo.Shared.Composable.CommandTypes
{
    /// <summary>
    /// Interface for command names
    /// </summary>
    public interface ICommandName
    {
        string Value { get; }
        string DisplayName { get; }
        string Category { get; }
    }
    
    /// <summary>
    /// Default implementation of command name
    /// </summary>
    public class CommandName : ICommandName
    {
        public string Value { get; }
        public string DisplayName { get; }
        public string Category { get; }
        
        public CommandName(string value, string? displayName = null, string category = "General")
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            DisplayName = displayName ?? value;
            Category = category ?? throw new ArgumentNullException(nameof(category));
        }
        
        public override string ToString() => DisplayName;
        public override bool Equals(object? obj) => obj is ICommandName other && Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();
    }
}
