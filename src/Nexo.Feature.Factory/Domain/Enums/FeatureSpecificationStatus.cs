namespace Nexo.Feature.Factory.Domain.Enums
{
    /// <summary>
    /// Represents the status of a feature specification.
    /// </summary>
    public enum FeatureSpecificationStatus
    {
        /// <summary>
        /// Specification is in draft state
        /// </summary>
        Draft,

        /// <summary>
        /// Specification is being analyzed
        /// </summary>
        Analyzing,

        /// <summary>
        /// Specification is being generated
        /// </summary>
        Generating,

        /// <summary>
        /// Specification is complete
        /// </summary>
        Complete,

        /// <summary>
        /// Specification has errors
        /// </summary>
        Error,

        /// <summary>
        /// Specification is invalid
        /// </summary>
        Invalid
    }
}
