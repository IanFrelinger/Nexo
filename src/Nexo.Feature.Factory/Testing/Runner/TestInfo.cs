using System.Reflection;
using Nexo.Feature.Factory.Testing.Models;

namespace Nexo.Feature.Factory.Testing.Runner
{
    /// <summary>
    /// Represents information about a test method.
    /// </summary>
    public sealed class TestInfo
    {
        /// <summary>
        /// Gets the unique identifier for the test.
        /// </summary>
        public string TestId { get; }

        /// <summary>
        /// Gets the display name of the test.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the test method information.
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// Gets the test class information.
        /// </summary>
        public Type TestClass { get; }

        /// <summary>
        /// Gets the test category.
        /// </summary>
        public TestCategory Category { get; }

        /// <summary>
        /// Gets the test priority.
        /// </summary>
        public TestPriority Priority { get; }

        /// <summary>
        /// Gets the estimated duration of the test.
        /// </summary>
        public TimeSpan EstimatedDuration { get; }

        /// <summary>
        /// Gets the timeout for the test.
        /// </summary>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// Gets the dependencies of the test.
        /// </summary>
        public IReadOnlyList<string> Dependencies { get; }

        /// <summary>
        /// Gets the tags associated with the test.
        /// </summary>
        public IReadOnlyList<string> Tags { get; }

        /// <summary>
        /// Gets whether the test is enabled.
        /// </summary>
        public bool IsEnabled { get; }

        /// <summary>
        /// Gets the description of the test.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Initializes a new instance of the TestInfo class.
        /// </summary>
        public TestInfo(
            string testId,
            string displayName,
            MethodInfo method,
            Type testClass,
            TestCategory category,
            TestPriority priority,
            TimeSpan estimatedDuration,
            TimeSpan timeout,
            IReadOnlyList<string> dependencies,
            IReadOnlyList<string> tags,
            bool isEnabled,
            string description)
        {
            TestId = testId ?? throw new ArgumentNullException(nameof(testId));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Method = method ?? throw new ArgumentNullException(nameof(method));
            TestClass = testClass ?? throw new ArgumentNullException(nameof(testClass));
            Category = category;
            Priority = priority;
            EstimatedDuration = estimatedDuration;
            Timeout = timeout;
            Dependencies = dependencies ?? new List<string>();
            Tags = tags ?? new List<string>();
            IsEnabled = isEnabled;
            Description = description ?? string.Empty;
        }
    }
}
