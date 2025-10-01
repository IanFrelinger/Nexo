using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Profiles;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Header component for Director Studio window.
    /// </summary>
    public class DirectorStudioHeader : IDirectorStudioUIComponent
    {
        private readonly DirectorStudioUIState _state;

        public DirectorStudioHeader(DirectorStudioUIState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Draw()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Director Studio", EditorStyles.boldLabel);
            EditorGUILayout.FlexibleSpace();
            
            if (_state.IsGenerating)
            {
                EditorGUILayout.LabelField("Generating...", EditorStyles.miniLabel);
            }
            else if (!string.IsNullOrEmpty(_state.StatusMessage))
            {
                EditorGUILayout.LabelField(_state.StatusMessage, EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        public void OnEnable() { }
        public void OnDisable() { }
    }

    /// <summary>
    /// Tab navigation component for Director Studio window.
    /// </summary>
    public class DirectorStudioTabs : IDirectorStudioUIComponent
    {
        private readonly DirectorStudioUIState _state;

        public DirectorStudioTabs(DirectorStudioUIState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Draw()
        {
            _state.SelectedTab = GUILayout.Toolbar(_state.SelectedTab, DirectorStudioUIConfig.TabNames);
        }

        public void OnEnable() { }
        public void OnDisable() { }
    }

    /// <summary>
    /// Design tab component for Director Studio window.
    /// </summary>
    public class DirectorStudioDesignTab : IDirectorStudioUIComponent
    {
        private readonly DirectorStudioUIState _state;
        private readonly GenreProfileService _profileService;

        public DirectorStudioDesignTab(DirectorStudioUIState state, GenreProfileService profileService)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        }

        public void Draw()
        {
            EditorGUILayout.LabelField("Game Design Brief", EditorStyles.boldLabel);
            _state.DesignBrief = EditorGUILayout.TextArea(_state.DesignBrief, GUILayout.Height(100));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Genre", EditorStyles.boldLabel);
            var genres = _profileService.GetAllProfiles();
            var genreNames = genres.Select(g => g.Name).ToArray();
            var currentIndex = _state.SelectedGenre != null ? 
                Array.IndexOf(genreNames, _state.SelectedGenre.Name) : 0;
            
            var selectedIndex = EditorGUILayout.Popup("Select Genre", currentIndex, genreNames);
            if (selectedIndex >= 0 && selectedIndex < genres.Count)
            {
                _state.SelectedGenre = genres[selectedIndex];
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target Duration (minutes):", GUILayout.Width(150));
            // Duration would be set here
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Difficulty (1-5):", GUILayout.Width(150));
            // Difficulty would be set here
            EditorGUILayout.EndHorizontal();
        }

        public void OnEnable() { }
        public void OnDisable() { }
    }

    /// <summary>
    /// Plan tab component for Director Studio window.
    /// </summary>
    public class DirectorStudioPlanTab : IDirectorStudioUIComponent
    {
        private readonly DirectorStudioUIState _state;

        public DirectorStudioPlanTab(DirectorStudioUIState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Draw()
        {
            if (_state.CurrentGamePlan == null)
            {
                EditorGUILayout.HelpBox("No game plan generated yet. Click 'Generate Plan' in the Design tab.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Game Plan", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Genre: {_state.CurrentGamePlan.Genre}");
            EditorGUILayout.LabelField($"Duration: {_state.CurrentGamePlan.EstimatedDurationMinutes} minutes");
            EditorGUILayout.LabelField($"Core Mechanics: {string.Join(", ", _state.CurrentGamePlan.CoreMechanics)}");
            EditorGUILayout.LabelField($"Player Experience: {string.Join(", ", _state.CurrentGamePlan.PlayerExperience)}");

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Required Assets", EditorStyles.boldLabel);
            foreach (var asset in _state.CurrentGamePlan.RequiredAssets)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(asset.AssetType, GUILayout.Width(100));
                EditorGUILayout.LabelField(asset.Name);
                EditorGUILayout.LabelField(asset.IsRequired ? "Required" : "Optional", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        public void OnEnable() { }
        public void OnDisable() { }
    }

    /// <summary>
    /// Validation tab component for Director Studio window.
    /// </summary>
    public class DirectorStudioValidationTab : IDirectorStudioUIComponent
    {
        private readonly DirectorStudioUIState _state;

        public DirectorStudioValidationTab(DirectorStudioUIState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Draw()
        {
            if (_state.ValidationReport == null)
            {
                EditorGUILayout.HelpBox("No validation report available. Generate a game plan first.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Validation Report", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Report ID: {_state.ValidationReport.ReportId}");
            EditorGUILayout.LabelField($"Timestamp: {_state.ValidationReport.Timestamp}");

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Issues", EditorStyles.boldLabel);
            foreach (var issue in _state.ValidationReport.AllIssues)
            {
                var messageType = issue.Severity switch
                {
                    ValidationSeverity.Critical => MessageType.Error,
                    ValidationSeverity.Error => MessageType.Error,
                    ValidationSeverity.Warning => MessageType.Warning,
                    ValidationSeverity.Info => MessageType.Info,
                    _ => MessageType.Info
                };

                EditorGUILayout.HelpBox($"{issue.Severity}: {issue.Title} - {issue.Description}", messageType);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Suggestions", EditorStyles.boldLabel);
            foreach (var suggestion in _state.ValidationReport.AllSuggestions)
            {
                EditorGUILayout.HelpBox($"{suggestion.Priority}: {suggestion.Title} - {suggestion.Description}", MessageType.Info);
            }
        }

        public void OnEnable() { }
        public void OnDisable() { }
    }

    /// <summary>
    /// Fix tab component for Director Studio window.
    /// </summary>
    public class DirectorStudioFixTab : IDirectorStudioUIComponent
    {
        private readonly DirectorStudioUIState _state;

        public DirectorStudioFixTab(DirectorStudioUIState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Draw()
        {
            if (_state.AutoFixSuggestions == null || !_state.AutoFixSuggestions.Any())
            {
                EditorGUILayout.HelpBox("No auto-fix suggestions available. Generate a validation report first.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Auto-Fix Suggestions", EditorStyles.boldLabel);
            
            foreach (var suggestion in _state.AutoFixSuggestions)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(suggestion.Title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(suggestion.Description);
                EditorGUILayout.LabelField($"Priority: {suggestion.Priority}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Category: {suggestion.Category}", EditorStyles.miniLabel);
                
                if (GUILayout.Button("Apply Fix"))
                {
                    // Apply fix logic would go here
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        public void OnEnable() { }
        public void OnDisable() { }
    }

    /// <summary>
    /// Footer component for Director Studio window.
    /// </summary>
    public class DirectorStudioFooter : IDirectorStudioUIComponent
    {
        private readonly DirectorStudioUIState _state;
        private readonly IDirectorStudioOperations _operations;

        public DirectorStudioFooter(DirectorStudioUIState state, IDirectorStudioOperations operations)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _operations = operations ?? throw new ArgumentNullException(nameof(operations));
        }

        public void Draw()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Generate Plan") && !_state.IsGenerating)
            {
                _operations.GenerateGamePlanAsync();
            }
            
            if (GUILayout.Button("Validate Plan") && !_state.IsGenerating)
            {
                _operations.ValidateGamePlanAsync();
            }
            
            if (GUILayout.Button("Start Playtest") && _state.CurrentGamePlan != null)
            {
                _operations.StartPlaytest();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        public void OnEnable() { }
        public void OnDisable() { }
    }
}
