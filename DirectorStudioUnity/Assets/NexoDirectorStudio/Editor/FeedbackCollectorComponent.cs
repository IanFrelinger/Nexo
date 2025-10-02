using UnityEngine;
using NexoDirectorStudio.Agents;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// MonoBehaviour component for feedback collection in generated scenes.
    /// </summary>
    public class FeedbackCollectorComponent : MonoBehaviour
    {
        public string GamePlanId;
        public string ScenePath;

        [ContextMenu("Record Positive Feedback")]
        public void RecordPositiveFeedback()
        {
            FeedbackCollector.RecordFeedback(GamePlanId, ScenePath, "Positive playtest experience", 5);
        }

        [ContextMenu("Record Negative Feedback")]
        public void RecordNegativeFeedback()
        {
            FeedbackCollector.RecordFeedback(GamePlanId, ScenePath, "Issues identified during playtest", 2);
        }

        [ContextMenu("Show Feedback Summary")]
        public void ShowFeedbackSummary()
        {
            var summary = FeedbackCollector.GenerateFeedbackSummary(GamePlanId);
            Debug.Log($"Feedback Summary for {GamePlanId}:\n{summary}");
        }
    }
}
