using System;

namespace Nexo.Shared.Composable.CommandTypes
{
    /// <summary>
    /// Default implementation of validation error
    /// </summary>
    public class ValidationError : IValidationError
    {
        public string Code { get; }
        public string Message { get; }
        public string Property { get; }
        public string Severity { get; }
        
        public ValidationError(string code, string message, string? property = null, string severity = "Error")
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Property = property ?? string.Empty;
            Severity = severity ?? throw new ArgumentNullException(nameof(severity));
        }
        
        public override string ToString() => $"{Code}: {Message}";
        public override bool Equals(object? obj) => obj is IValidationError other && Code == other.Code;
        public override int GetHashCode() => Code.GetHashCode();
    }
}
