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
        }
    }

}
#endif
