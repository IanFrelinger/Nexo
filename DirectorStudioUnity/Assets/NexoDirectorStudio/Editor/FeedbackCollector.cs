using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Collects and manages feedback for iterative game design.
    /// </summary>
    public static class FeedbackCollector
    {
        private static readonly string FeedbackDirectory = "Assets/GeneratedScenes/Feedback";
        private static readonly string FeedbackFile = Path.Combine(FeedbackDirectory, "design_feedback.json");

        /// <summary>
        /// Records feedback for a specific game slice iteration.
        /// </summary>
        public static void RecordFeedback(string gamePlanId, string scenePath, string feedback, int rating = 3)
        {
            EnsureFeedbackDirectory();
            
            var feedbackEntry = new FeedbackEntry
            {
                Timestamp = DateTime.Now,
                GamePlanId = gamePlanId,
                ScenePath = scenePath,
                Feedback = feedback,
                Rating = Mathf.Clamp(rating, 1, 5),
                Iteration = GetNextIterationNumber(gamePlanId)
            };

            var allFeedback = LoadAllFeedback();
            allFeedback.Add(feedbackEntry);
            SaveAllFeedback(allFeedback);

            Debug.Log($"Feedback recorded for {gamePlanId}: {feedback} (Rating: {rating}/5)");
        }

        /// <summary>
        /// Gets the latest feedback for a game plan to inform the next iteration.
        /// </summary>
        public static List<FeedbackEntry> GetLatestFeedback(string gamePlanId, int count = 3)
        {
            var allFeedback = LoadAllFeedback();
            return allFeedback
                .Where(f => f.GamePlanId == gamePlanId)
                .OrderByDescending(f => f.Timestamp)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Generates a summary of feedback trends for design insights.
        /// </summary>
        public static string GenerateFeedbackSummary(string gamePlanId)
        {
            var feedback = GetLatestFeedback(gamePlanId, 10);
            if (!feedback.Any()) return "No feedback available yet.";

            var avgRating = feedback.Average(f => f.Rating);
            var commonThemes = feedback
                .SelectMany(f => f.Feedback.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .GroupBy(word => word)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key);

            return $"Average Rating: {avgRating:F1}/5\n" +
                   $"Common Themes: {string.Join(", ", commonThemes)}\n" +
                   $"Total Iterations: {feedback.Count}";
        }

        private static void EnsureFeedbackDirectory()
        {
            if (!Directory.Exists(FeedbackDirectory))
            {
                Directory.CreateDirectory(FeedbackDirectory);
            }
        }

        private static int GetNextIterationNumber(string gamePlanId)
        {
            var allFeedback = LoadAllFeedback();
            var gameFeedback = allFeedback.Where(f => f.GamePlanId == gamePlanId);
            return gameFeedback.Any() ? gameFeedback.Max(f => f.Iteration) + 1 : 1;
        }

        private static List<FeedbackEntry> LoadAllFeedback()
        {
            if (!File.Exists(FeedbackFile))
            {
                return new List<FeedbackEntry>();
            }

            try
            {
                var json = File.ReadAllText(FeedbackFile);
                return JsonUtility.FromJson<FeedbackCollection>(json).Entries ?? new List<FeedbackEntry>();
            }
            catch
            {
                return new List<FeedbackEntry>();
            }
        }

        private static void SaveAllFeedback(List<FeedbackEntry> feedback)
        {
            var collection = new FeedbackCollection { Entries = feedback };
            var json = JsonUtility.ToJson(collection, true);
            File.WriteAllText(FeedbackFile, json);
        }

        [System.Serializable]
        public class FeedbackEntry
        {
            public DateTime Timestamp;
            public string GamePlanId;
            public string ScenePath;
            public string Feedback;
            public int Rating;
            public int Iteration;
        }

        [System.Serializable]
        private class FeedbackCollection
        {
            public List<FeedbackEntry> Entries;
        }
    }
}
