namespace Nexo.Shared.Models
{
    /// <summary>
    /// Represents a command execution request.
    /// </summary>
    public class CommandRequest
    {
        public string Command { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public int TimeoutSeconds { get; set; }
        public bool CaptureOutput { get; set; }
    }
}