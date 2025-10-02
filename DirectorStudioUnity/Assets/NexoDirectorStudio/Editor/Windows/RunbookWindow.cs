#if UNITY_EDITOR
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Phases;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Validators;
using UnityEditor;
using UnityEngine;

namespace NexoDirectorStudio.EditorUI
{
    public sealed class RunbookWindow : EditorWindow
    {
        private int _seed = 1337;
        private string _artifacts = "Artifacts";
        private string _prompt = "micro room with one switch and one door";
        private Vector2 _scroll;

        [MenuItem("Nexo/Director Runbook")]
        public static void ShowWindow() => GetWindow<RunbookWindow>("Director Runbook");

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("Run context", EditorStyles.boldLabel);
            _seed = EditorGUILayout.IntField("Seed", _seed);
            _artifacts = EditorGUILayout.TextField("Artifacts Root", _artifacts);
            _prompt = EditorGUILayout.TextField("Prompt", _prompt);

            GUILayout.Space(8);
            if (GUILayout.Button("Run (Plan → Layout → Placement → Content → Validate)"))
                _ = RunPipeline();

            if (GUILayout.Button("Open Artifacts Folder"))
                EditorUtility.RevealInFinder(Path.Combine(_artifacts));

            EditorGUILayout.EndScrollView();
        }

        private async Task RunPipeline()
        {
            var svc = new DirectorStudioService();
            var ctx = new RunContext { Seed = _seed, ArtifactRoot = _artifacts, Checkpoints = new FileCheckpointStore() };
            ctx.Rng.Init(_seed);

            // construct phases using existing commands from service
            var runner = new PhaseRunner()
                .Add(new PlanPhase(svc.GetService<IPlanGameSliceCommand>()))
                .Add(new LayoutPhase(svc.GetService<IBuildWorldLayoutCommand>()))
                .Add(new PlacementPhase(svc.GetService<IPlaceInteractionsCommand>()))
                .Add(new ContentPhase(svc.GetService<ICreateContentBundleCommand>()))
                .Add(new ValidatePhase(svc.GetService<IValidator<ContentBundle>>()));

            var brief = new DesignBrief(Description: _prompt);
            await runner.RunAsync(brief, ctx, CancellationToken.None);
            Debug.Log("[Runbook] Pipeline completed.");
        }
    }
}
#endif
