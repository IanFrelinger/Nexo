using UnityEngine;
using System;

namespace NexoDirectorStudio.Profiles
{
    /// <summary>
    /// Configuration profile for DirectorStudio simulation runs.
    /// Allows prompts, durations, and budgets to be data-driven.
    /// </summary>
    [CreateAssetMenu(menuName = "Director/Simulation Profile", fileName = "SimulationProfile")]
    public class SimulationProfile : ScriptableObject
    {
        [Header("Simulation Configuration")]
        [TextArea(3, 6)]
        public string Prompt = "short FPS room with a switch and a door";
        
        [Header("Timing & Budgets")]
        [Range(1, 60)]
        public int AutoplaySeconds = 10;
        
        [Range(1, 30)]
        public int MinInteractions = 1;
        
        [Range(1, 100)]
        public int MaxInteractions = 50;
        
        [Range(1, 30)]
        public int PlanningTimeoutSeconds = 5;
        
        [Range(1, 30)]
        public int BuildingTimeoutSeconds = 10;
        
        [Range(1, 30)]
        public int PlacementTimeoutSeconds = 8;
        
        [Range(1, 30)]
        public int ContentTimeoutSeconds = 5;
        
        [Header("Game Configuration")]
        public string GenreHint = "FPS";
        
        [Range(1, 60)]
        public int TargetMinutes = 5;
        
        [Range(1, 5)]
        public int Difficulty = 3;
        
        [Range(0, 999999)]
        public int Seed = 12345;
        
        [Header("Performance Settings")]
        [Range(30, 120)]
        public int TargetFrameRate = 60;
        
        public bool EnableVSync = false;
        
        [Range(0.1f, 2f)]
        public float TimeScale = 1f;
        
        [Header("Validation")]
        public bool RequireNavMesh = true;
        
        public bool RequireMainCamera = true;
        
        public bool RequireEventSystem = true;
        
        public bool RequireAutoplayer = true;
        
        [Header("Output Settings")]
        public bool GenerateArtifacts = true;
        
        public bool EnableDetailedMetrics = true;
        
        public bool EnableHeartbeat = true;
        
        [Range(0.1f, 5f)]
        public float HeartbeatInterval = 1f;
        
        [Header("Debug Settings")]
        public bool EnableDebugLogging = true;
        
        public bool PauseOnFailure = false;
        
        public bool ShowMetricsInGame = false;
        
        /// <summary>
        /// Create a default simulation profile
        /// </summary>
        public static SimulationProfile CreateDefault()
        {
            var profile = CreateInstance<SimulationProfile>();
            profile.name = "Default Simulation Profile";
            return profile;
        }
        
        /// <summary>
        /// Create a quick test profile
        /// </summary>
        public static SimulationProfile CreateQuickTest()
        {
            var profile = CreateInstance<SimulationProfile>();
            profile.name = "Quick Test Profile";
            profile.Prompt = "tiny FPS room with one enemy";
            profile.AutoplaySeconds = 5;
            profile.MinInteractions = 1;
            profile.TargetMinutes = 2;
            profile.Difficulty = 1;
            profile.PlanningTimeoutSeconds = 2;
            profile.BuildingTimeoutSeconds = 3;
            profile.PlacementTimeoutSeconds = 2;
            profile.ContentTimeoutSeconds = 1;
            return profile;
        }
        
        /// <summary>
        /// Create a stress test profile
        /// </summary>
        public static SimulationProfile CreateStressTest()
        {
            var profile = CreateInstance<SimulationProfile>();
            profile.name = "Stress Test Profile";
            profile.Prompt = "large FPS level with many enemies, weapons, and power-ups";
            profile.AutoplaySeconds = 30;
            profile.MinInteractions = 10;
            profile.MaxInteractions = 100;
            profile.TargetMinutes = 15;
            profile.Difficulty = 5;
            profile.PlanningTimeoutSeconds = 10;
            profile.BuildingTimeoutSeconds = 20;
            profile.PlacementTimeoutSeconds = 15;
            profile.ContentTimeoutSeconds = 10;
            return profile;
        }
        
        /// <summary>
        /// Validate the profile settings
        /// </summary>
        public ValidationResult Validate()
        {
            var errors = new System.Collections.Generic.List<string>();
            var warnings = new System.Collections.Generic.List<string>();
            
            if (string.IsNullOrEmpty(Prompt))
            {
                errors.Add("Prompt cannot be empty");
            }
            
            if (AutoplaySeconds <= 0)
            {
                errors.Add("AutoplaySeconds must be greater than 0");
            }
            
            if (MinInteractions < 0)
            {
                errors.Add("MinInteractions cannot be negative");
            }
            
            if (MaxInteractions < MinInteractions)
            {
                errors.Add("MaxInteractions must be greater than or equal to MinInteractions");
            }
            
            if (TargetMinutes <= 0)
            {
                errors.Add("TargetMinutes must be greater than 0");
            }
            
            if (Difficulty < 1 || Difficulty > 5)
            {
                errors.Add("Difficulty must be between 1 and 5");
            }
            
            if (TargetFrameRate < 30)
            {
                warnings.Add("TargetFrameRate below 30 may cause performance issues");
            }
            
            if (TimeScale <= 0)
            {
                errors.Add("TimeScale must be greater than 0");
            }
            
            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            };
        }
    }
    
    /// <summary>
    /// Result of profile validation
    /// </summary>
    [System.Serializable]
    public struct ValidationResult
    {
        public bool IsValid;
        public string[] Errors;
        public string[] Warnings;
    }
}
