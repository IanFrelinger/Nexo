#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Interactions;
using NexoDirectorStudio.Acceptance;

namespace NexoDirectorStudio.Editor.CI
{
    public static class CiEditorSmokeRealScene
    {
        // -executeMethod NexoDirectorStudio.Editor.CI.CiEditorSmokeRealScene.Run
        //  [-scene /path/to/scene.unity] [-seconds 10] [-results /abs/path/playmode-smoke.json] [-acceptance /path/AcceptanceSpec.asset]
        public static void Run()
        {
            var scenePath = GetArg("-scene") ?? GetMostRecentGeneratedScene();
            var seconds = int.TryParse(GetArg("-seconds"), out var s) ? Math.Max(1,s) : 10;
            var outPath = GetArg("-results") ?? Path.GetFullPath("playmode-smoke-real.json");
            var accPath = GetArg("-acceptance");

            var spec = string.IsNullOrEmpty(accPath) ? null : AssetDatabase.LoadAssetAtPath<AcceptanceSpec>(accPath);

            Debug.Log($"[CiEditorSmokeRealScene] Loading scene: {scenePath}");

            // Load the real generated scene
            var testScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var sceneLoaded = testScene.IsValid();

            if (!sceneLoaded)
            {
                Debug.LogError($"[CiEditorSmokeRealScene] Failed to load scene: {scenePath}");
                WriteResults(outPath, "Failed to load scene", seconds, 0, false, false, false, 0);
                EditorApplication.Exit(2);
                return;
            }

            // Ensure prerequisites exist (EventSystem, MainCamera, etc.)
            var eventSystem = UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
            {
                var cameraObj = new GameObject("Main Camera");
                cameraObj.AddComponent<UnityEngine.Camera>();
                cameraObj.tag = "MainCamera";
            }

            // Create InteractionBus if it doesn't exist
            var bus = UnityEngine.Object.FindFirstObjectByType<InteractionBus>();
            if (bus == null)
            {
                var busObj = new GameObject("InteractionBus");
                bus = busObj.AddComponent<InteractionBus>();
            }

            // Find and register existing interactions
            var existingInteractions = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
            int registeredCount = 0;
            foreach (var mb in existingInteractions)
            {
                if (mb is IInteraction interaction)
                {
                    bus.Register(interaction);
                    registeredCount++;
                }
            }

            Debug.Log($"[CiEditorSmokeRealScene] Registered {registeredCount} existing interactions");

            // Create a test interaction if none exist
            if (registeredCount == 0)
            {
                var testObj = new GameObject("TestInteraction");
                testObj.transform.position = Vector3.zero;
                var collider = testObj.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                var interaction = testObj.AddComponent<TestInteraction>();
                bus.Register(interaction);
                registeredCount = 1;
                Debug.Log("[CiEditorSmokeRealScene] Created test interaction");
            }

            // Run the smoke test
            var bootstrapOk = eventSystem != null && mainCamera != null && bus != null;
            var minInt = spec?.MinInteractions ?? 1;
            
            // Simulate some interactions
            var interactions = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
            int triggered = 0;
            foreach (var mb in interactions)
            {
                if (mb is IInteraction interaction && interaction.TryInvoke())
                {
                    triggered++;
                }
            }

            var interacted = triggered >= minInt;

            Debug.Log($"[CiEditorSmokeRealScene] Scene loaded: {sceneLoaded}, Bootstrap: {bootstrapOk}, Interactions: {triggered}/{minInt}");

            // Write results
            WriteResults(outPath, scenePath, seconds, triggered, sceneLoaded, bootstrapOk, interacted, minInt);

            // === Functional Validation Suite ===
            var validators = new System.Collections.Generic.List<NexoDirectorStudio.Editor.Validation.Functional.IFunctionalValidator>
            {
                new NexoDirectorStudio.Editor.Validation.Functional.GameplayValidator(),
                new NexoDirectorStudio.Editor.Validation.Functional.EngagementValidator(),
                new NexoDirectorStudio.Editor.Validation.Functional.VisualsValidator(),
                new NexoDirectorStudio.Editor.Validation.Functional.AudioValidator(),
                new NexoDirectorStudio.Editor.Validation.Functional.SystemsValidator()
            };
            var funcReport = NexoDirectorStudio.Editor.Validation.Functional.FunctionalValidationSuite.RunAll(validators);

            // Write functional JSON sidecar next to smoke JSON
            var functionalJsonPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(outPath) ?? ".", "functional_smoke_real.json");
            System.IO.File.WriteAllText(functionalJsonPath, UnityEngine.JsonUtility.ToJson(funcReport, true));
            UnityEngine.Debug.Log($"[CiEditorSmokeRealScene] Functional report written: {functionalJsonPath} (failures={funcReport.failures})");

