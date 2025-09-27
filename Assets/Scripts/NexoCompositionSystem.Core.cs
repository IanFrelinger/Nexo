using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Core functionality for NexoCompositionSystem.
    /// </summary>
    public partial class NexoCompositionSystem
    {
        /// <summary>
        /// Initializes the composition system
        /// </summary>
        private void InitializeCompositionSystem()
        {
            Debug.Log("🎯 Initializing Nexo Composition System...");
            
            try
            {
                // Initialize Nexo Agent
                _nexoAgent = new Nexo.Agent.Implementations.AtlasAgent(
                    new Nexo.Agent.Implementations.SimplePlanner(),
                    new Nexo.Agent.Implementations.ToolBroker(),
                    new Nexo.Agent.Implementations.PipelineToolFactory()
                );
                
                // Set up UI
                if (startCompositionButton != null)
                    startCompositionButton.onClick.AddListener(() => StartCoroutine(ComposeAllComponents()));
                
                if (runTestsButton != null)
                    runTestsButton.onClick.AddListener(RunTests);
                
                UpdateCompositionStatus("Nexo Composition System Ready");
                Debug.Log("✅ Nexo Composition System initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to initialize composition system: {ex.Message}");
            }
        }

        /// <summary>
        /// Main composition coroutine
        /// </summary>
        public IEnumerator ComposeAllComponents()
        {
            if (_isComposing) yield break;
            
            _isComposing = true;
            _compositionResults.Clear();
            
            Debug.Log("🚀 Starting component composition...");
            UpdateCompositionStatus("Starting Component Composition");
            
            try
            {
                // Step 1: Load generated scripts
                yield return StartCoroutine(LoadGeneratedScripts());
                
                // Step 2: Load generated assets
                yield return StartCoroutine(LoadGeneratedAssets());
                
                // Step 3: Create game objects and components
                yield return StartCoroutine(CreateGameObjects());
                
                // Step 4: Compose components together
                yield return StartCoroutine(ComposeComponents());
                
                // Step 5: Validate composition
                if (enableValidation)
                {
                    yield return StartCoroutine(ValidateComposition());
                }
                
                // Step 6: Finalize composition
                yield return StartCoroutine(FinalizeComposition());
                
                Debug.Log("✅ Component composition completed successfully!");
                UpdateCompositionStatus("Composition Completed Successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Composition failed: {ex.Message}");
                UpdateCompositionStatus($"Composition Failed: {ex.Message}");
            }
            finally
            {
                _isComposing = false;
            }
        }

        /// <summary>
        /// Updates composition status
        /// </summary>
        private void UpdateCompositionStatus(string status)
        {
            if (compositionStatusText != null)
                compositionStatusText.text = status;
            
            Debug.Log($"🎯 Composition Status: {status}");
        }

        /// <summary>
        /// Updates composition progress
        /// </summary>
        private void UpdateCompositionProgress(float progress)
        {
            if (compositionProgressBar != null)
                compositionProgressBar.value = progress;
        }

        // Public methods for external access
        public bool IsComposing => _isComposing;
        public List<CompositionResult> GetCompositionResults => _compositionResults;
        public List<GameObject> GetComposedObjects => composedObjects;
    }
}
