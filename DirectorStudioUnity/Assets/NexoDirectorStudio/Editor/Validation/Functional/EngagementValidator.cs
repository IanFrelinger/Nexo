#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using NexoDirectorStudio.Interactions;

namespace NexoDirectorStudio.Editor.Validation.Functional
{
    public sealed class EngagementValidator : IFunctionalValidator
    {
        public string Name => "Engagement";

        public void Run(FunctionalReport r)
        {
            var player = FindPlayer();
            var interactions = FindInteractions();
            var enemies = FindEnemies();

            // Core Engagement Metrics
            var coreEngagement = TestCoreEngagement(player, interactions, enemies);
            r.Add("Engagement.Core", coreEngagement.Passed, coreEngagement.Message, coreEngagement.Details);

            // Progression Analysis
            var progression = AnalyzeProgression(interactions);
            r.Add("Engagement.Progression", progression.HasProgression, progression.Message, progression.Details);

            // Challenge Assessment
            var challenge = AssessChallenge(enemies, interactions);
            r.Add("Engagement.Challenge", challenge.HasAppropriateChallenge, challenge.Message, challenge.Details);

            // Spatial Engagement
            var spatial = AnalyzeSpatialEngagement(player, interactions);
            r.Add("Engagement.Spatial", spatial.IsWellDistributed, spatial.Message, spatial.Details);

            // Interaction Flow
            var flow = AnalyzeInteractionFlow(interactions);
            r.Add("Engagement.Flow", flow.HasGoodFlow, flow.Message, flow.Details);

            // Overall Engagement Score
            var overallScore = CalculateOverallEngagementScore(coreEngagement, progression, challenge, spatial, flow);
            r.Add("Engagement.OverallScore", overallScore >= 70, 
                $"Overall engagement score: {overallScore}/100", 
                $"Core: {coreEngagement.Score}/20, Progression: {progression.Score}/20, Challenge: {challenge.Score}/20, Spatial: {spatial.Score}/20, Flow: {flow.Score}/20");
        }

        private GameObject FindPlayer()
        {
            return GameObject.FindGameObjectWithTag("Player") 
                   ?? UnityEngine.Object.FindObjectsOfType<GameObject>(true)
                       .FirstOrDefault(go => go.name.ToLower().Contains("player"));
        }

        private IInteraction[] FindInteractions()
        {
            return UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true)
                .Where(m => m is IInteraction)
                .Cast<IInteraction>()
                .ToArray();
        }

        private GameObject[] FindEnemies()
        {
            var enemies = new System.Collections.Generic.List<GameObject>();
            
            try
            {
                enemies.AddRange(GameObject.FindGameObjectsWithTag("Enemy"));
            }
            catch { /* Tag doesn't exist */ }
            
            enemies.AddRange(UnityEngine.Object.FindObjectsOfType<GameObject>(true)
                .Where(go => go.name.ToLower().Contains("enemy") || 
                           go.name.ToLower().Contains("hostile") ||
                           go.name.ToLower().Contains("opponent")));
            
            return enemies.Distinct().ToArray();
        }

        private (bool Passed, int Score, string Message, string Details) TestCoreEngagement(GameObject player, IInteraction[] interactions, GameObject[] enemies)
        {
            int score = 0;
            var details = new System.Collections.Generic.List<string>();

            // Player mobility (5 points)
            if (player != null)
            {
                score += 2;
                details.Add("Player exists (+2)");
                
                if (player.GetComponent<CharacterController>() != null || player.GetComponent<Rigidbody>() != null)
                {
                    score += 3;
                    details.Add("Player can move (+3)");
                }
            }

            // Interaction availability (8 points)
            if (interactions.Length >= 5)
            {
                score += 8;
                details.Add($"Rich interactions: {interactions.Length} (+8)");
            }
            else if (interactions.Length >= 3)
            {
                score += 6;
                details.Add($"Good interactions: {interactions.Length} (+6)");
            }
            else if (interactions.Length >= 1)
            {
                score += 3;
                details.Add($"Basic interactions: {interactions.Length} (+3)");
            }

            // Challenge presence (7 points)
            if (enemies.Length >= 3)
            {
                score += 7;
                details.Add($"Strong challenge: {enemies.Length} enemies (+7)");
            }
            else if (enemies.Length >= 1)
            {
                score += 4;
                details.Add($"Some challenge: {enemies.Length} enemies (+4)");
            }

            bool passed = score >= 12; // 60% of max 20 points
            return (passed, score, 
                passed ? "Core engagement elements present" : "Missing core engagement elements",
                string.Join("; ", details));
        }

