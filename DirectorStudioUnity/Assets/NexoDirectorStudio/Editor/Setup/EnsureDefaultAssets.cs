#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using NexoDirectorStudio.Acceptance;
using NexoDirectorStudio.Scenarios;

namespace NexoDirectorStudio.Editor.Setup
{
    [InitializeOnLoad]
    public static class EnsureDefaultAssets
    {
        static EnsureDefaultAssets()
        {
            EnsureAcceptance();
            EnsureScenario();
        }

        static void EnsureAcceptance()
        {
            var path = "Assets/NexoDirectorStudio/Acceptance/Default.asset";
            if (AssetDatabase.LoadAssetAtPath<AcceptanceSpec>(path) != null) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var spec = ScriptableObject.CreateInstance<AcceptanceSpec>();
            spec.MinInteractions = 1;
            AssetDatabase.CreateAsset(spec, path);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Created default AcceptanceSpec at " + path);
        }

        static void EnsureScenario()
        {
            var path = "Assets/NexoDirectorStudio/Scenarios/Smoke.asset";
            if (AssetDatabase.LoadAssetAtPath<Scenario>(path) != null) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var scn = ScriptableObject.CreateInstance<Scenario>();
            AssetDatabase.CreateAsset(scn, path);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Created default Scenario at " + path);
        }
    }
}
#endif
