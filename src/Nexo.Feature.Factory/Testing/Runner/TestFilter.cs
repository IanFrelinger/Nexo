using Nexo.Feature.Factory.Testing.Models;

namespace Nexo.Feature.Factory.Testing.Runner
{
    /// <summary>
    /// Represents filter criteria for test execution.
    /// </summary>
    public sealed class TestFilter
    {
        /// <summary>
        /// Gets or sets the categories to include.
        /// </summary>
        public IReadOnlyList<TestCategory> IncludeCategories { get; set; } = new List<TestCategory>();

        /// <summary>
        /// Gets or sets the categories to exclude.
        /// </summary>
        public IReadOnlyList<TestCategory> ExcludeCategories { get; set; } = new List<TestCategory>();

        /// <summary>
        /// Gets or sets the priorities to include.
        /// </summary>
        public IReadOnlyList<TestPriority> IncludePriorities { get; set; } = new List<TestPriority>();

        /// <summary>
        /// Gets or sets the priorities to exclude.
        /// </summary>
        public IReadOnlyList<TestPriority> ExcludePriorities { get; set; } = new List<TestPriority>();

        /// <summary>
        /// Gets or sets the tags to include.
        /// </summary>
        public IReadOnlyList<string> IncludeTags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the tags to exclude.
        /// </summary>
        public IReadOnlyList<string> ExcludeTags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the test IDs to include.
        /// </summary>
        public IReadOnlyList<string> IncludeTestIds { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the test IDs to exclude.
        /// </summary>
        public IReadOnlyList<string> ExcludeTestIds { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether to include only enabled tests.
        /// </summary>
        public bool IncludeOnlyEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum duration for individual tests.
        /// </summary>
        public TimeSpan? MaxTestDuration { get; set; }

        /// <summary>
        /// Gets or sets the maximum total execution time.
        /// </summary>
        public TimeSpan? MaxTotalDuration { get; set; }

        /// <summary>
        /// Determines if a test matches the filter criteria.
        /// </summary>
        /// <param name="testInfo">The test information</param>
        /// <returns>True if the test matches the filter</returns>
        public bool Matches(TestInfo testInfo)
        {
            if (testInfo == null)
                return false;

            // Check if test is enabled
            if (IncludeOnlyEnabled && !testInfo.IsEnabled)
                return false;

            // Check categories
            if (IncludeCategories.Any() && !IncludeCategories.Contains(testInfo.Category))
                return false;

            if (ExcludeCategories.Any() && ExcludeCategories.Contains(testInfo.Category))
                return false;

            // Check priorities
            if (IncludePriorities.Any() && !IncludePriorities.Contains(testInfo.Priority))
                return false;

            if (ExcludePriorities.Any() && ExcludePriorities.Contains(testInfo.Priority))
                return false;

            // Check tags
            if (IncludeTags.Any() && !IncludeTags.Any(tag => testInfo.Tags.Contains(tag)))
                return false;

            if (ExcludeTags.Any() && ExcludeTags.Any(tag => testInfo.Tags.Contains(tag)))
                return false;

            // Check test IDs
            if (IncludeTestIds.Any() && !IncludeTestIds.Contains(testInfo.TestId))
                return false;

            if (ExcludeTestIds.Any() && ExcludeTestIds.Contains(testInfo.TestId))
                return false;

            // Check duration
            if (MaxTestDuration.HasValue && testInfo.EstimatedDuration > MaxTestDuration.Value)
                return false;

            return true;
        }
    }
}
