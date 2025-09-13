using Nexo.Core.Domain.Common;
using Nexo.Core.Domain.Composition;

namespace Nexo.Core.Domain.Models.Extensions
{
    public class CompilationResult : BaseResult
    {
        public byte[]? AssemblyBytes { get; set; }
        public List<CompilationError> Errors { get; set; } = new();
        public new List<CompilationWarning> Warnings { get; set; } = new();
        public TimeSpan CompilationTime { get; set; }
        public string AssemblyName { get; set; } = string.Empty;
        public int ErrorCount => Errors.Count;
        public int WarningCount => Warnings.Count;
        public bool HasErrors => ErrorCount > 0;
        public bool HasWarnings => WarningCount > 0;

        public new bool IsSuccess => !HasErrors && base.IsSuccess;

        public void AddCompilationError(string message, string code = "", string errorCode = "")
        {
            AddValidationError($"{errorCode}: {message}");
            Errors.Add(new CompilationError
            {
                Id = errorCode,
                Message = message,
                FileName = "Generated",
                Severity = "Error"
            });
        }
    }

    public class CompilationError
    {
        public string Id { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int Line { get; set; }
        public int Column { get; set; }
        public string Severity { get; set; } = string.Empty;
    }

    public class CompilationWarning
    {
        public string Id { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int Line { get; set; }
        public int Column { get; set; }
        public string Severity { get; set; } = string.Empty;
    }
}
