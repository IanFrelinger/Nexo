namespace Nexo.Shared.Models
{
    /// <summary>
    /// Represents a command execution request.
    /// </summary>
    public class CommandRequest
    {
        public string Command { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; }
        public bool CaptureOutput { get; set; }
    }
}