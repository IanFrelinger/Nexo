using System;
using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Support classes and enums
    /// </summary>
    public partial class TestExecutionResult
    {
        /// <summary>
        /// Test file path.
        /// </summary>
        public string TestFile { get; set; } = string.Empty;

        /// <summary>
        /// Whether the test passed.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Test execution time.
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Error message if test failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Test output.
        /// </summary>
        public string Output { get; set; } = string.Empty;

        /// <summary>
        /// Exit code.
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Whether the test was executed in parallel.
        /// </summary>
        public bool WasExecutedInParallel { get; set; }

        /// <summary>
        /// Phase in which the test was executed.
        /// </summary>
        public string ExecutionPhase { get; set; } = string.Empty;
    }

    /// <summary>
    /// Test dependency rule.
    /// </summary>
    public partial class TestDependencyRule
    {
        /// <summary>
        /// Test that depends on another.
        /// </summary>
        public string DependentTest { get; set; } = string.Empty;

        /// <summary>
        /// Test that the dependent test depends on.
        /// </summary>
        public string DependencyTest { get; set; } = string.Empty;

        /// <summary>
        /// Type of dependency.
        /// </summary>
        public DependencyType DependencyType { get; set; }

        /// <summary>
        /// Whether the dependency is required.
        /// </summary>
        public bool IsRequired { get; set; } = true;
    }

    /// <summary>
    /// Type of test dependency.
    /// </summary>
    public enum DependencyType
    {
        /// <summary>
        /// Execution dependency - must execute before.
        /// </summary>
        Execution,

        /// <summary>
        /// Data dependency - depends on data produced.
        /// </summary>
        Data,

        /// <summary>
        /// Resource dependency - depends on shared resources.
        /// </summary>
        Resource,

        /// <summary>
        /// Conditional dependency - depends under certain conditions.
        /// </summary>
        Conditional
    }

    /// <summary>
    /// Validation result for test orchestration options.
    /// </summary>
    public partial class TestOrchestrationValidation
    {
        /// <summary>
        /// Whether the options are valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation errors.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Validation warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
