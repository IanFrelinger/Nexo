using System;
using System.Collections.Generic;
using System.Linq;

namespace StandaloneTestRunner.Models
{
    public class TestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public List<TestInfo> Tests { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public TestOptions? Options { get; set; }

        public List<TestInfo> PassedTests => Tests.Where(t => t.Status == "Passed").ToList();
        public List<TestInfo> FailedTests => Tests.Where(t => t.Status == "Failed").ToList();
        public List<TestInfo> SkippedTests => Tests.Where(t => t.Status == "Skipped").ToList();
    }

    public class TestInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
