using System;

namespace Nexo.Shared.Composable.CommandTypes
{
    /// <summary>
    /// Interface for command identifiers
    /// </summary>
    public interface ICommandIdentifier
    {
        string Value { get; }
        string Namespace { get; }
        string Version { get; }
    }
    
    /// <summary>
    /// Default implementation of command identifier
    /// </summary>
    public class CommandIdentifier : ICommandIdentifier
    {
        public string Value { get; }
        public string Namespace { get; }
        public string Version { get; }
        
        public CommandIdentifier(string value, string @namespace, string version)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }
        
        public override string ToString() => $"{Namespace}.{Value}@{Version}";
        public override bool Equals(object? obj) => obj is ICommandIdentifier other && Value == other.Value && Namespace == other.Namespace && Version == other.Version;
        public override int GetHashCode() => HashCode.Combine(Value, Namespace, Version);
    }
}