using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Implementation of IApplyAutoFixesCommand.
    /// Applies auto-fixes to a game plan by executing the changes in a delta plan.
    /// </summary>
    public sealed class ApplyAutoFixesCommand : IApplyAutoFixesCommand
    {
        public async Task<GamePlan> ExecuteAsync(GamePlan gamePlan, DeltaPlan deltaPlan, CancellationToken cancellationToken = default)
        {
            if (gamePlan == null)
                throw new ArgumentNullException(nameof(gamePlan));
            
            if (deltaPlan == null)
                throw new ArgumentNullException(nameof(deltaPlan));
            
            if (deltaPlan.IsApplied)
                throw new InvalidOperationException("Delta plan has already been applied");
            
            // Create a copy of the original game plan
            var updatedGamePlan = CloneGamePlan(gamePlan);
            
            // Apply each change in the delta plan
            foreach (var change in deltaPlan.Changes)
            {
                updatedGamePlan = await ApplyChange(updatedGamePlan, change, cancellationToken);
            }
            
            // Update the game plan ID to reflect it's been modified
            updatedGamePlan.Id = $"{gamePlan.Id}_fixed_{DateTime.UtcNow:yyyyMMddHHmmss}";
            
            return updatedGamePlan;
        }
        
        private static GamePlan CloneGamePlan(GamePlan original)
        {
            return new GamePlan
            {
                Id = original.Id,
                SourceBrief = original.SourceBrief,
                Genre = original.Genre,
                Description = original.Description,
                CoreMechanics = original.CoreMechanics.ToArray(),
                PlayerExperience = original.PlayerExperience.ToArray(),
                EstimatedDurationMinutes = original.EstimatedDurationMinutes,
                DifficultyProgression = original.DifficultyProgression.ToArray(),
                NarrativeBeats = original.NarrativeBeats.ToArray(),
                RequiredAssets = original.RequiredAssets.ToArray(),
                Seed = original.Seed
            };
        }
        
        private static async Task<GamePlan> ApplyChange(GamePlan gamePlan, GamePlanChange change, CancellationToken cancellationToken)
        {
            return change.ChangeType switch
            {
                GamePlanChangeType.Add => await ApplyAddChange(gamePlan, change),
                GamePlanChangeType.Remove => await ApplyRemoveChange(gamePlan, change),
                GamePlanChangeType.Update => await ApplyUpdateChange(gamePlan, change),
                GamePlanChangeType.Replace => await ApplyReplaceChange(gamePlan, change),
                _ => throw new ArgumentException($"Unknown change type: {change.ChangeType}")
            };
        }
        
        private static async Task<GamePlan> ApplyAddChange(GamePlan gamePlan, GamePlanChange change)
        {
            var updatedGamePlan = CloneGamePlan(gamePlan);
            
            switch (change.PropertyPath)
            {
                case "CoreMechanics":
                    if (change.NewValue is string[] mechanics)
                    {
                        var existingMechanics = updatedGamePlan.CoreMechanics.ToList();
                        existingMechanics.AddRange(mechanics);
                        updatedGamePlan = updatedGamePlan with { CoreMechanics = existingMechanics.ToArray() };
                    }
                    break;
                
                case "RequiredAssets":
                    if (change.NewValue is AssetRequirement asset)
                    {
                        var existingAssets = updatedGamePlan.RequiredAssets.ToList();
                        existingAssets.Add(asset);
                        updatedGamePlan = updatedGamePlan with { RequiredAssets = existingAssets.ToArray() };
                    }
                    break;
                
                case "NarrativeBeats":
                    if (change.NewValue is string[] beats)
                    {
                        var existingBeats = updatedGamePlan.NarrativeBeats.ToList();
                        existingBeats.AddRange(beats);
                        updatedGamePlan = updatedGamePlan with { NarrativeBeats = existingBeats.ToArray() };
                    }
                    break;
                
                case "PlayerExperience":
                    if (change.NewValue is string[] experiences)
                    {
                        var existingExperiences = updatedGamePlan.PlayerExperience.ToList();
                        existingExperiences.AddRange(experiences);
                        updatedGamePlan = updatedGamePlan with { PlayerExperience = existingExperiences.ToArray() };
                    }
                    break;
            }
            
            return updatedGamePlan;
        }
        
        private static async Task<GamePlan> ApplyRemoveChange(GamePlan gamePlan, GamePlanChange change)
        {
            var updatedGamePlan = CloneGamePlan(gamePlan);
            
            switch (change.PropertyPath)
            {
                case "CoreMechanics":
                    if (change.OldValue is string[] mechanicsToRemove)
                    {
                        var existingMechanics = updatedGamePlan.CoreMechanics.ToList();
                        foreach (var mechanic in mechanicsToRemove)
                        {
                            existingMechanics.Remove(mechanic);
                        }
                        updatedGamePlan = updatedGamePlan with { CoreMechanics = existingMechanics.ToArray() };
                    }
                    break;
                
                case "RequiredAssets":
                    if (change.OldValue is AssetRequirement assetToRemove)
                    {
                        var existingAssets = updatedGamePlan.RequiredAssets.ToList();
                        existingAssets.RemoveAll(a => a.Name == assetToRemove.Name);
                        updatedGamePlan = updatedGamePlan with { RequiredAssets = existingAssets.ToArray() };
                    }
                    break;
                
                case "NarrativeBeats":
                    if (change.OldValue is string[] beatsToRemove)
                    {
                        var existingBeats = updatedGamePlan.NarrativeBeats.ToList();
                        foreach (var beat in beatsToRemove)
                        {
                            existingBeats.Remove(beat);
                        }
                        updatedGamePlan = updatedGamePlan with { NarrativeBeats = existingBeats.ToArray() };
                    }
                    break;
                
                case "PlayerExperience":
                    if (change.OldValue is string[] experiencesToRemove)
                    {
                        var existingExperiences = updatedGamePlan.PlayerExperience.ToList();
                        foreach (var experience in experiencesToRemove)
                        {
                            existingExperiences.Remove(experience);
                        }
                        updatedGamePlan = updatedGamePlan with { PlayerExperience = existingExperiences.ToArray() };
                    }
                    break;
            }
            
            return updatedGamePlan;
        }
        
        private static async Task<GamePlan> ApplyUpdateChange(GamePlan gamePlan, GamePlanChange change)
        {
            var updatedGamePlan = CloneGamePlan(gamePlan);
            
            switch (change.PropertyPath)
            {
                case "SourceBrief.DifficultyLevel":
                    if (change.NewValue is int difficultyLevel)
                    {
                        var updatedBrief = updatedGamePlan.SourceBrief with { DifficultyLevel = difficultyLevel };
                        updatedGamePlan = updatedGamePlan with { SourceBrief = updatedBrief };
                    }
                    break;
                
                case "EstimatedDurationMinutes":
                    if (change.NewValue is int duration)
                    {
                        updatedGamePlan = updatedGamePlan with { EstimatedDurationMinutes = duration };
                    }
                    break;
                
                case "Description":
                    if (change.NewValue is string description)
                    {
                        updatedGamePlan = updatedGamePlan with { Description = description };
                    }
                    break;
                
                case "Genre":
                    if (change.NewValue is string genre)
                    {
                        updatedGamePlan = updatedGamePlan with { Genre = genre };
                    }
                    break;
            }
            
            return updatedGamePlan;
        }
        
        private static async Task<GamePlan> ApplyReplaceChange(GamePlan gamePlan, GamePlanChange change)
        {
            var updatedGamePlan = CloneGamePlan(gamePlan);
            
            switch (change.PropertyPath)
            {
                case "CoreMechanics":
                    if (change.NewValue is string[] mechanics)
                    {
                        updatedGamePlan = updatedGamePlan with { CoreMechanics = mechanics };
                    }
                    break;
                
                case "RequiredAssets":
                    if (change.NewValue is AssetRequirement[] assets)
                    {
                        updatedGamePlan = updatedGamePlan with { RequiredAssets = assets };
                    }
                    break;
                
                case "NarrativeBeats":
                    if (change.NewValue is string[] beats)
                    {
                        updatedGamePlan = updatedGamePlan with { NarrativeBeats = beats };
                    }
                    break;
                
                case "PlayerExperience":
                    if (change.NewValue is string[] experiences)
                    {
                        updatedGamePlan = updatedGamePlan with { PlayerExperience = experiences };
                    }
                    break;
                
                case "DifficultyProgression":
                    if (change.NewValue is DifficultyBeat[] progression)
                    {
                        updatedGamePlan = updatedGamePlan with { DifficultyProgression = progression };
                    }
                    break;
            }
            
            return updatedGamePlan;
        }
    }
}