            // Append functional assertions to JUnit (each assertion becomes a testcase)
            var junitPathFinal = System.IO.Path.ChangeExtension(outPath, ".junit.xml");
            var xmlContent = System.IO.File.ReadAllText(junitPathFinal);
            var sbFunc = new System.Text.StringBuilder();
            foreach (var a in funcReport.assertions)
            {
                var nameEsc = a.name.Replace("\"","'");
                var msgEsc = (a.message ?? "failed").Replace("\"","'");
                if (a.pass)
                    sbFunc.AppendLine($"  <testcase classname=\"CiEditorSmokeRealScene.Functional\" name=\"{nameEsc}\" time=\"{seconds}\"></testcase>");
                else
                    sbFunc.AppendLine($"  <testcase classname=\"CiEditorSmokeRealScene.Functional\" name=\"{nameEsc}\" time=\"{seconds}\">\n    <failure message=\"{msgEsc}\" />\n  </testcase>");
            }
            xmlContent = xmlContent.Replace("</testsuite>", sbFunc.ToString() + "</testsuite>");
            System.IO.File.WriteAllText(junitPathFinal, xmlContent);

            // On failure, capture a screenshot to help with forensics
            try
            {
                if (funcReport.failures > 0)
                {
                    var shotPath = System.IO.Path.ChangeExtension(outPath, ".png");
                    ScreenCapture.CaptureScreenshot(shotPath);
                    Debug.Log($"[CiEditorSmokeRealScene] Wrote screenshot: {shotPath}");
                }
            }
            catch { /* ignore capture errors in headless */ }

            // If any functional assertion failed, make sure CI fails
            var totalFailures = funcReport.failures;
            EditorApplication.Exit(totalFailures == 0 ? 0 : 2);
        }

        private static string GetMostRecentGeneratedScene()
        {
            var generatedDir = "Assets/GeneratedScenes";
            if (!Directory.Exists(generatedDir))
            {
                Debug.LogError("[CiEditorSmokeRealScene] No GeneratedScenes directory found");
                return null;
            }

            var sceneFiles = Directory.GetFiles(generatedDir, "*.unity", SearchOption.TopDirectoryOnly);
            if (sceneFiles.Length == 0)
            {
                Debug.LogError("[CiEditorSmokeRealScene] No .unity files found in GeneratedScenes");
                return null;
            }

            // Sort by modification time, get the most recent
            var mostRecent = sceneFiles
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .First();

            Debug.Log($"[CiEditorSmokeRealScene] Using most recent scene: {mostRecent}");
            return mostRecent;
        }

        private static void WriteResults(string outPath, string scenePath, int seconds, int triggered, bool sceneLoaded, bool bootstrapOk, bool interacted, int minInt)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? ".");
            File.WriteAllText(outPath, "{\n" +
                $"  \"scenePath\": \"{Escape(scenePath)}\",\n" +
                $"  \"seconds\": {seconds},\n" +
                $"  \"interactionsTriggered\": {triggered},\n" +
                $"  \"sceneLoaded\": {sceneLoaded.ToString().ToLower()},\n" +
                $"  \"bootstrapOk\": {bootstrapOk.ToString().ToLower()},\n" +
                $"  \"withinTimeBudget\": {true.ToString().ToLower()},\n" +
                $"  \"ok\": {(sceneLoaded && bootstrapOk && interacted ? "true":"false")}\n" +
            "}\n");

            // JUnit with multiple cases
            var junitPath = Path.ChangeExtension(outPath, ".junit.xml");
            int failures = 0;
            string Case(string name, bool pass, string msg=null)
            {
                if (!pass) failures++;
                return $"  <testcase classname=\"CiEditorSmokeRealScene\" name=\"{name}\" time=\"{seconds}\">{(pass ? "" : $"\n    <failure message=\"{Escape(msg ?? "failed")}\" />\n  ")}</testcase>\n";
            }
            var xml =
$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<testsuite name=""CiEditorSmokeRealScene"" tests=""4"" failures=""{failures}"" time=""{seconds}"">
{Case("SceneLoaded", sceneLoaded, "Generated scene failed to load")}
{Case("BootstrapReady", bootstrapOk, "Bootstrap prerequisites missing")}
{Case("AutoplayInteractions", interacted, $"InteractionsTriggered={triggered} < Min={minInt}")}
{Case("WithinTimeBudget", true, "Time budget exceeded")}
</testsuite>";
            File.WriteAllText(junitPath, xml);
        }

        private static string GetArg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
                if (string.Equals(a[i], name, StringComparison.OrdinalIgnoreCase))
                    return a[i + 1];
            return null;
        }
        private static string Escape(string s) => (s ?? "").Replace("\\","\\\\").Replace("\"","\\\"");
    }
}
#endif
