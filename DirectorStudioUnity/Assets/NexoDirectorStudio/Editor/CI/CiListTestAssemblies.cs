#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NexoDirectorStudio.Editor.CI
{
    public static class CiListTestAssemblies
    {
        // Usage:
        //  -executeMethod NexoDirectorStudio.Editor.CI.CiListTestAssemblies.Run
        public static void Run()
        {
            var dir = Path.Combine("Library", "ScriptAssemblies");
            if (!Directory.Exists(dir))
            {
                Debug.Log("[CiListTestAssemblies] No ScriptAssemblies yet (open once to compile).");
                EditorApplication.Exit(0);
                return;
            }
            var dlls = Directory.GetFiles(dir, "*Tests*.dll").OrderBy(x => x).ToArray();
            Debug.Log("[CiListTestAssemblies] Test DLLs:\n" + string.Join("\n", dlls));
            var hasPlay = dlls.Any(s => s.EndsWith("NexoDirectorStudio.Tests.PlayMode.dll"));
            Debug.Log("[CiListTestAssemblies] Has PlayMode DLL: " + hasPlay);
            EditorApplication.Exit(0);
        }
    }
}
#endif