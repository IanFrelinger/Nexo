using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Command to plan a game slice from a design brief.
    /// </summary>
    public class PlanGameSliceCommand : IPlanGameSliceCommand
    {
        public async Task<GamePlan> ExecuteAsync(IPlanGameSliceCommand.Input input, CancellationToken cancellationToken)
        {
            // Generate FPS content based on the design brief
            await Task.Delay(100, cancellationToken); // Simulate async work
            
            var random = new System.Random(input.Brief.Seed);
            var mechanics = GenerateFPSMechanics(input.Brief, random);
            var assets = GenerateFPSAssets(input.Brief, random);
            var difficultyBeats = GenerateDifficultyProgression(input.Brief, random);
            var narrativeBeats = GenerateNarrativeBeats(input.Brief, random);
            
            return new GamePlan(
                Id: Guid.NewGuid().ToString(),
                SourceBrief: input.Brief,
                Genre: input.Brief.GenreHint,
                Description: input.Brief.Description,
                CoreMechanics: mechanics,
                PlayerExperience: GeneratePlayerExperience(input.Brief),
                EstimatedDurationMinutes: input.Brief.TargetDurationMinutes,
                DifficultyProgression: difficultyBeats,
                NarrativeBeats: narrativeBeats,
                RequiredAssets: assets,
                Seed: input.Brief.Seed,
                GeneratedAt: DateTimeOffset.UtcNow,
                Hash: Guid.NewGuid().ToString()
            );
        }

        private List<string> GenerateFPSMechanics(DesignBrief brief, System.Random random)
        {
            var mechanics = new List<string>();
            
            if (brief.GenreHint.ToLower().Contains("fps") || brief.Description.ToLower().Contains("shoot"))
            {
                mechanics.Add("Shooting");
                mechanics.Add("Aiming");
                mechanics.Add("Reloading");
            }
            
            if (brief.Description.ToLower().Contains("keycard") || brief.Description.ToLower().Contains("door"))
            {
                mechanics.Add("Keycard Collection");
                mechanics.Add("Door Unlocking");
            }
            
            if (brief.Description.ToLower().Contains("boss"))
            {
                mechanics.Add("Boss Combat");
                mechanics.Add("Pattern Recognition");
            }
            
            if (brief.Description.ToLower().Contains("vertical") || brief.Description.ToLower().Contains("lift"))
            {
                mechanics.Add("Vertical Movement");
                mechanics.Add("Elevator Usage");
            }
            
            mechanics.Add("Health Management");
            mechanics.Add("Ammo Conservation");
            
            return mechanics;
        }

        private List<AssetRequirement> GenerateFPSAssets(DesignBrief brief, System.Random random)
        {
            var assets = new List<AssetRequirement>();
            
            // For now, return empty list since AssetRequirement is not defined
            // In a real implementation, this would create actual asset requirements
            return assets;
        }

        private List<DifficultyBeat> GenerateDifficultyProgression(DesignBrief brief, System.Random random)
        {
            var beats = new List<DifficultyBeat>();
            var duration = brief.TargetDurationMinutes;
            
            // Early game - tutorial
            beats.Add(new DifficultyBeat(0f, 1, "Tutorial"));
            
            // Mid game - main combat
            beats.Add(new DifficultyBeat(2f, 2, "Main Combat"));
            
            // Late game - boss fight
            if (brief.Description.ToLower().Contains("boss"))
            {
                beats.Add(new DifficultyBeat(duration - 2f, 3, "Boss Fight"));
            }
            
            return beats;
        }

        private List<string> GenerateNarrativeBeats(DesignBrief brief, System.Random random)
        {
            var beats = new List<string>();
            
            beats.Add("Enter the facility");
            beats.Add("Find the first keycard");
            beats.Add("Unlock the first door");
            
            if (brief.Description.ToLower().Contains("boss"))
            {
                beats.Add("Discover the boss chamber");
                beats.Add("Defeat the final boss");
                beats.Add("Escape the facility");
            }
            
            return beats;
        }

        private List<string> GeneratePlayerExperience(DesignBrief brief)
        {
            var experiences = new List<string>();
            
            experiences.Add("Intense combat encounters");
            experiences.Add("Exploration and discovery");
            experiences.Add("Resource management");
            
            if (brief.Description.ToLower().Contains("retro"))
            {
                experiences.Add("Nostalgic retro gaming feel");
            }
            
            if (brief.Description.ToLower().Contains("vertical"))
            {
                experiences.Add("Vertical level design");
            }
            
            return experiences;
        }
    }
}