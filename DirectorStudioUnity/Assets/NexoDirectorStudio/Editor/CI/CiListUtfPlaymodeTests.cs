#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace NexoDirectorStudio.Editor.CI
{
    public static class CiListUtfPlaymodeTests
    {
        // Usage:
        //  -executeMethod NexoDirectorStudio.Editor.CI.CiListUtfPlaymodeTests.Run
        public static void Run()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RetrieveTestList(TestMode.PlayMode, (testsToRun) =>
            {
                Debug.Log($"[CiListUtfPlaymodeTests] PlayMode test cases: {testsToRun?.TestCaseCount ?? 0}");
                PrintTree(testsToRun, 0);
                EditorApplication.Exit(0);
            });
        }

        private static void PrintTree(ITestAdaptor n, int depth)
        {
            if (n == null) return;
            var pad = new string(' ', depth * 2);
            Debug.Log($"{pad}- {n.Name} (Cases={n.TestCaseCount})");
            if (n.Children == null) return;
            foreach (var c in n.Children) PrintTree(c, depth + 1);
        }
    }
}
#endif
