namespace Nexo.Shared.Models
{
    public class ProcessStartInfo {
        public string FileName { get; set; }
        public string Arguments { get; set; }
        public ProcessStartInfo() {}
        public ProcessStartInfo(string fileName, string arguments) {
            FileName = fileName;
            Arguments = arguments;
        }
    }
} 