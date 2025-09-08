namespace Nexo.Core.Application.Models
{
    /// <summary>
    /// Represents a build warning encountered during the build process.
    /// </summary>
    public sealed class BuildWarning
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string File { get; set; } = string.Empty;
        public int Line { get; set; }
        public int Column { get; set; }
        public BuildWarning(string code, string message, string file, int line, int column)
        {
            Code = code;
            Message = message;
            File = file;
            Line = line;
            Column = column;
        }
        public BuildWarning() { }
    }
}