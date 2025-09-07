using System;
using Nexo.Feature.Factory.Testing.Models;

namespace Nexo.Feature.Factory.Testing.Attributes
{
    /// <summary>
    /// Base attribute for test methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public abstract class TestAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the display name of the test.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the test.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the estimated duration of the test.
        /// </summary>
        public int EstimatedDurationSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the timeout for the test in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Gets or sets the priority of the test.
        /// </summary>
        public TestPriority Priority { get; set; } = TestPriority.Medium;

        /// <summary>
        /// Gets or sets whether the test is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the tags associated with the test.
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the dependencies of the test.
        /// </summary>
        public string[] Dependencies { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Attribute for AI connectivity tests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AiConnectivityTestAttribute : TestAttribute
    {
        public AiConnectivityTestAttribute()
        {
            Priority = TestPriority.Critical;
            EstimatedDurationSeconds = 30;
            TimeoutSeconds = 60;
            Tags = new[] { "ai", "connectivity", "critical" };
        }
    }

    /// <summary>
    /// Attribute for domain analysis tests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DomainAnalysisTestAttribute : TestAttribute
    {
        public DomainAnalysisTestAttribute()
        {
            Priority = TestPriority.High;
            EstimatedDurationSeconds = 120;
            TimeoutSeconds = 300;
            Tags = new[] { "domain", "analysis", "ai" };
        }
    }

    /// <summary>
    /// Attribute for code generation tests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CodeGenerationTestAttribute : TestAttribute
    {
        public CodeGenerationTestAttribute()
        {
            Priority = TestPriority.High;
            EstimatedDurationSeconds = 180;
            TimeoutSeconds = 600;
            Tags = new[] { "code", "generation", "ai" };
        }
    }

    /// <summary>
    /// Attribute for end-to-end tests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class EndToEndTestAttribute : TestAttribute
    {
        public EndToEndTestAttribute()
        {
            Priority = TestPriority.Critical;
            EstimatedDurationSeconds = 300;
            TimeoutSeconds = 900;
            Tags = new[] { "e2e", "integration", "critical" };
        }
    }

    /// <summary>
    /// Attribute for performance tests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PerformanceTestAttribute : TestAttribute
    {
        public PerformanceTestAttribute()
        {
            Priority = TestPriority.Medium;
            EstimatedDurationSeconds = 120;
            TimeoutSeconds = 300;
            Tags = new[] { "performance", "metrics" };
        }
    }

    /// <summary>
    /// Attribute for validation tests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ValidationTestAttribute : TestAttribute
    {
        public ValidationTestAttribute()
        {
            Priority = TestPriority.Medium;
            EstimatedDurationSeconds = 10;
            TimeoutSeconds = 30;
            Tags = new[] { "validation", "quick" };
        }
    }

    /// <summary>
    /// Attribute for test classes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TestClassAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the display name of the test class.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the test class.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the category of the test class.
        /// </summary>
        public TestCategory Category { get; set; } = TestCategory.Functional;

        /// <summary>
        /// Gets or sets the tags associated with the test class.
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets whether the test class is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Attribute for test setup methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TestSetupAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the timeout for the setup method in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Attribute for test cleanup methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TestCleanupAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the timeout for the cleanup method in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }
}
