using UnityEditor;
using UnityEngine;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Unity menu items for feedback collection during iterative design.
    /// </summary>
    public static class FeedbackMenu
    {
        [MenuItem("NexoDirectorStudio/Record Feedback/Positive (5/5)")]
        public static void RecordPositiveFeedback()
        {
            var gamePlanId = EditorPrefs.GetString("CurrentGamePlanId", "");
            var scenePath = EditorPrefs.GetString("CurrentScenePath", "");
            
            if (string.IsNullOrEmpty(gamePlanId))
            {
                EditorUtility.DisplayDialog("No Active Game", "No active game plan found. Run the CLI pipeline first.", "OK");
                return;
            }

            FeedbackCollector.RecordFeedback(gamePlanId, scenePath, "Positive playtest experience", 5);
            EditorUtility.DisplayDialog("Feedback Recorded", "Positive feedback (5/5) recorded successfully!", "OK");
        }

        [MenuItem("NexoDirectorStudio/Record Feedback/Good (4/5)")]
        public static void RecordGoodFeedback()
        {
            var gamePlanId = EditorPrefs.GetString("CurrentGamePlanId", "");
            var scenePath = EditorPrefs.GetString("CurrentScenePath", "");
            
            if (string.IsNullOrEmpty(gamePlanId))
            {
                EditorUtility.DisplayDialog("No Active Game", "No active game plan found. Run the CLI pipeline first.", "OK");
                return;
            }

            FeedbackCollector.RecordFeedback(gamePlanId, scenePath, "Good playtest experience with minor issues", 4);
            EditorUtility.DisplayDialog("Feedback Recorded", "Good feedback (4/5) recorded successfully!", "OK");
        }

        [MenuItem("NexoDirectorStudio/Record Feedback/Neutral (3/5)")]
        public static void RecordNeutralFeedback()
        {
            var gamePlanId = EditorPrefs.GetString("CurrentGamePlanId", "");
            var scenePath = EditorPrefs.GetString("CurrentScenePath", "");
            
            if (string.IsNullOrEmpty(gamePlanId))
            {
                EditorUtility.DisplayDialog("No Active Game", "No active game plan found. Run the CLI pipeline first.", "OK");
                return;
            }

            FeedbackCollector.RecordFeedback(gamePlanId, scenePath, "Neutral playtest experience", 3);
            EditorUtility.DisplayDialog("Feedback Recorded", "Neutral feedback (3/5) recorded successfully!", "OK");
        }

        [MenuItem("NexoDirectorStudio/Record Feedback/Poor (2/5)")]
        public static void RecordPoorFeedback()
        {
            var gamePlanId = EditorPrefs.GetString("CurrentGamePlanId", "");
            var scenePath = EditorPrefs.GetString("CurrentScenePath", "");
            
            if (string.IsNullOrEmpty(gamePlanId))
            {
                EditorUtility.DisplayDialog("No Active Game", "No active game plan found. Run the CLI pipeline first.", "OK");
                return;
            }

            FeedbackCollector.RecordFeedback(gamePlanId, scenePath, "Poor playtest experience with significant issues", 2);
            EditorUtility.DisplayDialog("Feedback Recorded", "Poor feedback (2/5) recorded successfully!", "OK");
        }

        [MenuItem("NexoDirectorStudio/Record Feedback/Terrible (1/5)")]
        public static void RecordTerribleFeedback()
        {
            var gamePlanId = EditorPrefs.GetString("CurrentGamePlanId", "");
            var scenePath = EditorPrefs.GetString("CurrentScenePath", "");
            
            if (string.IsNullOrEmpty(gamePlanId))
            {
                EditorUtility.DisplayDialog("No Active Game", "No active game plan found. Run the CLI pipeline first.", "OK");
                return;
            }

            FeedbackCollector.RecordFeedback(gamePlanId, scenePath, "Terrible playtest experience - major redesign needed", 1);
            EditorUtility.DisplayDialog("Feedback Recorded", "Terrible feedback (1/5) recorded successfully!", "OK");
        }

        [MenuItem("NexoDirectorStudio/Show Feedback Summary")]
        public static void ShowFeedbackSummary()
        {
            var gamePlanId = EditorPrefs.GetString("CurrentGamePlanId", "");
            
            if (string.IsNullOrEmpty(gamePlanId))
            {
                EditorUtility.DisplayDialog("No Active Game", "No active game plan found. Run the CLI pipeline first.", "OK");
                return;
            }

            var summary = FeedbackCollector.GenerateFeedbackSummary(gamePlanId);
            EditorUtility.DisplayDialog("Feedback Summary", summary, "OK");
        }

        [MenuItem("NexoDirectorStudio/Record Custom Feedback")]
        public static void RecordCustomFeedback()
        {
            var gamePlanId = EditorPrefs.GetString("CurrentGamePlanId", "");
            var scenePath = EditorPrefs.GetString("CurrentScenePath", "");
            
            if (string.IsNullOrEmpty(gamePlanId))
            {
                EditorUtility.DisplayDialog("No Active Game", "No active game plan found. Run the CLI pipeline first.", "OK");
                return;
            }

            // Open a custom feedback window
            FeedbackWindow.ShowWindow(gamePlanId, scenePath);
        }
    }

    /// <summary>
    /// Custom feedback window for detailed feedback entry.
    /// </summary>
    public class FeedbackWindow : EditorWindow
    {
        private string gamePlanId;
        private string scenePath;
        private string feedbackText = "";
        private int rating = 3;

        public static void ShowWindow(string gamePlanId, string scenePath)
        {
            var window = GetWindow<FeedbackWindow>("Record Custom Feedback");
            window.gamePlanId = gamePlanId;
            window.scenePath = scenePath;
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Custom Feedback Entry", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label($"Game Plan: {gamePlanId}");
            GUILayout.Label($"Scene: {scenePath}");
            GUILayout.Space(10);

            GUILayout.Label("Rating (1-5):");
            rating = EditorGUILayout.IntSlider(rating, 1, 5);
            GUILayout.Space(10);

            GUILayout.Label("Feedback Details:");
            feedbackText = EditorGUILayout.TextArea(feedbackText, GUILayout.Height(100));
            GUILayout.Space(10);

            if (GUILayout.Button("Record Feedback"))
            {
                if (string.IsNullOrWhiteSpace(feedbackText))
                {
                    EditorUtility.DisplayDialog("Empty Feedback", "Please enter some feedback text.", "OK");
                    return;
                }

                FeedbackCollector.RecordFeedback(gamePlanId, scenePath, feedbackText, rating);
                EditorUtility.DisplayDialog("Feedback Recorded", $"Feedback (Rating: {rating}/5) recorded successfully!", "OK");
                Close();
            }

            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
        }
    }
}
