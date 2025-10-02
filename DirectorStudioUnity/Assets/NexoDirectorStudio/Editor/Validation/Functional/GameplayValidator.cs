#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using NexoDirectorStudio.Interactions;

namespace NexoDirectorStudio.Editor.Validation.Functional
{
    public sealed class GameplayValidator : IFunctionalValidator
    {
        public string Name => "Gameplay";

        public void Run(FunctionalReport r)
        {
            // Player presence & basic locomotion substrate
            var player = GameObject.FindGameObjectWithTag("Player") 
                         ?? UnityEngine.Object.FindObjectsOfType<GameObject>(true).FirstOrDefault(go => go.name.ToLower().Contains("player"));
            bool hasCharCtrl = player && (player.GetComponent<CharacterController>() || player.GetComponent<Rigidbody>()) != null;
            bool hasCol = player && player.GetComponent<Collider>() != null;
            r.Add("Gameplay.PlayerPresent", player != null, player ? "Player found" : "No Player in scene (tag 'Player' or name contains 'player')");
            r.Add("Gameplay.PlayerLocomotion", hasCharCtrl, hasCharCtrl ? "CharacterController/Rigidbody present" : "No CharacterController/Rigidbody on Player");
            r.Add("Gameplay.PlayerCollider", hasCol, hasCol ? "Collider present on Player" : "No Collider on Player");

            // Interactions wired
            var interactions = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true).Where(m => m is IInteraction).Cast<IInteraction>().ToArray();
            var armedCount = interactions.Count(i => i.IsArmed);
            r.Add("Gameplay.InteractionsExist", interactions.Length > 0, interactions.Length > 0 ? $"Found {interactions.Length} interactions" : "No interactions found (IInteraction)");
            r.Add("Gameplay.InteractionsArmed", armedCount == interactions.Length && interactions.Length > 0,
                $"{armedCount}/{interactions.Length} interactions armed",
                "Ensure InteractionBus.Register(...) is called for each interactable and colliders/layers permit activation.");

            // Gameplay Quality Checks
            var gameplayQuality = AssessGameplayQuality(player, interactions);
            r.Add("Gameplay.QualityScore", gameplayQuality.Score >= 60, 
                $"Gameplay quality score: {gameplayQuality.Score}/100", 
                gameplayQuality.Details);

            // Engagement Testing
            var engagement = TestEngagement(player, interactions);
            r.Add("Gameplay.EngagementTest", engagement.IsEngaging, 
                engagement.IsEngaging ? "Gameplay is engaging" : "Gameplay lacks engagement",
                engagement.Feedback);

            // NavMesh availability (for AI/autoplayer nav)
            bool hasNav = false;
#if UNITY_AI_NAVIGATION
            var tri = NavMesh.CalculateTriangulation();
            hasNav = tri.vertices != null && tri.vertices.Length > 0 && tri.indices != null && tri.indices.Length > 0;
#else
            // Without AI package, best-effort: check for any NavMeshSurface component
            hasNav = UnityEngine.Object.FindObjectsOfType<Component>(true).Any(c => c.GetType().Name == "NavMeshSurface");
#endif
            r.Add("Gameplay.NavMeshReady", hasNav, hasNav ? "NavMesh present" : "No NavMesh detected (bake or build at runtime)");

            // Enemies or targets exist
            GameObject enemy = null;
            try
            {
                enemy = GameObject.FindGameObjectsWithTag("Enemy").FirstOrDefault();
            }
            catch
            {
                // Tag doesn't exist, try by name
                enemy = UnityEngine.Object.FindObjectsOfType<GameObject>(true).FirstOrDefault(go => go.name.ToLower().Contains("enemy"));
            }
            r.Add("Gameplay.OppositionPresent", enemy != null, enemy ? "Enemy/target present" : "No enemy/target object found");

            // Gameplay Flow Validation
            var flow = ValidateGameplayFlow(player, interactions, enemy);
            r.Add("Gameplay.FlowValid", flow.IsValid, 
                flow.IsValid ? "Gameplay flow is coherent" : "Gameplay flow has issues",
                flow.Issues);
        }