        private (bool HasProgression, int Score, string Message, string Details) AnalyzeProgression(IInteraction[] interactions)
        {
            int score = 0;
            var details = new System.Collections.Generic.List<string>();

            // Check for goal-oriented interactions
            var goalInteractions = interactions.Where(i => 
            {
                var mb = i as MonoBehaviour;
                return mb != null && (mb.name.ToLower().Contains("goal") || 
                                    mb.name.ToLower().Contains("objective") || 
                                    mb.name.ToLower().Contains("target") ||
                                    mb.name.ToLower().Contains("finish"));
            }).ToArray();

            if (goalInteractions.Length >= 1)
            {
                score += 8;
                details.Add($"Clear objectives: {goalInteractions.Length} (+8)");
            }

            // Check for power-up/upgrade interactions
            var powerUpInteractions = interactions.Where(i => 
            {
                var mb = i as MonoBehaviour;
                return mb != null && (mb.name.ToLower().Contains("power") || 
                                    mb.name.ToLower().Contains("upgrade") || 
                                    mb.name.ToLower().Contains("collect") ||
                                    mb.name.ToLower().Contains("item"));
            }).ToArray();

            if (powerUpInteractions.Length >= 1)
            {
                score += 6;
                details.Add($"Progression items: {powerUpInteractions.Length} (+6)");
            }

            // Check for interaction variety (suggests different gameplay mechanics)
            var interactionTypes = interactions.Select(i => i.GetType().Name).Distinct().Count();
            if (interactionTypes >= 3)
            {
                score += 6;
                details.Add($"Rich variety: {interactionTypes} types (+6)");
            }
            else if (interactionTypes >= 2)
            {
                score += 3;
                details.Add($"Some variety: {interactionTypes} types (+3)");
            }

            bool hasProgression = score >= 10; // 50% of max 20 points
            return (hasProgression, score,
                hasProgression ? "Good progression elements" : "Limited progression elements",
                string.Join("; ", details));
        }

        private (bool HasAppropriateChallenge, int Score, string Message, string Details) AssessChallenge(GameObject[] enemies, IInteraction[] interactions)
        {
            int score = 0;
            var details = new System.Collections.Generic.List<string>();

            // Enemy presence and distribution
            if (enemies.Length >= 3)
            {
                score += 8;
                details.Add($"Strong opposition: {enemies.Length} enemies (+8)");
            }
            else if (enemies.Length >= 1)
            {
                score += 5;
                details.Add($"Some opposition: {enemies.Length} enemies (+5)");
            }

            // Interaction difficulty (based on spatial distribution)
            if (interactions.Length > 1)
            {
                var positions = interactions.Select(i => (i as MonoBehaviour)?.transform.position ?? Vector3.zero).ToArray();
                var avgDistance = CalculateAverageDistance(positions);
                
                if (avgDistance > 8f)
                {
                    score += 6;
                    details.Add($"Challenging spread: {avgDistance:F1} avg distance (+6)");
                }
                else if (avgDistance > 4f)
                {
                    score += 4;
                    details.Add($"Moderate spread: {avgDistance:F1} avg distance (+4)");
                }
                else
                {
                    score += 2;
                    details.Add($"Tight layout: {avgDistance:F1} avg distance (+2)");
                }
            }

            // Risk/reward balance (enemies vs power-ups)
            var powerUps = interactions.Count(i => 
            {
                var mb = i as MonoBehaviour;
                return mb != null && (mb.name.ToLower().Contains("power") || mb.name.ToLower().Contains("health"));
            });
            
            if (enemies.Length > 0 && powerUps > 0)
            {
                var ratio = (float)powerUps / enemies.Length;
                if (ratio >= 0.5f && ratio <= 2f)
                {
                    score += 6;
                    details.Add($"Balanced risk/reward: {powerUps} power-ups vs {enemies.Length} enemies (+6)");
                }
                else
                {
                    score += 3;
                    details.Add($"Imbalanced risk/reward: {powerUps} power-ups vs {enemies.Length} enemies (+3)");
                }
            }

            bool hasAppropriateChallenge = score >= 10; // 50% of max 20 points
            return (hasAppropriateChallenge, score,
                hasAppropriateChallenge ? "Appropriate challenge level" : "Challenge needs adjustment",
                string.Join("; ", details));
        }

