namespace Nexo.Core.Application.Models
{
    /// <summary>
    /// Represents a test failure with details such as test name, error message, stack trace, and optional source file and line number information.
    /// </summary>
    public sealed class TestFailure
    {
        public string TestName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public string? SourceFile { get; set; }
        public int LineNumber { get; set; }
        public TestFailure(string testName, string message, string stackTrace, string? sourceFile = null, int lineNumber = 0)
        {
            TestName = testName;
            Message = message;
            StackTrace = stackTrace;
            SourceFile = sourceFile;
            LineNumber = lineNumber;
        }
        public TestFailure() { }
    }
}