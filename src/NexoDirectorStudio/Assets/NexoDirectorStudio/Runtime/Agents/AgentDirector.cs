using UnityEngine;
using UnityEngine.Events;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Adapters;

namespace NexoDirectorStudio.Agents
{
    /// <summary>
    /// Agent-first Director: turns a natural-language prompt into a generated, launched slice.
    /// </summary>
    public class AgentDirector : MonoBehaviour
    {
        [TextArea(4, 12)] public string prompt = "Doom-style FPS. 15 min. Intense combat, key gates. seed=666.";
        public bool autoLaunch = true;
        public bool attachAutoplayer = true;
        public string genreHint = "FPS";
        public int targetMinutes = 15;
        public int difficulty = 4;
        public int seed = 666;

        [Header("Events")] public UnityEvent onPlanned;
        public UnityEvent onWorldBuilt;
        public UnityEvent onLaunched;

        private DirectorStudioService _svc;

        async void Start()
        {
            if (autoLaunch)
            {
                await RunAsync(CancellationToken.None);
            }
        }

        public async Task RunAsync(CancellationToken ct)
        {
            _svc = new DirectorStudioService();

            // Optional: consult adapters for enrichment (LLM, etc.)
            var ollama = _svc.GetService<IOllamaAdapter>();
            // In a real setup, call ollama to expand prompt → constraints (kept stub/deterministic)

            var brief = new DesignBrief(
                Description: prompt,
                GenreHint: genreHint,
                TargetDurationMinutes: targetMinutes,
                DifficultyLevel: difficulty,
                Constraints: null,
                Seed: seed
            );

            var plan = await _svc.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), ct);
            onPlanned?.Invoke();

            var world = await _svc.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), ct);
            onWorldBuilt?.Invoke();

            var interactions = await _svc.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), ct);

            var content = await _svc.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), ct);

            // Launch runtime scene objects in Unity
            var launcherGo = new GameObject("DoomGameLauncher (Agent)");
            var launcher = launcherGo.AddComponent<NexoDirectorStudio.Game.DoomGameLauncher>();
            launcher.autoLaunch = false;

            // Inject data and start
            var gameGo = new GameObject("DoomFPSGame (Agent)");
            var game = gameGo.AddComponent<NexoDirectorStudio.Game.DoomFPSGame>();
            game.gamePlan = plan;
            game.worldLayout = world;
            game.interactionGraph = interactions;
            game.contentBundle = content;

            // Player + camera
            var player = new GameObject("Player");
            player.tag = "Player";
            player.AddComponent<CharacterController>();
            gameGo.transform.SetParent(player.transform, false);

            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.AddComponent<Camera>();

            if (attachAutoplayer)
            {
                player.AddComponent<AIAutoplayer>();
            }

            onLaunched?.Invoke();
        }

        void OnDestroy()
        {
            _svc?.Dispose();
        }
    }
}
