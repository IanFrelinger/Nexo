namespace Nexo.Feature.Analysis.Enums
{
    /// <summary>
    /// Status of an individual test.
    /// </summary>
    public enum TestStatus
    {
        /// <summary>
        /// Test passed.
        /// </summary>
        Passed,

        /// <summary>
        /// Test failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Test was skipped.
        /// </summary>
        Skipped,

        /// <summary>
        /// Test result was inconclusive.
        /// </summary>
        Inconclusive
    }
} 