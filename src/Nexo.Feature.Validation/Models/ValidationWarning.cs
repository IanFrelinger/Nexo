namespace Nexo.Core.Application.Models
{
    /// <summary>
    /// Represents a validation warning, typically providing details about
    /// non-critical issues encountered during validation.
    /// </summary>
    public sealed class ValidationWarning
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public ValidationWarning(string code, string message, int line, int column)
        {
            Code = code;
            Message = message;
            Line = line;
            Column = column;
        }
    }
}