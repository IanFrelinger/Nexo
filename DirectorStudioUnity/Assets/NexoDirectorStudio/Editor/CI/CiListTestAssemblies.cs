#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NexoDirectorStudio.Editor.CI
{
    public static class CiListTestAssemblies
    {
        // -executeMethod NexoDirectorStudio.Editor.CI.CiListTestAssemblies.Run
        public static void Run()
        {
            var dir = Path.Combine("Library", "ScriptAssemblies");
            if (!Directory.Exists(dir))
            {
                Debug.Log("[CiListTestAssemblies] No ScriptAssemblies yet.");
                EditorApplication.Exit(0);
                return;
            }

            var dlls = Directory.GetFiles(dir, "*Tests*.dll").OrderBy(x => x).ToArray();
            Debug.Log("[CiListTestAssemblies] DLLs:\n" + string.Join("\n", dlls));
            var match = dlls.FirstOrDefault(s => s.EndsWith("NexoDirectorStudio.Tests.PlayMode.dll"));
            Debug.Log("[CiListTestAssemblies] Has PlayMode DLL: " + (match != null));
            EditorApplication.Exit(0);
        }
    }
}
#endif
