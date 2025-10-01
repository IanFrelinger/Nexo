using System;
using System.Collections.Generic;
using UnityEngine;

namespace NexoDirectorStudio.Metrics
{
    /// <summary>
    /// Result of a phase execution with success status, warnings, errors, and metrics.
    /// </summary>
    [System.Serializable]
    public struct PhaseResult
    {
        public bool Ok;
        public string[] Warnings;
        public string[] Errors;
        public Dictionary<string, object> Metrics;
        public float Duration;
        public string PhaseName;
        public DateTime Timestamp;
        
        public static PhaseResult Success(string phaseName, float duration, Dictionary<string, object> metrics = null)
        {
            return new PhaseResult
            {
                Ok = true,
                PhaseName = phaseName,
                Duration = duration,
                Metrics = metrics ?? new Dictionary<string, object>(),
                Timestamp = DateTime.Now,
                Warnings = new string[0],
                Errors = new string[0]
            };
        }
        
        public static PhaseResult Failure(string phaseName, float duration, string[] errors, string[] warnings = null)
        {
            return new PhaseResult
            {
                Ok = false,
                PhaseName = phaseName,
                Duration = duration,
                Errors = errors ?? new string[0],
                Warnings = warnings ?? new string[0],
                Metrics = new Dictionary<string, object>(),
                Timestamp = DateTime.Now
            };
        }
        
        public static PhaseResult Failure(string phaseName, float duration, string error, string[] warnings = null)
        {
            return Failure(phaseName, duration, new[] { error }, warnings);
        }
    }
    
    /// <summary>
    /// Comprehensive metrics for a DirectorStudio run
    /// </summary>
    [System.Serializable]
    public struct RunMetrics
    {
        public string RunId;
        public DateTime StartTime;
        public DateTime EndTime;
        public float TotalDuration;
        public PhaseResult[] PhaseResults;
        public InteractionMetrics InteractionMetrics;
        public PerformanceMetrics PerformanceMetrics;
        public bool Success;
        public string[] GlobalErrors;
        public string[] GlobalWarnings;
        
        public static RunMetrics Create(string runId)
        {
            return new RunMetrics
            {
                RunId = runId,
                StartTime = DateTime.Now,
                PhaseResults = new PhaseResult[0],
                GlobalErrors = new string[0],
                GlobalWarnings = new string[0]
            };
        }
        
        public void Complete(bool success, string[] errors = null, string[] warnings = null)
        {
            EndTime = DateTime.Now;
            TotalDuration = (float)(EndTime - StartTime).TotalSeconds;
            Success = success;
            GlobalErrors = errors ?? new string[0];
            GlobalWarnings = warnings ?? new string[0];
        }
    }
    
    /// <summary>
    /// Performance metrics for a DirectorStudio run
    /// </summary>
    [System.Serializable]
    public struct PerformanceMetrics
    {
        public float AverageFPS;
        public float MinFPS;
        public float MaxFPS;
        public int FrameCount;
        public float MemoryUsageMB;
        public int ObjectsCreated;
        public int ObjectsDestroyed;
        public float NavMeshBuildTime;
        public float SceneLoadTime;
        
        public static PerformanceMetrics Create()
        {
            return new PerformanceMetrics
            {
                AverageFPS = 0f,
                MinFPS = float.MaxValue,
                MaxFPS = 0f,
                FrameCount = 0,
                MemoryUsageMB = 0f,
                ObjectsCreated = 0,
                ObjectsDestroyed = 0,
                NavMeshBuildTime = 0f,
                SceneLoadTime = 0f
            };
        }
    }
}
