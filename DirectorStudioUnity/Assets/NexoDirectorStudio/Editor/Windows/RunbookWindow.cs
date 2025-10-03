#if UNITY_EDITOR
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Phases;
using NexoDirectorStudio.Config;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Validators;
using UnityEditor;
using UnityEngine;

namespace NexoDirectorStudio.EditorUI
{
    public sealed class RunbookWindow : EditorWindow
    {
        private string _configPath = "nexo.pipeline.json";
        private PipelineConfig _cfg = new PipelineConfig();
        private Vector2 _scroll;

        [MenuItem("Nexo/Director Runbook")]
        public static void ShowWindow() => GetWindow<RunbookWindow>("Director Runbook");

        private void OnEnable() { if (File.Exists(_configPath)) _cfg = PipelineConfigLoader.LoadFromPath(_configPath, _cfg); }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _configPath = EditorGUILayout.TextField("Config Path", _configPath);
            if (GUILayout.Button("Load", GUILayout.Width(80))) _cfg = PipelineConfigLoader.LoadFromPath(_configPath, _cfg);
            EditorGUILayout.EndHorizontal();

            _cfg.prompt = EditorGUILayout.TextField("Prompt", _cfg.prompt);
            _cfg.seed   = EditorGUILayout.IntField("Seed", _cfg.seed);
            _cfg.artifacts = EditorGUILayout.TextField("Artifacts Root", _cfg.artifacts);
            EditorGUILayout.Space();

            if (GUILayout.Button("Run Pipeline (Plan→Layout→Placement→Content→Validate)"))
                _ = RunPipeline();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Review Mode", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Review mode ensures playable results with fallbacks for demos", MessageType.Info);
            
            if (GUILayout.Button("Run for Review (Always-Green)", GUILayout.Height(30)))
                _ = RunForReview("always-green");

            if (GUILayout.Button("Run for Review (Strict)", GUILayout.Height(30)))
                _ = RunForReview("strict");

            if (GUILayout.Button("Create Presentation Bundle", GUILayout.Height(30)))
                CreatePresentationBundle();

            if (GUILayout.Button("Open Artifacts Root"))
                EditorUtility.RevealInFinder(_cfg.artifacts);

            EditorGUILayout.EndScrollView();
        }

        private async Task RunPipeline()
        {
            var svc = new DirectorStudioService(); // direct instantiation (not MonoBehaviour)
            var ctx = new RunContext { Seed = _cfg.seed, ArtifactRoot = _cfg.artifacts, Checkpoints = new FileCheckpointStore() };
            ctx.Rng.Init(_cfg.seed);

            // Build default phases; if cfg.phases has entries, respect their order/types.
            var runner = new PhaseRunner();
            var reg = PhaseRegistry.BuildDefault(svc);
            if (_cfg.phases != null && _cfg.phases.Length > 0)
                foreach (var p in _cfg.phases) runner.Add(reg.Create(p.type, svc));
            else
                runner.Add(new PlanPhase(svc.GetService<IPlanGameSliceCommand>()))
                      .Add(new LayoutPhase(svc.GetService<IBuildWorldLayoutCommand>()))
                      .Add(new PlacementPhase(svc.GetService<IPlaceInteractionsCommand>()))
                      .Add(new ContentPhase(svc.GetService<ICreateContentBundleCommand>()))
                      .Add(new ValidatePhase(svc.GetService<IValidator<ContentBundle>>()));

            var brief = new DesignBrief(Description: _cfg.prompt, GenreHint: "fps", Seed: _cfg.seed); // adapt if your DTO differs
            await runner.RunAsync(brief, ctx, CancellationToken.None);
            Debug.Log($"[Runbook] Done. Artifacts: {ctx.RunFolder}");
        }

        private async Task RunForReview(string mode)
        {
            Debug.Log($"[Runbook] Starting review mode: {mode}");
            
            // Update config for review mode
            if (_cfg.review == null)
                _cfg.review = new ReviewConfig();
            
            _cfg.review.mode = mode;
            _cfg.review.failSoft = mode == "always-green";
            
            // Run review CLI
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"cd {Directory.GetCurrentDirectory()} && ./scripts/run-for-review.sh {mode}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            
            Debug.Log($"[Runbook] Review mode output: {output}");
            if (!string.IsNullOrEmpty(error))
                Debug.LogWarning($"[Runbook] Review mode errors: {error}");
            
            Debug.Log($"[Runbook] Review mode complete. Check Artifacts/review/ for results.");
        }

        private void CreatePresentationBundle()
        {
            Debug.Log("[Runbook] Creating presentation bundle...");
            
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"cd {Directory.GetCurrentDirectory()} && ./scripts/present-bundle.sh\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            Debug.Log($"[Runbook] Presentation bundle output: {output}");
            if (!string.IsNullOrEmpty(error))
                Debug.LogWarning($"[Runbook] Presentation bundle errors: {error}");
            
            Debug.Log("[Runbook] Presentation bundle created. Check Presentation/ folder.");
        }
    }
}
#endif