using System.Collections.Generic;
using UnityEngine;

namespace NexoDirectorStudio.Scenarios
{
    public abstract class ScriptableStep : ScriptableObject { public abstract void Describe(System.Text.StringBuilder sb); }

    [CreateAssetMenu(menuName="Director/Scenario")]
    public sealed class Scenario : ScriptableObject
    {
        public List<ScriptableStep> Steps = new();
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var s in Steps) s?.Describe(sb);
            return sb.ToString();
        }
    }

    [CreateAssetMenu(menuName="Director/Steps/MoveTo")]
    public sealed class MoveTo : ScriptableStep { public string TargetId; public override void Describe(System.Text.StringBuilder sb) => sb.AppendLine($"MoveTo:{TargetId}"); }

    [CreateAssetMenu(menuName="Director/Steps/Interact")]
    public sealed class Interact : ScriptableStep { public string TargetId; public override void Describe(System.Text.StringBuilder sb) => sb.AppendLine($"Interact:{TargetId}"); }

    [CreateAssetMenu(menuName="Director/Steps/WaitFor")]
    public sealed class WaitFor : ScriptableStep { public string EventName; public float Timeout=5; public override void Describe(System.Text.StringBuilder sb) => sb.AppendLine($"WaitFor:{EventName}@{Timeout}s"); }
}
