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
        private IDirectorStudioService _service;
        private GenreProfileService _profileService;
        private DirectorStudioUIState _uiState;
        private DirectorStudioOperations _operations;
        private List<IDirectorStudioUIComponent> _uiComponents;

        [MenuItem(DirectorStudioUIConfig.MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<DirectorStudioWindow>(DirectorStudioUIConfig.WindowTitle);
            window.minSize = DirectorStudioUIConfig.MinWindowSize;
            window.Show();
        }
        
        private void OnEnable()
        {
            InitializeServices();
            InitializeUI();
        }
        
        private void InitializeServices()
        {
            try
            {
                _service = new DirectorStudioServiceUnified();
                _profileService = _service.GetService<GenreProfileService>();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize services: {ex.Message}");
            }
        }

        private void InitializeUI()
        {
            _uiState = new DirectorStudioUIState();
            _operations = new DirectorStudioOperations(_uiState, _service, _profileService);
            
            _uiComponents = new List<IDirectorStudioUIComponent>
            {
                new DirectorStudioHeader(_uiState),
                new DirectorStudioTabs(_uiState),
                new DirectorStudioDesignTab(_uiState, _profileService),
                new DirectorStudioPlanTab(_uiState),
                new DirectorStudioValidationTab(_uiState),
                new DirectorStudioFixTab(_uiState),
                new DirectorStudioFooter(_uiState, _operations)
            };

            foreach (var component in _uiComponents)
            {
                component.OnEnable();
            }
        }

        private void OnGUI()
        {
            if (_service == null || _profileService == null)
            {
                EditorGUILayout.HelpBox("Failed to initialize services. Please check the console for errors.", MessageType.Error);
                return;
            }
            
            _uiState.ScrollPosition = EditorGUILayout.BeginScrollView(_uiState.ScrollPosition);

            // Draw header
            _uiComponents[0].Draw(); // Header
            EditorGUILayout.Space();
            
            // Draw tabs
            _uiComponents[1].Draw(); // Tabs
            EditorGUILayout.Space();
            
            // Draw content based on selected tab
            switch (_uiState.SelectedTab)
            {
                case 0: // Design
                    _uiComponents[2].Draw(); // Design Tab
                    break;
                case 1: // Plan
                    _uiComponents[3].Draw(); // Plan Tab
                    break;
                case 2: // Validation
                    _uiComponents[4].Draw(); // Validation Tab
                    break;
                case 3: // Fix
                    _uiComponents[5].Draw(); // Fix Tab
                    break;
            }
            
            EditorGUILayout.Space();
            
            // Draw footer
            _uiComponents[6].Draw(); // Footer

            EditorGUILayout.EndScrollView();
        }

        private void OnDisable()
        {
            if (_uiComponents != null)
            {
                foreach (var component in _uiComponents)
                {
                    component.OnDisable();
                }
            }

            _service?.Dispose();
        }
    }
}