#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NexoDirectorStudio.Editor.Validation.Functional
{
    public sealed class SystemsValidator : IFunctionalValidator
    {
        public string Name => "Systems";

        public void Run(FunctionalReport r)
        {
            // EventSystem present (for UI & pointer events)
            var ev = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            r.Add("Systems.EventSystem", ev != null, ev ? "EventSystem present" : "No EventSystem in scene");

            // TimeScale sane
            r.Add("Systems.TimeScale", Time.timeScale > 0f, Time.timeScale > 0f ? "TimeScale > 0" : "TimeScale is 0 (paused)");

            // Physics substrates exist (at least one Collider in scene)
            var hasCollider = UnityEngine.Object.FindObjectsOfType<Collider>(true).Any();
            r.Add("Systems.PhysicsPresent", hasCollider, hasCollider ? "At least one Collider present" : "No Collider present (physics likely inert)");

            // Layer sanity: raycast hits something from MainCamera forward (smoke probe)
            bool rayOk = false;
            if (Camera.main)
            {
                var origin = Camera.main.transform.position;
                var dir = Camera.main.transform.forward;
                rayOk = Physics.Raycast(origin, dir, out _, 1000f);
            }
            r.Add("Systems.Raycastable", rayOk, rayOk ? "Camera forward ray hits something" : "Nothing is hit by a forward ray from MainCamera (layers/colliders?)");
        }
    }
}
#endif
