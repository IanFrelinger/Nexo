namespace Nexo.Core.Application.Models
{
    /// <summary>
    /// Represents a build error encountered during the build process.
    /// </summary>
    public sealed class BuildError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string File { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public BuildError(string code, string message, string file, int line, int column)
        {
            Code = code;
            Message = message;
            File = file;
            Line = line;
            Column = column;
        }
        public BuildError() { }
    }
}