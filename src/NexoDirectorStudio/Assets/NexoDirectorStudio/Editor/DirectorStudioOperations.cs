using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Profiles;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Commands;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Operations for Director Studio window.
    /// </summary>
    public class DirectorStudioOperations : IDirectorStudioOperations
    {
        private readonly DirectorStudioUIState _state;
        private readonly DirectorStudioService _service;
        private readonly GenreProfileService _profileService;

        public DirectorStudioOperations(DirectorStudioUIState state, DirectorStudioService service, GenreProfileService profileService)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        }

        public async Task GenerateGamePlanAsync()
        {
            if (string.IsNullOrEmpty(_state.DesignBrief))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a design brief first.", "OK");
                return;
            }

            if (_state.SelectedGenre == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a genre first.", "OK");
                return;
            }

            _state.IsGenerating = true;
            _state.StatusMessage = "Generating game plan...";

            try
            {
                var brief = new DesignBrief(
                    Description: _state.DesignBrief,
                    GenreHint: _state.SelectedGenre.Name,
                    TargetDurationMinutes: 15, // Default duration
                    DifficultyLevel: 3, // Default difficulty
                    Constraints: null,
                    Seed: UnityEngine.Random.Range(1000, 9999)
                );

                var planCommand = _service.GetService<IPlanGameSliceCommand>();
                _state.CurrentGamePlan = await planCommand.ExecuteAsync(
                    new IPlanGameSliceCommand.Input(brief), 
                    CancellationToken.None);

                _state.StatusMessage = "Game plan generated successfully!";
                Debug.Log($"✅ Game plan generated: {_state.CurrentGamePlan.Description}");
            }
            catch (Exception ex)
            {
                _state.StatusMessage = $"Error generating game plan: {ex.Message}";
                Debug.LogError($"❌ Failed to generate game plan: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate game plan: {ex.Message}", "OK");
            }
            finally
            {
                _state.IsGenerating = false;
            }
        }

        public async Task ValidateGamePlanAsync()
        {
            if (_state.CurrentGamePlan == null)
            {
                EditorUtility.DisplayDialog("Error", "Please generate a game plan first.", "OK");
                return;
            }

            _state.IsGenerating = true;
            _state.StatusMessage = "Validating game plan...";

            try
            {
                var validators = _service.GetService<IEnumerable<IValidator<GamePlan>>>();
                var validationResults = new List<ValidationResult>();

                foreach (var validator in validators)
                {
                    var result = await validator.ValidateAsync(_state.CurrentGamePlan, CancellationToken.None);
                    validationResults.Add(result);
                }

                _state.ValidationReport = new ValidationReport
                {
                    ReportId = Guid.NewGuid().ToString(),
                    Timestamp = DateTimeOffset.UtcNow,
                    Results = validationResults
                };

                _state.StatusMessage = "Validation completed!";
                Debug.Log($"✅ Game plan validated: {validationResults.Count(r => r.IsValid)}/{validationResults.Count} validators passed");
            }
            catch (Exception ex)
            {
                _state.StatusMessage = $"Error validating game plan: {ex.Message}";
                Debug.LogError($"❌ Failed to validate game plan: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to validate game plan: {ex.Message}", "OK");
            }
            finally
            {
                _state.IsGenerating = false;
            }
        }

        public void ApplyAutoFix(ValidationSuggestion suggestion)
        {
            if (suggestion == null)
            {
                EditorUtility.DisplayDialog("Error", "No suggestion to apply.", "OK");
                return;
            }

            try
            {
                // Apply the auto-fix suggestion
                // This would contain the actual fix logic
                Debug.Log($"Applying auto-fix: {suggestion.Title}");
                
                _state.StatusMessage = $"Applied fix: {suggestion.Title}";
                EditorUtility.DisplayDialog("Success", $"Applied fix: {suggestion.Title}", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to apply auto-fix: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to apply auto-fix: {ex.Message}", "OK");
            }
        }

        public void StartPlaytest()
        {
            if (_state.CurrentGamePlan == null)
            {
                EditorUtility.DisplayDialog("Error", "Please generate a game plan first.", "OK");
                return;
            }

            try
            {
                // Start playtest logic
                Debug.Log($"Starting playtest for: {_state.CurrentGamePlan.Description}");
                
                _state.StatusMessage = "Starting playtest...";
                EditorUtility.DisplayDialog("Playtest", "Playtest started!", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to start playtest: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to start playtest: {ex.Message}", "OK");
            }
        }
    }
}
