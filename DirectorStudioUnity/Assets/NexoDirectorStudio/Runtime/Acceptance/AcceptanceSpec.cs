using UnityEngine;

namespace NexoDirectorStudio.Acceptance
{
    [CreateAssetMenu(menuName="Director/Acceptance Spec")]
    public sealed class AcceptanceSpec : ScriptableObject
    {
        [Min(0)] public int MinInteractions = 1;
        public string[] MustTriggerIds;
        [Min(0)] public float MaxPlanMs = 1000;
        [Min(0)] public float MaxLayoutMs = 1500;
        [Min(0)] public float MaxPlacementMs = 1500;
        [Min(0)] public float MaxContentMs = 3000;
    }
}
