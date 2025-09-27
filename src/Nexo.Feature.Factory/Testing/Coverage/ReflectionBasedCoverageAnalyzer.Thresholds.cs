using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Progress;

namespace Nexo.Feature.Factory.Testing.Coverage
{
    /// <summary>
    /// Thresholds functionality for reflection-based coverage analyzer.
    /// </summary>
    public sealed partial class ReflectionBasedCoverageAnalyzer
    {
        /// <summary>
        /// Gets coverage thresholds for different coverage types.
        /// </summary>
        public CoverageThresholds GetCoverageThresholds()
        {
            return _thresholds;
        }

        /// <summary>
        /// Sets coverage thresholds for different coverage types.
        /// </summary>
        public void SetCoverageThresholds(CoverageThresholds thresholds)
        {
            _thresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
        }

        private static TestCoverageInfo CreateEmptyCoverageInfo()
        {
            return new TestCoverageInfo(
                0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                new Dictionary<string, FileCoverageInfo>(),
                new Dictionary<string, ClassCoverageInfo>()
            );
        }
    }
}