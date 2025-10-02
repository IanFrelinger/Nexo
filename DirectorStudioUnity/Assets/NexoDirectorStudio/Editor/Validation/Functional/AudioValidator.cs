#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NexoDirectorStudio.Editor.Validation.Functional
{
    public sealed class AudioValidator : IFunctionalValidator
    {
        public string Name => "Audio";

        public void Run(FunctionalReport r)
        {
            var listeners = UnityEngine.Object.FindObjectsOfType<AudioListener>(true).Where(l => l.enabled).ToArray();
            r.Add("Audio.SingleListener", listeners.Length == 1, $"Enabled listeners: {listeners.Length}", "There should be exactly one enabled AudioListener.");

            var sources = UnityEngine.Object.FindObjectsOfType<AudioSource>(true);
            bool anySource = sources.Any();
            r.Add("Audio.SourcePresent", anySource, anySource ? "At least one AudioSource exists" : "No AudioSource found");

            int badAwake = sources.Count(s => s.playOnAwake && s.clip == null);
            r.Add("Audio.PlayOnAwakeHasClip", badAwake == 0, badAwake == 0 ? "All playOnAwake sources have clips" : $"playOnAwake sources missing clips: {badAwake}");

            int volOut = sources.Count(s => s.volume < 0f || s.volume > 1f);
            r.Add("Audio.VolumeRange", volOut == 0, volOut == 0 ? "All volumes in [0,1]" : $"Out-of-range volumes: {volOut}");
        }
    }
}
#endif
