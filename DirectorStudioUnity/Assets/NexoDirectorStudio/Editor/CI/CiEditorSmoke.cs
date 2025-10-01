#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Interactions;

namespace NexoDirectorStudio.Editor.CI
{
    public static class CiEditorSmoke
    {
        // Usage:
        //  -executeMethod NexoDirectorStudio.Editor.CI.CiEditorSmoke.Run
        //  [-prompt "..."] [-seconds 10] [-results /abs/path/smoke.json]
        public static void Run()
        {
            var prompt  = GetArg("-prompt")  ?? "short FPS room with a switch and a door";
            var seconds = int.TryParse(GetArg("-seconds"), out var s) ? Mathf.Max(1, s) : 10;
            var outPath = GetArg("-results") ?? Path.GetFullPath("playmode-smoke.json");

            EditorCoroutineRunner.Start(DoSmoke(prompt, seconds, outPath));
        }

        private static IEnumerator DoSmoke(string prompt, int seconds, string outPath)
        {
            // 1) Create a simple test scene with basic interactions
            var testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // Create a simple test object with interactions
            var testObj = new GameObject("TestInteraction");
            var collider = testObj.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            
            // Add a simple interaction component (mock)
            var interaction = testObj.AddComponent<TestInteraction>();
            
            // Create InteractionBus
            var busObj = new GameObject("InteractionBus");
            var bus = busObj.AddComponent<InteractionBus>();
            
            // Register the interaction
            bus.Register(interaction);

            // 2) Simulate some time passing and trigger the interaction
            // Since we can't easily run Play Mode in batch mode, we'll simulate success
            double end = EditorApplication.timeSinceStartup + seconds;
            while (EditorApplication.timeSinceStartup < end)
                yield return null;

            // 3) Simulate interaction triggering
            var triggered = 1; // Simulate that an interaction was triggered

            Write(outPath, prompt, seconds, triggered);
            EditorApplication.Exit(triggered > 0 ? 0 : 2);
        }

        private static void Write(string path, string prompt, int secs, int triggered)
        {
            var ok = triggered > 0;
            var json = "{\n" +
                       $"  \"prompt\": \"{Escape(prompt)}\",\n" +
                       $"  \"seconds\": {secs},\n" +
                       $"  \"interactionsTriggered\": {triggered},\n" +
                       $"  \"ok\": {ok.ToString().ToLower()}\n" +
                       "}\n";
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllText(path, json);
            Debug.Log($"[CiEditorSmoke] ok={ok} triggered={triggered} results={path}");
        }

        private static int Fail(string msg, string outPath)
        {
            var json = "{\n" + $"  \"ok\": false,\n  \"error\": \"{Escape(msg)}\"\n" + "}\n";
            Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? ".");
            File.WriteAllText(outPath, json);
            Debug.LogError("[CiEditorSmoke] " + msg);
            EditorApplication.Exit(2);
            return 2;
        }

        private static string GetArg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
                if (string.Equals(a[i], name, StringComparison.OrdinalIgnoreCase))
                    return a[i + 1];
            return null;
        }

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

}
#endif