        private (bool IsWellDistributed, int Score, string Message, string Details) AnalyzeSpatialEngagement(GameObject player, IInteraction[] interactions)
        {
            int score = 0;
            var details = new System.Collections.Generic.List<string>();

            if (player == null || interactions.Length == 0)
            {
                return (false, 0, "Cannot analyze spatial engagement without player or interactions", "Missing prerequisites");
            }

            var playerPos = player.transform.position;
            var positions = interactions.Select(i => (i as MonoBehaviour)?.transform.position ?? Vector3.zero).ToArray();

            // Reachability analysis
            var maxDistance = 15f; // Reasonable interaction range
            var reachableCount = positions.Count(pos => Vector3.Distance(playerPos, pos) <= maxDistance);
            var reachabilityRatio = (float)reachableCount / interactions.Length;

            if (reachabilityRatio >= 0.8f)
            {
                score += 8;
                details.Add($"Excellent reachability: {reachableCount}/{interactions.Length} reachable (+8)");
            }
            else if (reachabilityRatio >= 0.6f)
            {
                score += 6;
                details.Add($"Good reachability: {reachableCount}/{interactions.Length} reachable (+6)");
            }
            else if (reachabilityRatio >= 0.4f)
            {
                score += 4;
                details.Add($"Moderate reachability: {reachableCount}/{interactions.Length} reachable (+4)");
            }

            // Spatial distribution
            var avgDistance = CalculateAverageDistance(positions);
            if (avgDistance > 6f)
            {
                score += 6;
                details.Add($"Good distribution: {avgDistance:F1} avg distance (+6)");
            }
            else if (avgDistance > 3f)
            {
                score += 4;
                details.Add($"Moderate distribution: {avgDistance:F1} avg distance (+4)");
            }
            else
            {
                score += 2;
                details.Add($"Tight distribution: {avgDistance:F1} avg distance (+2)");
            }

            // Exploration encouragement
            var bounds = CalculateBounds(positions);
            var explorationArea = bounds.size.magnitude;
            if (explorationArea > 20f)
            {
                score += 6;
                details.Add($"Encourages exploration: {explorationArea:F1} area (+6)");
            }
            else if (explorationArea > 10f)
            {
                score += 4;
                details.Add($"Some exploration: {explorationArea:F1} area (+4)");
            }

            bool isWellDistributed = score >= 12; // 60% of max 20 points
            return (isWellDistributed, score,
                isWellDistributed ? "Good spatial engagement" : "Spatial engagement needs improvement",
                string.Join("; ", details));
        }

        private (bool HasGoodFlow, int Score, string Message, string Details) AnalyzeInteractionFlow(IInteraction[] interactions)
        {
            int score = 0;
            var details = new System.Collections.Generic.List<string>();

            // Interaction density
            if (interactions.Length >= 5)
            {
                score += 6;
                details.Add($"Rich content: {interactions.Length} interactions (+6)");
            }
            else if (interactions.Length >= 3)
            {
                score += 4;
                details.Add($"Adequate content: {interactions.Length} interactions (+4)");
            }
            else if (interactions.Length >= 1)
            {
                score += 2;
                details.Add($"Minimal content: {interactions.Length} interactions (+2)");
            }

            // Interaction variety
            var interactionTypes = interactions.Select(i => i.GetType().Name).Distinct().Count();
            if (interactionTypes >= 4)
            {
                score += 8;
                details.Add($"Excellent variety: {interactionTypes} types (+8)");
            }
            else if (interactionTypes >= 3)
            {
                score += 6;
                details.Add($"Good variety: {interactionTypes} types (+6)");
            }
            else if (interactionTypes >= 2)
            {
                score += 4;
                details.Add($"Some variety: {interactionTypes} types (+4)");
            }

            // Logical grouping (check for themed interactions)
            var themedGroups = interactions.GroupBy(i => 
            {
                var mb = i as MonoBehaviour;
                if (mb == null) return "Unknown";
                var name = mb.name.ToLower();
                if (name.Contains("weapon") || name.Contains("gun")) return "Combat";
                if (name.Contains("power") || name.Contains("upgrade")) return "Power";
                if (name.Contains("goal") || name.Contains("objective")) return "Objective";
                return "Other";
            }).Where(g => g.Count() > 1).Count();

            if (themedGroups >= 2)
            {
                score += 6;
                details.Add($"Good thematic grouping: {themedGroups} groups (+6)");
            }
            else if (themedGroups >= 1)
            {
                score += 3;
                details.Add($"Some thematic grouping: {themedGroups} groups (+3)");
            }

            bool hasGoodFlow = score >= 12; // 60% of max 20 points
            return (hasGoodFlow, score,
                hasGoodFlow ? "Good interaction flow" : "Interaction flow needs improvement",
                string.Join("; ", details));
        }

        private int CalculateOverallEngagementScore(
            (bool Passed, int Score, string Message, string Details) core,
            (bool HasProgression, int Score, string Message, string Details) progression,
            (bool HasAppropriateChallenge, int Score, string Message, string Details) challenge,
            (bool IsWellDistributed, int Score, string Message, string Details) spatial,
            (bool HasGoodFlow, int Score, string Message, string Details) flow)
        {
            return core.Score + progression.Score + challenge.Score + spatial.Score + flow.Score;
        }

        private float CalculateAverageDistance(Vector3[] positions)
        {
            if (positions.Length < 2) return 0f;
            
            float totalDistance = 0f;
            int pairs = 0;
            
            for (int i = 0; i < positions.Length; i++)
            {
                for (int j = i + 1; j < positions.Length; j++)
                {
                    totalDistance += Vector3.Distance(positions[i], positions[j]);
                    pairs++;
                }
            }
            
            return pairs > 0 ? totalDistance / pairs : 0f;
        }

        private Bounds CalculateBounds(Vector3[] positions)
        {
            if (positions.Length == 0) return new Bounds();
            
            var min = positions[0];
            var max = positions[0];
            
            foreach (var pos in positions)
            {
                min = Vector3.Min(min, pos);
                max = Vector3.Max(max, pos);
            }
            
            return new Bounds((min + max) * 0.5f, max - min);
        }
    }
}
#endif
