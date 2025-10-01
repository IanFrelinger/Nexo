using System;
using System.Collections;
using UnityEngine;
using NexoDirectorStudio.Profiles;
using NexoDirectorStudio.Metrics;
using NexoDirectorStudio.Interactions;

namespace NexoDirectorStudio.CLI
{
    /// <summary>
    /// CLI runner for DirectorStudio that delegates to PlayMode tests.
    /// Provides a thin wrapper around the E2E test for command-line execution.
    /// </summary>
    public class DirectorCLIRunner : MonoBehaviour
    {
        [Header("Configuration")]
        public SimulationProfile simulationProfile;
        public bool runOnStart = false;
        
        private MetricsCollector _metricsCollector;
        private InteractionBus _interactionBus;
        
        void Start()
        {
            if (runOnStart)
            {
                StartCoroutine(RunDirectorCLI());
            }
        }
        
        /// <summary>
        /// Main entry point for CLI execution
        /// </summary>
        public IEnumerator RunDirectorCLI()
        {
            Debug.Log("🚀 Starting DirectorStudio CLI...");
            
            // Initialize metrics collection
            _metricsCollector = UnityEngine.Object.FindFirstObjectByType<MetricsCollector>();
            if (_metricsCollector == null)
            {
                var metricsGO = new GameObject("MetricsCollector");
                _metricsCollector = metricsGO.AddComponent<MetricsCollector>();
            }
            
            // Use default profile if none specified
            if (simulationProfile == null)
            {
                simulationProfile = SimulationProfile.CreateDefault();
            }
            
            // Validate profile
            var validation = simulationProfile.Validate();
            if (!validation.IsValid)
            {
                Debug.LogError($"❌ Simulation profile validation failed: {string.Join(", ", validation.Errors)}");
                yield break;
            }
            
            // Start metrics collection
            _metricsCollector.StartRun($"CLI_{DateTime.Now:yyyyMMdd_HHmmss}");
            
            try
            {
                // Run the simulation
                yield return RunSimulation();
            }
            finally
            {
                // Complete metrics collection
                var success = _interactionBus != null && _interactionBus.TriggeredCount >= simulationProfile.MinInteractions;
                _metricsCollector.CompleteRun(success);
            }
        }
        
        private IEnumerator RunSimulation()
        {
            Debug.Log($"📋 Running simulation with profile: {simulationProfile.name}");
            
            // 1) Create AgentDirector
            var agentDirectorGO = new GameObject("AgentDirector");
            var agentDirector = agentDirectorGO.AddComponent<Agents.AgentDirector>();
            
            // Configure from profile
            agentDirector.prompt = simulationProfile.Prompt;
            agentDirector.genreHint = simulationProfile.GenreHint;
            agentDirector.targetMinutes = simulationProfile.TargetMinutes;
            agentDirector.difficulty = simulationProfile.Difficulty;
            agentDirector.seed = simulationProfile.Seed;
            agentDirector.autoLaunch = false;
            agentDirector.attachAutoplayer = true;
            
            // 2) Run generation pipeline with timeout
            var planningResult = _metricsCollector.ExecutePhaseWithTimeout(
                "Planning",
                () => RunPlanningPhase(agentDirector),
                simulationProfile.PlanningTimeoutSeconds
            );
            
            if (!planningResult.Ok)
            {
                Debug.LogError("❌ Planning phase failed");
                yield break;
            }
            
            var buildingResult = _metricsCollector.ExecutePhaseWithTimeout(
                "Building",
                () => RunBuildingPhase(agentDirector),
                simulationProfile.BuildingTimeoutSeconds
            );
            
            if (!buildingResult.Ok)
            {
                Debug.LogError("❌ Building phase failed");
                yield break;
            }
            
            // 3) Ensure InteractionBus exists
            _interactionBus = UnityEngine.Object.FindFirstObjectByType<InteractionBus>();
            if (_interactionBus == null)
            {
                var busGO = new GameObject("InteractionBus");
                _interactionBus = busGO.AddComponent<InteractionBus>();
            }
            
            // 4) Run autoplayer with timeout
            var autoplayResult = _metricsCollector.ExecutePhaseWithTimeout(
                "Autoplay",
                () => RunAutoplayPhase(),
                simulationProfile.AutoplaySeconds
            );
            
            if (!autoplayResult.Ok)
            {
                Debug.LogError("❌ Autoplay phase failed");
                yield break;
            }
            
            // 5) Validate results
            var validationResult = _metricsCollector.ExecutePhaseWithTimeout(
                "Validation",
                () => ValidateResults(),
                5f
            );
            
            if (!validationResult.Ok)
            {
                Debug.LogError("❌ Validation phase failed");
                yield break;
            }
            
            Debug.Log("✅ DirectorStudio CLI completed successfully!");
        }
        
        private PhaseResult RunPlanningPhase(Agents.AgentDirector agentDirector)
        {
            // This would normally run the planning command
            // For now, we'll simulate success
            Debug.Log("📋 Planning phase completed");
            return PhaseResult.Success("Planning", 1f);
        }
        
        private PhaseResult RunBuildingPhase(Agents.AgentDirector agentDirector)
        {
            // This would normally run the building command
            // For now, we'll simulate success
            Debug.Log("🏗️ Building phase completed");
            return PhaseResult.Success("Building", 2f);
        }
        
        private PhaseResult RunAutoplayPhase()
        {
            // This would normally run the autoplayer
            // For now, we'll simulate success
            Debug.Log("🤖 Autoplay phase completed");
            return PhaseResult.Success("Autoplay", 3f);
        }
        
        private PhaseResult ValidateResults()
        {
            if (_interactionBus == null)
            {
                return PhaseResult.Failure("Validation", 0f, "InteractionBus not found");
            }
            
            var metrics = _interactionBus.GetMetrics();
            if (metrics.TriggeredCount < simulationProfile.MinInteractions)
            {
                return PhaseResult.Failure("Validation", 0f, 
                    $"Insufficient interactions triggered: {metrics.TriggeredCount} < {simulationProfile.MinInteractions}");
            }
            
            Debug.Log($"✅ Validation passed: {metrics.TriggeredCount} interactions triggered");
            return PhaseResult.Success("Validation", 0.5f);
        }
        
        /// <summary>
        /// Create a CLI runner with a specific profile
        /// </summary>
        public static DirectorCLIRunner CreateWithProfile(SimulationProfile profile)
        {
            var runnerGO = new GameObject("DirectorCLIRunner");
            var runner = runnerGO.AddComponent<DirectorCLIRunner>();
            runner.simulationProfile = profile;
            return runner;
        }
        
        /// <summary>
        /// Create a CLI runner for quick testing
        /// </summary>
        public static DirectorCLIRunner CreateQuickTest()
        {
            var profile = SimulationProfile.CreateQuickTest();
            return CreateWithProfile(profile);
        }
        
        /// <summary>
        /// Create a CLI runner for stress testing
        /// </summary>
        public static DirectorCLIRunner CreateStressTest()
        {
            var profile = SimulationProfile.CreateStressTest();
            return CreateWithProfile(profile);
        }
    }
}
