using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using System.Threading.Tasks;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;

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

        

        async void Start()
        {
            if (autoLaunch)
            {
                await RunAsync(CancellationToken.None);
            }
        }

        public async Task RunAsync(CancellationToken ct)
        {
            var brief = new DesignBrief(
                Description: prompt,
                GenreHint: genreHint,
                TargetDurationMinutes: targetMinutes,
                DifficultyLevel: difficulty,
                Constraints: null,
                Seed: seed
            );

            var plan = await new PlanGameSliceCommand().ExecuteAsync(new IPlanGameSliceCommand.Input(brief), ct);
            onPlanned?.Invoke();

            var world = await new BuildWorldLayoutCommand().ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), ct);
            onWorldBuilt?.Invoke();

            // Game launcher removed for simplified batch flow

            var gameGo = new GameObject("DoomFPSGame (Agent)");
            var game = gameGo.AddComponent<NexoDirectorStudio.Game.DoomFPSGame>();
            game.gamePlan = plan;
            game.worldLayout = world;

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
            
        }
    }
}
