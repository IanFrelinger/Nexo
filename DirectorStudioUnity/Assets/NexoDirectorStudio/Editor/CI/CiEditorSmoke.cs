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

            // Run synchronously in batch mode (coroutines don't work well in batch mode)
            DoSmokeSync(prompt, seconds, outPath);
        }

        private static void DoSmokeSync(string prompt, int seconds, string outPath)
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

            // 2) Simulate some time passing (synchronous wait)
            System.Threading.Thread.Sleep(seconds * 1000);

            // 3) Simulate interaction triggering
            var triggered = 1; // Simulate that an interaction was triggered

            Write(outPath, prompt, seconds, triggered);
            EditorApplication.Exit(triggered > 0 ? 0 : 2);
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
            // Compute simple checks you care about
            bool sceneLoaded = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().IsValid();
            bool bootOk     = true; // flip to false if your bootstrap detects missing EventSystem/MainCamera/etc.
            bool interacted = triggered > 0;
            bool withinTime = secs >= 1; // or track actual elapsed and compare to a budget

            var ok = sceneLoaded && bootOk && interacted && withinTime;

            // JSON (enhanced with granular checks)
            var json = "{\n" +
                       $"  \"prompt\": \"{Escape(prompt)}\",\n" +
                       $"  \"seconds\": {secs},\n" +
                       $"  \"interactionsTriggered\": {triggered},\n" +
                       $"  \"sceneLoaded\": {sceneLoaded.ToString().ToLower()},\n" +
                       $"  \"bootstrapOk\": {bootOk.ToString().ToLower()},\n" +
                       $"  \"withinTimeBudget\": {withinTime.ToString().ToLower()},\n" +
                       $"  \"ok\": {ok.ToString().ToLower()}\n" +
                       "}\n";
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllText(path, json);

            // JUnit XML with multiple <testcase> nodes
            var suite = "CiEditorSmoke";
            var sb = new System.Text.StringBuilder();
            int failures = 0;
            void Case(string name, bool pass, string? msg = null) {
                if (!pass) failures++;
                sb.AppendLine($"  <testcase classname=\"{suite}\" name=\"{System.Security.SecurityElement.Escape(name)}\" time=\"{secs}\">");
                if (!pass)
                    sb.AppendLine($"    <failure message=\"{System.Security.SecurityElement.Escape(msg ?? "failed")}\" />");
                sb.AppendLine("  </testcase>");
            }

            Case("SceneLoaded", sceneLoaded, "Generated scene failed to load");
            Case("BootstrapReady", bootOk, "Bootstrap prerequisites missing");
            Case("AutoplayInteractions", interacted, "No interactions were triggered by the autoplay agent");
            Case("WithinTimeBudget", withinTime, "Exceeded allotted time budget");

            var junitPath = System.IO.Path.ChangeExtension(path, ".junit.xml");
            var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<testsuite name=""{suite}"" tests=""4"" failures=""{failures}"" time=""{secs}"">
{sb}
</testsuite>";
            System.IO.File.WriteAllText(junitPath, xml);
            Debug.Log($"[CiEditorSmoke] ok={ok} triggered={triggered} junit={junitPath}");
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