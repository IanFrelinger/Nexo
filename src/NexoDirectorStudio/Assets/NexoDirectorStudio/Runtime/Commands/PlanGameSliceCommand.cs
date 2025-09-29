using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Stub implementation of the plan game slice command.
    /// Returns a deterministic canned plan for testing purposes.
    /// </summary>
    public sealed class PlanGameSliceCommand : IPlanGameSliceCommand
    {
        public async ValueTask<GamePlan> ExecuteAsync(DesignBrief input, CancellationToken ct)
        {
            // TODO: Replace with real AI model integration in Phase 8
            // For now, return a deterministic canned plan based on the input
            
            await Task.Delay(100, ct); // Simulate processing time
            
            var random = new Random(input.Seed);
            
            // Generate deterministic plan based on brief
            var genre = DetectGenre(input);
            var mechanics = GenerateMechanics(genre, random);
            var experience = GeneratePlayerExperience(input, random);
            var assets = GenerateAssetRequirements(genre, random);
            
            var plan = new GamePlan
            {
                SourceBrief = input,
                Genre = genre,
                Description = $"A {genre} game slice: {input.Description}",
                CoreMechanics = mechanics,
                PlayerExperience = experience,
                EstimatedDurationMinutes = input.TargetDurationMinutes,
                DifficultyProgression = GenerateDifficultyProgression(input, random),
                NarrativeBeats = GenerateNarrativeBeats(input, random),
                RequiredAssets = assets,
                Seed = input.Seed,
                Hash = GeneratePlanHash(input, genre, mechanics)
            };
            
            return plan;
        }
        
        private static string DetectGenre(DesignBrief brief)
        {
            var description = brief.Description.ToLowerInvariant();
            var hint = brief.GenreHint?.ToLowerInvariant() ?? string.Empty;
            
            // Simple keyword-based genre detection
            if (hint.Contains("fps") || description.Contains("shoot") || description.Contains("gun"))
                return "FPS";
            if (hint.Contains("platformer") || description.Contains("jump") || description.Contains("platform"))
                return "Platformer";
            if (hint.Contains("rpg") || description.Contains("quest") || description.Contains("character"))
                return "RPG";
            
            // Default to Platformer for now
            return "Platformer";
        }
        
        private static IReadOnlyList<string> GenerateMechanics(string genre, Random random)
        {
            return genre switch
            {
                "FPS" => new[] { "Aim", "Shoot", "Reload", "Move", "Crouch" },
                "Platformer" => new[] { "Jump", "Run", "Wall Jump", "Double Jump", "Dash" },
                "RPG" => new[] { "Talk", "Inventory", "Quest", "Level Up", "Combat" },
                _ => new[] { "Move", "Interact", "Collect" }
            };
        }
        
        private static IReadOnlyList<string> GeneratePlayerExperience(DesignBrief brief, Random random)
        {
            var experiences = new List<string>();
            
            if (brief.DifficultyLevel >= 4)
                experiences.Add("Challenging");
            if (brief.DifficultyLevel <= 2)
                experiences.Add("Relaxing");
            
            experiences.Add("Engaging");
            experiences.Add("Rewarding");
            
            return experiences;
        }
        
        private static IReadOnlyList<AssetRequirement> GenerateAssetRequirements(string genre, Random random)
        {
            var requirements = new List<AssetRequirement>();
            
            // Common requirements
            requirements.Add(new AssetRequirement
            {
                AssetType = "Prefab",
                Name = "Player",
                Description = "Player character prefab",
                IsRequired = true,
                Priority = 5
            });
            
            // Genre-specific requirements
            switch (genre)
            {
                case "FPS":
                    requirements.Add(new AssetRequirement
                    {
                        AssetType = "Prefab",
                        Name = "Weapon",
                        Description = "Weapon prefab",
                        IsRequired = true,
                        Priority = 4
                    });
                    break;
                case "Platformer":
                    requirements.Add(new AssetRequirement
                    {
                        AssetType = "Prefab",
                        Name = "Platform",
                        Description = "Platform prefab",
                        IsRequired = true,
                        Priority = 4
                    });
                    break;
                case "RPG":
                    requirements.Add(new AssetRequirement
                    {
                        AssetType = "ScriptableObject",
                        Name = "Quest",
                        Description = "Quest data",
                        IsRequired = true,
                        Priority = 4
                    });
                    break;
            }
            
            return requirements;
        }
        
        private static IReadOnlyList<DifficultyBeat> GenerateDifficultyProgression(DesignBrief brief, Random random)
        {
            var beats = new List<DifficultyBeat>();
            var duration = brief.TargetDurationMinutes * 60; // Convert to seconds
            
            // Generate 3-5 difficulty beats
            var beatCount = 3 + random.Next(3);
            for (int i = 0; i < beatCount; i++)
            {
                var timeOffset = (float)(duration * i / (beatCount - 1));
                var difficulty = Math.Max(1, Math.Min(5, brief.DifficultyLevel + random.Next(-1, 2)));
                
                beats.Add(new DifficultyBeat
                {
                    TimeOffsetSeconds = timeOffset,
                    DifficultyLevel = difficulty,
                    Description = $"Difficulty spike at {timeOffset:F1}s"
                });
            }
            
            return beats;
        }
        
        private static IReadOnlyList<string> GenerateNarrativeBeats(DesignBrief brief, Random random)
        {
            var beats = new List<string>();
            
            beats.Add("Introduction");
            beats.Add("Rising Action");
            beats.Add("Climax");
            beats.Add("Resolution");
            
            return beats;
        }
        
        private static string GeneratePlanHash(DesignBrief brief, string genre, IReadOnlyList<string> mechanics)
        {
            // Simple hash for deterministic validation
            var content = $"{brief.Description}|{brief.Seed}|{genre}|{string.Join(",", mechanics)}";
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content));
        }
    }
}
