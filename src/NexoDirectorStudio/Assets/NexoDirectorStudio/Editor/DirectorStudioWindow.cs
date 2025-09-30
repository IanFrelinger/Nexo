using UnityEditor;
using UnityEngine;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Profiles;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Orchestration;
using System.Collections.Generic;
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
        
        private DirectorStudioService _service;
        private GenreProfileService _profileService;
        
        // UI State
        private string _designBrief = "";
        private IGenreProfile _selectedGenre;
        private GamePlan _currentGamePlan;
        private ValidationReport _validationReport;
        private bool _isGenerating = false;
        private string _statusMessage = "";
        
        // UI Layout
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Design", "Plan", "Validation", "Fix" };
        
        // Auto-fix state
        private List<ValidationSuggestion> _autoFixSuggestions = new();
        private bool _showAutoFixPreview = false;
        private string _autoFixPreview = "";
        
        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<DirectorStudioWindow>(WindowTitle);
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            InitializeServices();
        }
        
        private void InitializeServices()
        {
            try
            {
                _service = new DirectorStudioService();
                _profileService = _service.GetService<GenreProfileService>();
                _profileService.InitializeProfiles();
                
                // Auto-select first genre as default
                var profiles = _profileService.GetAllProfiles();
                if (profiles.Count > 0)
                {
                    _selectedGenre = profiles[0];
                }
                
                _statusMessage = "Director Studio initialized successfully";
            }
            catch (System.Exception ex)
            {
                _statusMessage = $"Initialization failed: {ex.Message}";
                Debug.LogError($"Director Studio initialization error: {ex}");
            }
        }
        
        private void OnGUI()
        {
            if (_service == null)
            {
                GUILayout.Label("Initializing Director Studio...", EditorStyles.helpBox);
                if (GUILayout.Button("Retry Initialization"))
                {
                    InitializeServices();
                }
                return;
            }
            
            DrawHeader();
            DrawTabs();
            DrawContent();
            DrawFooter();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Director Studio", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(_statusMessage, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawTabs()
        {
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            EditorGUILayout.Space();
        }
        
        private void DrawContent()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            switch (_selectedTab)
            {
                case 0:
                    DrawDesignTab();
                    break;
                case 1:
                    DrawPlanTab();
                    break;
                case 2:
                    DrawValidationTab();
                    break;
                case 3:
                    DrawFixTab();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawDesignTab()
        {
            EditorGUILayout.LabelField("Design Brief", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Design brief input
            EditorGUILayout.LabelField("Describe your game slice:", EditorStyles.label);
            _designBrief = EditorGUILayout.TextArea(_designBrief, GUILayout.Height(100));
            
            EditorGUILayout.Space();
            
            // Genre selection
            EditorGUILayout.LabelField("Genre Selection:", EditorStyles.label);
            var profiles = _profileService.GetAllProfiles();
            var profileNames = profiles.Select(p => p.Name).ToArray();
            var currentIndex = profiles.ToList().IndexOf(_selectedGenre);
            var newIndex = EditorGUILayout.Popup("Genre", currentIndex, profileNames);
            
            if (newIndex != currentIndex && newIndex >= 0)
            {
                _selectedGenre = profiles[newIndex];
            }
            
            EditorGUILayout.Space();
            
            // Genre info
            if (_selectedGenre != null)
            {
                EditorGUILayout.HelpBox($"Selected: {_selectedGenre.Name}\n{_selectedGenre.Description}", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            
            // Generate button
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_designBrief) || _selectedGenre == null || _isGenerating);
            if (GUILayout.Button(_isGenerating ? "Generating..." : "Generate Game Plan", GUILayout.Height(30)))
            {
                GenerateGamePlan();
            }
            EditorGUI.EndDisabledGroup();
        }
        
        private void DrawPlanTab()
        {
            if (_currentGamePlan == null)
            {
                EditorGUILayout.HelpBox("No game plan generated yet. Use the Design tab to create a plan.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField("Game Plan", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Plan details
            EditorGUILayout.LabelField("Plan ID:", _currentGamePlan.Id);
            EditorGUILayout.LabelField("Genre:", _currentGamePlan.Genre);
            EditorGUILayout.LabelField("Description:", _currentGamePlan.Description);
            EditorGUILayout.LabelField("Duration:", $"{_currentGamePlan.EstimatedDurationMinutes} minutes");
            
            EditorGUILayout.Space();
            
            // Core mechanics
            EditorGUILayout.LabelField("Core Mechanics:", EditorStyles.boldLabel);
            foreach (var mechanic in _currentGamePlan.CoreMechanics)
            {
                EditorGUILayout.LabelField($"• {mechanic}");
            }
            
            EditorGUILayout.Space();
            
            // Player experience
            EditorGUILayout.LabelField("Player Experience:", EditorStyles.boldLabel);
            foreach (var experience in _currentGamePlan.PlayerExperience)
            {
                EditorGUILayout.LabelField($"• {experience}");
            }
            
            EditorGUILayout.Space();
            
            // Required assets
            EditorGUILayout.LabelField("Required Assets:", EditorStyles.boldLabel);
            foreach (var asset in _currentGamePlan.RequiredAssets)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"• {asset.Name} ({asset.AssetType})");
                EditorGUILayout.LabelField($"Priority: {asset.Priority}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void DrawValidationTab()
        {
            if (_validationReport == null)
            {
                EditorGUILayout.HelpBox("No validation report available. Generate a game plan first.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField("Validation Report", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Overall status
            var statusColor = _validationReport.Status switch
            {
                ValidationStatus.Pass => Color.green,
                ValidationStatus.Warning => Color.yellow,
                ValidationStatus.Fail => Color.red,
                _ => Color.gray
            };
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel);
            var originalColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(_validationReport.Status.ToString(), EditorStyles.boldLabel);
            GUI.color = originalColor;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField("Overall Score:", $"{_validationReport.OverallScore}/100");
            EditorGUILayout.LabelField("Playtest Allowed:", _validationReport.IsPlaytestAllowed ? "Yes" : "No");
            
            EditorGUILayout.Space();
            
            // Issues
            if (_validationReport.AllIssues.Count > 0)
            {
                EditorGUILayout.LabelField("Issues:", EditorStyles.boldLabel);
                foreach (var issue in _validationReport.AllIssues)
                {
                    var issueColor = issue.Severity switch
                    {
                        ValidationSeverity.Critical => Color.red,
                        ValidationSeverity.Error => Color.red,
                        ValidationSeverity.Warning => Color.yellow,
                        _ => Color.gray
                    };
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    originalColor = GUI.color;
                    GUI.color = issueColor;
                    EditorGUILayout.LabelField($"{issue.Severity}: {issue.Title}", EditorStyles.boldLabel);
                    GUI.color = originalColor;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.LabelField(issue.Description);
                    if (!string.IsNullOrEmpty(issue.SuggestedFix))
                    {
                        EditorGUILayout.LabelField($"Suggested Fix: {issue.SuggestedFix}", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            
            EditorGUILayout.Space();
            
            // Suggestions
            if (_validationReport.AllSuggestions.Count > 0)
            {
                EditorGUILayout.LabelField("Suggestions:", EditorStyles.boldLabel);
                foreach (var suggestion in _validationReport.AllSuggestions)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"{suggestion.Title} (Priority: {suggestion.Priority})", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(suggestion.Description);
                    EditorGUILayout.LabelField($"Effort: {suggestion.Effort}", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                }
            }
        }
        
        private void DrawFixTab()
        {
            if (_validationReport == null)
            {
                EditorGUILayout.HelpBox("No validation report available. Generate a game plan first.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField("Auto-Fix Workflow", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Auto-fix suggestions
            if (_autoFixSuggestions.Count == 0)
            {
                EditorGUILayout.HelpBox("No auto-fix suggestions available.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField("Available Fixes:", EditorStyles.boldLabel);
            foreach (var suggestion in _autoFixSuggestions)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{suggestion.Title} (Priority: {suggestion.Priority})", EditorStyles.boldLabel);
                if (GUILayout.Button("Apply", GUILayout.Width(60)))
                {
                    ApplyAutoFix(suggestion);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField(suggestion.Description);
                EditorGUILayout.LabelField($"Effort: {suggestion.Effort}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space();
            
            // Preview toggle
            _showAutoFixPreview = EditorGUILayout.Toggle("Show Auto-Fix Preview", _showAutoFixPreview);
            
            if (_showAutoFixPreview && !string.IsNullOrEmpty(_autoFixPreview))
            {
                EditorGUILayout.LabelField("Preview:", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(_autoFixPreview, GUILayout.Height(100));
            }
        }
        
        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // Playtest button
            EditorGUI.BeginDisabledGroup(_validationReport == null || !_validationReport.IsPlaytestAllowed);
            if (GUILayout.Button("Playtest", GUILayout.Width(80)))
            {
                StartPlaytest();
            }
            EditorGUI.EndDisabledGroup();
            
            GUILayout.FlexibleSpace();
            
            // Status
            if (_isGenerating)
            {
                GUILayout.Label("Generating...", EditorStyles.miniLabel);
            }
            else if (_validationReport != null)
            {
                var statusText = _validationReport.IsPlaytestAllowed ? "Ready for Playtest" : "Issues Found";
                GUILayout.Label(statusText, EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private async void GenerateGamePlan()
        {
            if (string.IsNullOrEmpty(_designBrief) || _selectedGenre == null)
                return;
            
            _isGenerating = true;
            _statusMessage = "Generating game plan...";
            
            try
            {
                // Create design brief
                var brief = new DesignBrief
                {
                    Description = _designBrief,
                    GenreHint = _selectedGenre.Name,
                    TargetDurationMinutes = 5, // Default duration
                    DifficultyLevel = 3, // Default difficulty
                    Seed = System.DateTime.Now.Millisecond
                };
                
                // Generate game plan
                var planCommand = _service.GetService<IPlanGameSliceCommand>();
                _currentGamePlan = await planCommand.ExecuteAsync(brief, System.Threading.CancellationToken.None);
                
                // Validate the plan
                await ValidateGamePlan();
                
                _statusMessage = "Game plan generated successfully";
            }
            catch (System.Exception ex)
            {
                _statusMessage = $"Generation failed: {ex.Message}";
                Debug.LogError($"Game plan generation error: {ex}");
            }
            finally
            {
                _isGenerating = false;
            }
        }
        
        private async System.Threading.Tasks.Task ValidateGamePlan()
        {
            if (_currentGamePlan == null)
                return;
            
            try
            {
                // Get validators
                var playabilityValidator = _service.GetService<IValidator<GamePlan>>();
                var mechanicsValidator = _service.GetService<IValidator<GamePlan>>();
                
                // Run validation
                var playabilityResult = await playabilityValidator.ValidateAsync(_currentGamePlan, System.Threading.CancellationToken.None);
                var mechanicsResult = await mechanicsValidator.ValidateAsync(_currentGamePlan, System.Threading.CancellationToken.None);
                
                // Create validation report
                _validationReport = ValidationReport.Create(new[] { playabilityResult, mechanicsResult });
                
                // Generate auto-fix suggestions
                _autoFixSuggestions = _validationReport.AllSuggestions.ToList();
                
                // Generate preview
                _autoFixPreview = GenerateAutoFixPreview();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Validation error: {ex}");
            }
        }
        
        private void ApplyAutoFix(ValidationSuggestion suggestion)
        {
            // TODO: Implement auto-fix application
            _statusMessage = $"Applied fix: {suggestion.Title}";
        }
        
        private string GenerateAutoFixPreview()
        {
            if (_autoFixSuggestions.Count == 0)
                return "No auto-fix suggestions available.";
            
            var preview = "Auto-Fix Preview:\n\n";
            foreach (var suggestion in _autoFixSuggestions)
            {
                preview += $"• {suggestion.Title}\n";
                preview += $"  {suggestion.Description}\n";
                preview += $"  Priority: {suggestion.Priority}, Effort: {suggestion.Effort}\n\n";
            }
            
            return preview;
        }
        
        private void StartPlaytest()
        {
            if (_validationReport == null || !_validationReport.IsPlaytestAllowed)
                return;
            
            _statusMessage = "Starting playtest...";
            // TODO: Implement playtest functionality
        }
        
        private void OnDisable()
        {
            _service?.Dispose();
        }
    }
}