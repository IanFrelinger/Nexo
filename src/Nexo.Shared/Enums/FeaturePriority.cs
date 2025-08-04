namespace Nexo.Core.Application.Enums
{
    /// <summary>
    /// Represents the priority levels assigned to features, indicating their importance and urgency.
    /// </summary>
    public enum FeaturePriority
    {
        /// <summary>
        /// Represents a low-priority level for features.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Represents a medium priority level for features.
        /// </summary>
        Medium = 1,

        /// <summary>
        /// Represents a high-priority level for features, indicating significant importance and urgency.
        /// </summary>
        High = 2,

        /// <summary>
        /// Represents the highest-priority level for features, indicating critical importance and urgency.
        /// </summary>
        Critical = 3
    }
}