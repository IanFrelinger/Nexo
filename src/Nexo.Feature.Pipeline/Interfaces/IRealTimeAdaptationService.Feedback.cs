using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// User feedback and related models
    /// </summary>
    public partial interface IRealTimeAdaptationService
    {
        // Feedback models are defined in separate files
    }

    /// <summary>
    /// User feedback for system improvement.
    /// </summary>
    public class UserFeedback
    {
        /// <summary>
        /// Gets or sets the feedback identifier.
        /// </summary>
        public string FeedbackId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the feedback type.
        /// </summary>
        public FeedbackType Type { get; set; }

        /// <summary>
        /// Gets or sets the feedback rating (1-5).
        /// </summary>
        public int Rating { get; set; }

        /// <summary>
        /// Gets or sets the feedback message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the context of the feedback.
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the timestamp when this feedback was provided.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the execution ID this feedback relates to.
        /// </summary>
        public string? ExecutionId { get; set; }
    }

    /// <summary>
    /// Types of user feedback.
    /// </summary>
    public enum FeedbackType
    {
        /// <summary>
        /// Performance feedback.
        /// </summary>
        Performance,

        /// <summary>
        /// Usability feedback.
        /// </summary>
        Usability,

        /// <summary>
        /// Feature request feedback.
        /// </summary>
        FeatureRequest,

        /// <summary>
        /// Bug report feedback.
        /// </summary>
        BugReport,

        /// <summary>
        /// General feedback.
        /// </summary>
        General
    }
}
