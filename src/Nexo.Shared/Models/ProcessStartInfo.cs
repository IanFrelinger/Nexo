namespace Nexo.Shared.Models
{
    public class ProcessStartInfo {
        public string FileName { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public ProcessStartInfo() {}
        public ProcessStartInfo(string fileName, string arguments) {
            FileName = fileName;
            Arguments = arguments;
        }
    }
} 