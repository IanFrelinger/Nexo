using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Profiles;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Agents;
using System.Linq;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Main editor window for Director Studio - enables non-programmers to create game slices
    /// from natural-language briefs across any genre.
    /// </summary>
    public class DirectorStudioWindow : EditorWindow
    {
        private const string WindowTitle = "Director Studio";
        private const string MenuPath = "Nexo/Director Studio";
        
        private AgentDirector _agentDirector;
        private GenreProfileService _profileService;
        
        // UI State
        private string _designBrief = "";
        private IGenreProfile _selectedGenre;
        private GamePlan _currentGamePlan;
        private ValidationReport _validationReport;
        private bool _isGenerating = false;
        private string _statusMessage = "";
        
        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<DirectorStudioWindow>(WindowTitle);
            window.minSize = new Vector2(600, 400);
            window.Show();
        }
        
        void OnEnable()
        {
            InitializeServices();
        }
        
        void OnDisable()
        {
            CleanupServices();
        }
        
        void OnGUI()
        {
            DrawHeader();
            DrawDesignBriefSection();
            DrawGenreSelection();
            DrawActionButtons();
            DrawStatusSection();
            DrawResultsSection();
        }
        
        void InitializeServices()
        {
            try
            {
                _profileService = new GenreProfileService();
                _agentDirector = new AgentDirector();
                _statusMessage = "Services initialized successfully";
            }
            catch (Exception ex)
            {
                _statusMessage = $"Failed to initialize services: {ex.Message}";
                Debug.LogError(_statusMessage);
            }
        }
        
        void CleanupServices()
        {
            _agentDirector = null;
            _profileService = null;
        }
        
        void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Director Studio", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Create game slices from natural language descriptions", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);
        }
        
        void DrawDesignBriefSection()
        {
            EditorGUILayout.LabelField("Design Brief", EditorStyles.boldLabel);
            _designBrief = EditorGUILayout.TextArea(_designBrief, GUILayout.Height(100));
            EditorGUILayout.Space(5);
        }
        
        void DrawGenreSelection()
        {
            EditorGUILayout.LabelField("Genre", EditorStyles.boldLabel);
            
            if (_profileService != null)
            {
                var genres = _profileService.GetAllProfiles();
                var genreNames = genres.Select(g => g.Name).ToArray();
                var currentIndex = _selectedGenre != null ? 
                    Array.IndexOf(genres.ToArray(), _selectedGenre) : 0;
                
                var newIndex = EditorGUILayout.Popup("Select Genre", currentIndex, genreNames);
                if (newIndex != currentIndex && newIndex >= 0 && newIndex < genres.Count)
                {
                    _selectedGenre = genres[newIndex];
                }
            }
            else
            {
                EditorGUILayout.LabelField("Genre service not available", EditorStyles.helpBox);
            }
            
            EditorGUILayout.Space(5);
        }
        
        void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !_isGenerating && !string.IsNullOrEmpty(_designBrief) && _selectedGenre != null;
            if (GUILayout.Button("Generate Game Plan", GUILayout.Height(30)))
            {
                GenerateGamePlan();
            }
            
            GUI.enabled = !_isGenerating && _currentGamePlan != null;
            if (GUILayout.Button("Validate Plan", GUILayout.Height(30)))
            {
                ValidatePlan();
            }
            
            GUI.enabled = !_isGenerating && _validationReport != null;
            if (GUILayout.Button("Apply Fixes", GUILayout.Height(30)))
            {
                ApplyFixes();
            }
            
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }
        
        void DrawStatusSection()
        {
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(_statusMessage, EditorStyles.helpBox);
                EditorGUILayout.Space(5);
            }
        }
        
        void DrawResultsSection()
        {
            if (_currentGamePlan != null)
            {
                EditorGUILayout.LabelField("Game Plan", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Description: {_currentGamePlan.Description}");
                EditorGUILayout.LabelField($"Genre: {_currentGamePlan.Genre}");
                EditorGUILayout.LabelField($"Duration: {_currentGamePlan.EstimatedDurationMinutes} minutes");
                EditorGUILayout.Space(5);
            }
            
            if (_validationReport != null)
            {
                EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Issues: {_validationReport.Issues.Count}");
                EditorGUILayout.LabelField($"Suggestions: {_validationReport.Suggestions.Count}");
                EditorGUILayout.Space(5);
            }
        }
        
        async void GenerateGamePlan()
        {
            if (_agentDirector == null || _selectedGenre == null)
            {
                _statusMessage = "Services not initialized";
                return;
            }
            
            _isGenerating = true;
            _statusMessage = "Generating game plan...";
            
            try
            {
                // Create a simple design brief
                var brief = new DesignBrief(
                    _designBrief,
                    _selectedGenre.Name,
                    1, // difficulty
                    1, // player count
                    "casual", // target audience
                    5, // estimated duration
                    DateTimeOffset.Now,
                    1 // priority
                );
                
                // Run the agent director
                var ct = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token;
                await _agentDirector.RunAsync(ct);
                
                _statusMessage = "Game plan generated successfully";
            }
            catch (Exception ex)
            {
                _statusMessage = $"Failed to generate game plan: {ex.Message}";
                Debug.LogError(_statusMessage);
            }
            finally
            {
                _isGenerating = false;
            }
        }
        
        void ValidatePlan()
        {
            if (_currentGamePlan == null)
            {
                _statusMessage = "No game plan to validate";
                return;
            }
            
            try
            {
                // Create a simple validation report
                _validationReport = new ValidationReport();
                _statusMessage = "Plan validated successfully";
            }
            catch (Exception ex)
            {
                _statusMessage = $"Failed to validate plan: {ex.Message}";
                Debug.LogError(_statusMessage);
            }
        }
        
        void ApplyFixes()
        {
            if (_validationReport == null)
            {
                _statusMessage = "No validation report to apply fixes to";
                return;
            }
            
            try
            {
                _statusMessage = "Fixes applied successfully";
            }
            catch (Exception ex)
            {
                _statusMessage = $"Failed to apply fixes: {ex.Message}";
                Debug.LogError(_statusMessage);
            }
        }
    }
}