        private (int Score, string Details) AssessGameplayQuality(GameObject player, IInteraction[] interactions)
        {
            int score = 0;
            var details = new System.Collections.Generic.List<string>();

            // Player mobility (20 points)
            if (player != null)
            {
                score += 10;
                details.Add("Player exists (+10)");
                
                if (player.GetComponent<CharacterController>() != null || player.GetComponent<Rigidbody>() != null)
                {
                    score += 10;
                    details.Add("Player has locomotion (+10)");
                }
            }

            // Interaction density (30 points)
            if (interactions.Length >= 3)
            {
                score += 30;
                details.Add($"Good interaction density: {interactions.Length} interactions (+30)");
            }
            else if (interactions.Length >= 1)
            {
                score += 15;
                details.Add($"Minimal interactions: {interactions.Length} interactions (+15)");
            }

            // Interaction variety (20 points)
            var interactionTypes = interactions.Select(i => i.GetType().Name).Distinct().Count();
            if (interactionTypes >= 2)
            {
                score += 20;
                details.Add($"Good interaction variety: {interactionTypes} types (+20)");
            }
            else if (interactionTypes >= 1)
            {
                score += 10;
                details.Add($"Limited interaction variety: {interactionTypes} types (+10)");
            }

            // Spatial distribution (15 points)
            var positions = interactions.Select(i => (i as MonoBehaviour)?.transform.position ?? Vector3.zero).ToArray();
            var avgDistance = CalculateAverageDistance(positions);
            if (avgDistance > 5f)
            {
                score += 15;
                details.Add($"Good spatial distribution: avg distance {avgDistance:F1} (+15)");
            }
            else if (avgDistance > 2f)
            {
                score += 8;
                details.Add($"Moderate spatial distribution: avg distance {avgDistance:F1} (+8)");
            }

            // Challenge elements (15 points)
            var enemies = UnityEngine.Object.FindObjectsOfType<GameObject>(true)
                .Where(go => go.name.ToLower().Contains("enemy") || go.name.ToLower().Contains("hostile")).ToArray();
            if (enemies.Length >= 2)
            {
                score += 15;
                details.Add($"Good challenge: {enemies.Length} enemies (+15)");
            }
            else if (enemies.Length >= 1)
            {
                score += 8;
                details.Add($"Some challenge: {enemies.Length} enemies (+8)");
            }

            return (score, string.Join("; ", details));
        }

        private (bool IsEngaging, string Feedback) TestEngagement(GameObject player, IInteraction[] interactions)
        {
            var feedback = new System.Collections.Generic.List<string>();

            // Test 1: Can player move around?
            bool canMove = player != null && (player.GetComponent<CharacterController>() != null || player.GetComponent<Rigidbody>() != null);
            if (!canMove) feedback.Add("Player cannot move");

            // Test 2: Are there things to interact with?
            bool hasInteractions = interactions.Length >= 2;
            if (!hasInteractions) feedback.Add("Insufficient interactions for engagement");

            // Test 3: Are interactions reachable?
            bool interactionsReachable = true;
            if (player != null && interactions.Length > 0)
            {
                var playerPos = player.transform.position;
                var maxDistance = 10f; // Reasonable interaction range
                var reachableCount = interactions.Count(i => 
                {
                    var mb = i as MonoBehaviour;
                    return mb != null && Vector3.Distance(playerPos, mb.transform.position) <= maxDistance;
                });
                interactionsReachable = reachableCount >= interactions.Length / 2;
                if (!interactionsReachable) feedback.Add($"Only {reachableCount}/{interactions.Length} interactions are reachable");
            }

            // Test 4: Is there progression/objectives?
            bool hasObjectives = interactions.Any(i => 
            {
                var mb = i as MonoBehaviour;
                return mb != null && (mb.name.ToLower().Contains("goal") || mb.name.ToLower().Contains("objective") || mb.name.ToLower().Contains("target"));
            });
            if (!hasObjectives) feedback.Add("No clear objectives or goals");

            // Test 5: Is there challenge/opposition?
            var enemies = UnityEngine.Object.FindObjectsOfType<GameObject>(true)
                .Where(go => go.name.ToLower().Contains("enemy") || go.name.ToLower().Contains("hostile")).ToArray();
            bool hasChallenge = enemies.Length >= 1;
            if (!hasChallenge) feedback.Add("No challenge or opposition elements");

            bool isEngaging = canMove && hasInteractions && interactionsReachable && (hasObjectives || hasChallenge);
            return (isEngaging, feedback.Count == 0 ? "All engagement tests passed" : string.Join("; ", feedback));
        }

        private (bool IsValid, string Issues) ValidateGameplayFlow(GameObject player, IInteraction[] interactions, GameObject enemy)
        {
            var issues = new System.Collections.Generic.List<string>();

            // Check for logical flow
            if (player == null)
            {
                issues.Add("No player - cannot start gameplay");
                return (false, string.Join("; ", issues));
            }

            if (interactions.Length == 0)
            {
                issues.Add("No interactions - nothing to do");
                return (false, string.Join("; ", issues));
            }

            // Check for win/lose conditions
            var hasWinCondition = interactions.Any(i => 
            {
                var mb = i as MonoBehaviour;
                return mb != null && (mb.name.ToLower().Contains("goal") || mb.name.ToLower().Contains("win") || mb.name.ToLower().Contains("finish"));
            });
            if (!hasWinCondition) issues.Add("No clear win condition");

            // Check for spatial coherence
            if (interactions.Length > 1)
            {
                var positions = interactions.Select(i => (i as MonoBehaviour)?.transform.position ?? Vector3.zero).ToArray();
                var bounds = CalculateBounds(positions);
                if (bounds.size.magnitude < 5f) issues.Add("Interactions too clustered - may feel cramped");
            }

            // Check for reasonable challenge
            if (enemy == null && interactions.Length < 3)
            {
                issues.Add("No enemies and few interactions - may be too easy");
            }

            return (issues.Count == 0, issues.Count == 0 ? "Gameplay flow is coherent" : string.Join("; ", issues));
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
