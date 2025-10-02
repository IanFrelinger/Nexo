#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NexoDirectorStudio.Editor.Validation.Functional
{
    public sealed class VisualsValidator : IFunctionalValidator
    {
        public string Name => "Visuals";

        public void Run(FunctionalReport r)
        {
            // Main camera + at least one Light
            r.Add("Visuals.MainCamera", Camera.main != null, Camera.main ? "MainCamera present" : "No Camera tagged MainCamera");
            var anyLight = UnityEngine.Object.FindObjectsOfType<Light>(true).Any(l => l.enabled);
            r.Add("Visuals.LightPresent", anyLight, anyLight ? "At least one Light is enabled" : "No enabled Light in scene");

            // No error/pink shaders; all renderers have materials
            var rends = UnityEngine.Object.FindObjectsOfType<Renderer>(true);
            bool matsOk = true; bool errorShader = false;
            foreach (var rrr in rends)
            {
                var mats = rrr.sharedMaterials;
                if (mats == null || mats.Length == 0) { matsOk = false; break; }
                foreach (var m in mats)
                {
                    if (m == null) { matsOk = false; break; }
                    var sh = m.shader;
                    if (sh != null && sh.name == "Hidden/InternalErrorShader") { errorShader = true; break; }
                }
                if (!matsOk || errorShader) break;
            }
            r.Add("Visuals.MaterialsAssigned", matsOk, matsOk ? "All renderers have materials" : "Some renderers have no/NULL materials assigned");
            r.Add("Visuals.NoErrorShader", !errorShader, !errorShader ? "No InternalErrorShader in use" : "Pink/error shader detected (compile/import issue)");

            // Transforms sane (no NaNs, non-zero scale)
            bool transformsOk = true;
            foreach (var t in UnityEngine.Object.FindObjectsOfType<Transform>(true))
            {
                var pos = t.position; var rot = t.rotation; var scl = t.localScale;
                if (float.IsNaN(pos.x) || float.IsNaN(rot.x) || scl == Vector3.zero) { transformsOk = false; break; }
            }
            r.Add("Visuals.TransformSane", transformsOk, transformsOk ? "No NaN or zero-scale transforms" : "Invalid transforms found (NaN/zero scale)");
        }
    }
}
#endif
