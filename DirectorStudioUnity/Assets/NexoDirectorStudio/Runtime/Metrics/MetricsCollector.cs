using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NexoDirectorStudio.Interactions;

namespace NexoDirectorStudio.Metrics
{
    /// <summary>
    /// Collects and manages metrics for DirectorStudio runs.
    /// Provides timeouts, heartbeats, and JSON artifact generation.
    /// </summary>
    public class MetricsCollector : MonoBehaviour
    {
        public static MetricsCollector Instance { get; private set; }
        
        [Header("Configuration")]
        public float HeartbeatInterval = 1f;
        public float DefaultPhaseTimeout = 10f;
        public bool EnableDetailedMetrics = true;
        
        [Header("Current Run")]
        public RunMetrics CurrentRun;
        public bool IsRunning = false;
        
        private float _lastHeartbeat;
        private readonly List<PhaseResult> _phaseResults = new();
        private PerformanceMetrics _performanceMetrics;
        private int _frameCount;
        private float _fpsSum;
        private float _minFps = float.MaxValue;
        private float _maxFps;
        
        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            _performanceMetrics = PerformanceMetrics.Create();
        }
        
        void Update()
        {
            if (IsRunning)
            {
                UpdatePerformanceMetrics();
                CheckHeartbeat();
            }
        }
        
        /// <summary>
        /// Start a new metrics collection run
        /// </summary>
        public void StartRun(string runId = null)
        {
            runId ??= Guid.NewGuid().ToString();
            CurrentRun = RunMetrics.Create(runId);
            IsRunning = true;
            _phaseResults.Clear();
            _performanceMetrics = PerformanceMetrics.Create();
            
            Debug.Log($"📊 Started metrics collection for run: {runId}");
        }
        
        /// <summary>
        /// Complete the current run
        /// </summary>
        public void CompleteRun(bool success, string[] errors = null, string[] warnings = null)
        {
            if (!IsRunning) return;
            
            // Get interaction metrics
            var interactionBus = Object.FindFirstObjectByType<InteractionBus>();
            if (interactionBus != null)
            {
                CurrentRun.InteractionMetrics = interactionBus.GetMetrics();
            }
            
            // Complete the run
            CurrentRun.Complete(success, errors, warnings);
            CurrentRun.PhaseResults = _phaseResults.ToArray();
            CurrentRun.PerformanceMetrics = _performanceMetrics;
            
            IsRunning = false;
            
            // Generate artifacts
            GenerateArtifacts();
            
            Debug.Log($"📊 Completed metrics collection. Success: {success}, Duration: {CurrentRun.TotalDuration:F2}s");
        }
        
        /// <summary>
        /// Record a phase result
        /// </summary>
        public void RecordPhase(PhaseResult phaseResult)
        {
            _phaseResults.Add(phaseResult);
            
            var status = phaseResult.Ok ? "✅" : "❌";
            Debug.Log($"{status} Phase '{phaseResult.PhaseName}' completed in {phaseResult.Duration:F2}s");
            
            if (phaseResult.Errors.Length > 0)
            {
                foreach (var error in phaseResult.Errors)
                {
                    Debug.LogError($"❌ Phase '{phaseResult.PhaseName}' error: {error}");
                }
            }
            
            if (phaseResult.Warnings.Length > 0)
            {
                foreach (var warning in phaseResult.Warnings)
                {
                    Debug.LogWarning($"⚠️ Phase '{phaseResult.PhaseName}' warning: {warning}");
                }
            }
        }
        
