using System;

namespace Nexo.Shared.Composable.CommandTypes
{
    /// <summary>
    /// Default implementation of validation warning
    /// </summary>
    public class ValidationWarning : IValidationWarning
    {
        public string Code { get; }
        public string Message { get; }
        public string Property { get; }
        
        public ValidationWarning(string code, string message, string? property = null)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Property = property ?? string.Empty;
        }
        
        public override string ToString() => $"{Code}: {Message}";
        public override bool Equals(object? obj) => obj is IValidationWarning other && Code == other.Code;
        public override int GetHashCode() => Code.GetHashCode();
    }
}
