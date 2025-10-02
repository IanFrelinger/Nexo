#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Simple test to verify command line argument parsing
    /// </summary>
    public static class ArgumentTestWizard
    {
        // Usage: -executeMethod NexoDirectorStudio.Editor.ArgumentTestWizard.RunTest
        public static void RunTest()
        {
            Debug.Log("🧪 === ARGUMENT PARSING TEST ===");
            
            var args = Environment.GetCommandLineArgs();
            Debug.Log($"Total arguments: {args.Length}");
            
            for (int i = 0; i < args.Length; i++)
            {
                Debug.Log($"  [{i}] {args[i]}");
            }
            
            Debug.Log("\n🔍 Testing specific argument parsing:");
            var inspiration = ReadArg(args, "--inspiration");
            var emotionalGoal = ReadArg(args, "--emotionalGoal");
            var coreFantasy = ReadArg(args, "--coreFantasy");
            
            Debug.Log($"--inspiration: '{inspiration}'");
            Debug.Log($"--emotionalGoal: '{emotionalGoal}'");
            Debug.Log($"--coreFantasy: '{coreFantasy}'");
            
            EditorApplication.Exit(0);
        }
        
        private static string ReadArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return null;
        }
    }
}
#endif