        /// <summary>
        /// Execute a phase with timeout protection
        /// </summary>
        public PhaseResult ExecutePhaseWithTimeout(string phaseName, System.Func<PhaseResult> phaseAction, float timeoutSeconds = -1)
        {
            if (timeoutSeconds < 0) timeoutSeconds = DefaultPhaseTimeout;
            
            var startTime = Time.realtimeSinceStartup;
            var timeoutTime = startTime + timeoutSeconds;
            
            try
            {
                // Execute the phase
                var result = phaseAction();
                
                // Check if it completed within timeout
                var actualDuration = Time.realtimeSinceStartup - startTime;
                if (actualDuration > timeoutSeconds)
                {
                    var timeoutError = $"Phase '{phaseName}' exceeded timeout of {timeoutSeconds}s (took {actualDuration:F2}s)";
                    result = PhaseResult.Failure(phaseName, actualDuration, timeoutError);
                }
                
                RecordPhase(result);
                return result;
            }
            catch (Exception ex)
            {
                var duration = Time.realtimeSinceStartup - startTime;
                var result = PhaseResult.Failure(phaseName, duration, ex.Message);
                RecordPhase(result);
                return result;
            }
        }
        
        /// <summary>
        /// Get current run summary
        /// </summary>
        public RunSummary GetRunSummary()
        {
            return new RunSummary
            {
                RunId = CurrentRun.RunId,
                IsRunning = IsRunning,
                Duration = IsRunning ? Time.realtimeSinceStartup - (float)CurrentRun.StartTime.TimeOfDay.TotalSeconds : CurrentRun.TotalDuration,
                PhaseCount = _phaseResults.Count,
                SuccessCount = _phaseResults.FindAll(p => p.Ok).Count,
                FailureCount = _phaseResults.FindAll(p => !p.Ok).Count,
                CurrentFPS = 1f / Time.deltaTime,
                MemoryUsageMB = _performanceMetrics.MemoryUsageMB
            };
        }
        
        private void UpdatePerformanceMetrics()
        {
            _frameCount++;
            var currentFps = 1f / Time.deltaTime;
            _fpsSum += currentFps;
            _minFps = Mathf.Min(_minFps, currentFps);
            _maxFps = Mathf.Max(_maxFps, currentFps);
            
            _performanceMetrics.AverageFPS = _fpsSum / _frameCount;
            _performanceMetrics.MinFPS = _minFps;
            _performanceMetrics.MaxFPS = _maxFps;
            _performanceMetrics.FrameCount = _frameCount;
            _performanceMetrics.MemoryUsageMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false) / (1024f * 1024f);
        }
        
        private void CheckHeartbeat()
        {
            if (Time.realtimeSinceStartup - _lastHeartbeat >= HeartbeatInterval)
            {
                _lastHeartbeat = Time.realtimeSinceStartup;
                
                var summary = GetRunSummary();
                Debug.Log($"💓 Heartbeat - Run: {summary.RunId}, FPS: {summary.CurrentFPS:F1}, Memory: {summary.MemoryUsageMB:F1}MB");
            }
        }
        
        private void GenerateArtifacts()
        {
            try
            {
                var artifactsDir = Path.Combine(Application.dataPath, "Generated", "run_artifacts", CurrentRun.RunId);
                Directory.CreateDirectory(artifactsDir);
                
                // Generate JSON summary
                var summaryPath = Path.Combine(artifactsDir, "summary.json");
                var summaryJson = JsonUtility.ToJson(CurrentRun, true);
                File.WriteAllText(summaryPath, summaryJson);
                
                // Generate detailed metrics
                if (EnableDetailedMetrics)
                {
                    var detailedPath = Path.Combine(artifactsDir, "detailed_metrics.json");
                    var detailedJson = JsonUtility.ToJson(CurrentRun, true);
                    File.WriteAllText(detailedPath, detailedJson);
                }
                
                Debug.Log($"📊 Metrics artifacts generated in: {artifactsDir}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to generate metrics artifacts: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Summary of current run status
    /// </summary>
    [System.Serializable]
    public struct RunSummary
    {
        public string RunId;
        public bool IsRunning;
        public float Duration;
        public int PhaseCount;
        public int SuccessCount;
        public int FailureCount;
        public float CurrentFPS;
        public float MemoryUsageMB;
    }
}
