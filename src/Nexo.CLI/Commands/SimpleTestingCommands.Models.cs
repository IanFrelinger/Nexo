using System;
using System.Collections.Generic;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Models for simple testing commands.
    /// </summary>
    public static partial class SimpleTestingCommands
    {
        // Simple test models
        public sealed record SimpleTestInfo(
            string TestId,
            string DisplayName,
            string Description,
            string Category,
            string Priority,
            TimeSpan EstimatedDuration,
            TimeSpan Timeout,
            IReadOnlyList<string> Tags
        );

        public sealed record SimpleTestResult(
            string TestId,
            bool IsSuccess,
            TimeSpan Duration,
            string? ErrorMessage
        );

        public sealed record SimpleTestSummary(
            int TotalTests,
            int PassedTests,
            int FailedTests,
            TimeSpan TotalDuration,
            TimeSpan TotalExecutionTime,
            double AverageDuration,
            IReadOnlyList<string> ErrorMessages
        );
    }
}
