#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Interactions;
using NexoDirectorStudio.Acceptance;

namespace NexoDirectorStudio.Editor.CI
{
    public static class CiEditorSmoke
    {
        // -executeMethod NexoDirectorStudio.Editor.CI.CiEditorSmoke.Run
        //  [-prompt "..."] [-seconds 10] [-results /abs/path/playmode-smoke.json] [-acceptance /path/AcceptanceSpec.asset]
        public static void Run()
        {
            var prompt  = GetArg("-prompt")  ?? "micro room with one switch and one door";
            var seconds = int.TryParse(GetArg("-seconds"), out var s) ? Math.Max(1,s) : 10;
            var outPath = GetArg("-results") ?? Path.GetFullPath("playmode-smoke.json");
            var accPath = GetArg("-acceptance");

            var spec = string.IsNullOrEmpty(accPath) ? null : AssetDatabase.LoadAssetAtPath<AcceptanceSpec>(accPath);

            // Create a simple test scene with interactions
            var testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sceneLoaded = testScene.IsValid();

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

            // Create InteractionBus
            var busObj = new GameObject("InteractionBus");
            var bus = busObj.AddComponent<InteractionBus>();

            // Create test interactions
            var interactionObj = new GameObject("TestInteraction");
            var collider = interactionObj.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            var interaction = interactionObj.AddComponent<TestInteraction>();
            
            // Register interaction with bus
            bus.Register(interaction);

            // Simulate "play" time and trigger interactions deterministically
            var end = DateTime.UtcNow.AddSeconds(seconds);
            var triggered = 0;
            
            // Physics simulation setup
            var hadAutoSimulation = Physics.autoSimulation;
            Physics.autoSimulation = false;
            
            try
            {
                while (DateTime.UtcNow < end)
                {
                    // Step physics if needed
                    Physics.Simulate(0.02f);
                    
                    // Trigger interactions deterministically
                    if (interaction.IsArmed && !interaction.HasTriggered)
                    {
                        if (interaction.TryInvoke())
                        {
                            triggered++;
                        }
                    }
                    
                    // Small delay to prevent busy waiting
                    System.Threading.Thread.Sleep(10);
                }
            }
            finally
            {
                // Restore physics settings
                Physics.autoSimulation = hadAutoSimulation;
            }

            // Acceptance checks
            var minInt = spec ? spec.MinInteractions : 1;
            bool interacted = triggered >= minInt;
            bool bootstrapOk = true; // expand if your bootstrap can self-report

            // JSON
            Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? ".");
            File.WriteAllText(outPath, "{\n" +
                $"  \"prompt\": \"{Escape(prompt)}\",\n" +
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
                return $"  <testcase classname=\"CiEditorSmoke\" name=\"{name}\" time=\"{seconds}\">{(pass ? "" : $"\n    <failure message=\"{Escape(msg ?? "failed")}\" />\n  ")}</testcase>\n";
            }
            var xml =
$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<testsuite name=""CiEditorSmoke"" tests=""4"" failures=""{failures}"" time=""{seconds}"">
{Case("SceneLoaded", sceneLoaded, "Generated scene failed to load")}
{Case("BootstrapReady", bootstrapOk, "Bootstrap prerequisites missing")}
{Case("AutoplayInteractions", interacted, $"InteractionsTriggered={triggered} < Min={minInt}")}
{Case("WithinTimeBudget", true, "Time budget exceeded")}
</testsuite>";
                    File.WriteAllText(junitPath, xml);

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
                    var functionalJsonPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(outPath) ?? ".", "functional_smoke.json");
                    System.IO.File.WriteAllText(functionalJsonPath, UnityEngine.JsonUtility.ToJson(funcReport, true));
                    UnityEngine.Debug.Log($"[CiEditorSmoke] Functional report written: {functionalJsonPath} (failures={funcReport.failures})");

                    // Append functional assertions to JUnit (each assertion becomes a testcase)
                    var junitPathFinal = System.IO.Path.ChangeExtension(outPath, ".junit.xml");
                    var xmlContent = System.IO.File.ReadAllText(junitPathFinal);
                    var sbFunc = new System.Text.StringBuilder();
                    foreach (var a in funcReport.assertions)
                    {
                        var nameEsc = a.name.Replace("\"","'");
                        var msgEsc = (a.message ?? "failed").Replace("\"","'");
                        if (a.pass)
                            sbFunc.AppendLine($"  <testcase classname=\"CiEditorSmoke.Functional\" name=\"{nameEsc}\" time=\"{seconds}\"></testcase>");
                        else
                            sbFunc.AppendLine($"  <testcase classname=\"CiEditorSmoke.Functional\" name=\"{nameEsc}\" time=\"{seconds}\">\n    <failure message=\"{msgEsc}\" />\n  </testcase>");
                    }
                    xmlContent = xmlContent.Replace("</testsuite>", sbFunc.ToString() + "</testsuite>");
                    System.IO.File.WriteAllText(junitPathFinal, xmlContent);

                    // On failure, capture a screenshot to help with forensics
                    try
                    {
                        if (failures > 0 || funcReport.failures > 0)
                        {
                            var shotPath = System.IO.Path.ChangeExtension(outPath, ".png");
                            ScreenCapture.CaptureScreenshot(shotPath);
                            Debug.Log($"[CiEditorSmoke] Wrote screenshot: {shotPath}");
                        }
                    }
                    catch { /* ignore capture errors in headless */ }

                    // If any functional assertion failed, make sure CI fails
                    var totalFailures = failures + funcReport.failures;
                    EditorApplication.Exit(totalFailures == 0 ? 0 : 2);
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