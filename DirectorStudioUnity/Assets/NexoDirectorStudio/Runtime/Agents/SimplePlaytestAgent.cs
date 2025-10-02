using System;
using System.Collections;
using UnityEngine;
using NexoDirectorStudio.Interactions;

namespace NexoDirectorStudio.Agents
{
    /// <summary>
    /// Simple playtesting agent that monitors AI autoplayer performance
    /// </summary>
    public class SimplePlaytestAgent : MonoBehaviour
    {
        [Header("Playtest Configuration")]
        public float testDuration = 30f;
        public bool enableMetrics = true;
        
        [Header("Metrics")]
        public PlaytestMetrics metrics = new PlaytestMetrics();
        
        private float startTime;
        private bool isTesting = false;
        private AIAutoplayer autoplayer;
        private InteractionBus interactionBus;
        
        public event Action<PlaytestMetrics> OnPlaytestComplete;
        public event Action<string> OnPlaytestEvent;

        void Start()
        {
            startTime = Time.time;
            isTesting = true;
            
            // Find autoplayer
            autoplayer = FindFirstObjectByType<AIAutoplayer>();
            if (autoplayer == null)
            {
                Debug.LogWarning("[SimplePlaytestAgent] No AIAutoplayer found, creating one");
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    autoplayer = player.AddComponent<AIAutoplayer>();
                }
            }
            
            // Find InteractionBus
            interactionBus = FindFirstObjectByType<InteractionBus>();
            if (interactionBus == null)
            {
                Debug.LogWarning("[SimplePlaytestAgent] No InteractionBus found, creating one");
                var busGO = new GameObject("InteractionBus");
                interactionBus = busGO.AddComponent<InteractionBus>();
            }
            
            // Start playtest coroutine
            StartCoroutine(RunPlaytest());
            
            OnPlaytestEvent?.Invoke("🎮 Simple playtest started");
        }

        void Update()
        {
            if (!isTesting) return;
            
            // Update metrics
            if (enableMetrics)
            {
                UpdateMetrics();
            }
        }

        private IEnumerator RunPlaytest()
        {
            OnPlaytestEvent?.Invoke($"⏱️ Running playtest for {testDuration} seconds...");
            
            // Wait for test duration
            yield return new WaitForSeconds(testDuration);
            
            // Complete playtest
            CompletePlaytest();
        }

        private void UpdateMetrics()
        {
            metrics.playtestDuration = Time.time - startTime;
            metrics.framesRendered = Time.frameCount;
            metrics.averageFPS = metrics.framesRendered / metrics.playtestDuration;
            
            // Update position tracking
            if (autoplayer != null)
            {
                metrics.maxDistanceTraveled = Mathf.Max(metrics.maxDistanceTraveled, 
                    Vector3.Distance(Vector3.zero, autoplayer.transform.position));
            }
            
            // Count interactions
            if (interactionBus != null)
            {
                metrics.totalInteractionsFound = interactionBus.TotalInteractions;
                metrics.interactionTriggerCount = interactionBus.TriggeredCount;
            }
        }

        private int CountTriggeredInteractions()
        {
            if (interactionBus == null) return 0;
            return interactionBus.TriggeredCount;
        }

        private void CompletePlaytest()
        {
            isTesting = false;
            
            // Finalize metrics
            metrics.playtestCompleted = true;
            metrics.completionTime = Time.time;
            metrics.totalPlaytestDuration = Time.time - startTime;
            
            // Calculate success rate
            if (metrics.totalInteractionsFound > 0)
            {
                metrics.interactionSuccessRate = (float)metrics.interactionTriggerCount / metrics.totalInteractionsFound;
            }
            
            // Generate playtest report
            var report = GeneratePlaytestReport();
            OnPlaytestEvent?.Invoke($"📊 Playtest completed!\n{report}");
            
            // Notify completion
            OnPlaytestComplete?.Invoke(metrics);
            
            Debug.Log($"[SimplePlaytestAgent] Playtest completed: {report}");
        }

        private string GeneratePlaytestReport()
        {
            var report = $"Simple Playtest Report:\n";
            report += $"⏱️ Duration: {metrics.totalPlaytestDuration:F1}s\n";
            report += $"🎯 FPS: {metrics.averageFPS:F1}\n";
            report += $"🚶 Distance: {metrics.maxDistanceTraveled:F1}m\n";
            report += $"🔍 Interactions Found: {metrics.totalInteractionsFound}\n";
            report += $"⚡ Interactions Triggered: {metrics.interactionTriggerCount}\n";
            report += $"✅ Success Rate: {metrics.interactionSuccessRate:P1}\n";
            report += $"🎮 Status: {(metrics.playtestCompleted ? "COMPLETED" : "FAILED")}";
            
            return report;
        }
    }
    
    /// <summary>
    /// Metrics collected during playtesting
    /// </summary>
    [System.Serializable]
    public class PlaytestMetrics
    {
        [Header("Timing")]
        public float playtestDuration;
        public float totalPlaytestDuration;
        public float completionTime;
        public bool playtestCompleted;
        
        [Header("Performance")]
        public int framesRendered;
        public float averageFPS;
        public float maxDistanceTraveled;
        
        [Header("Interactions")]
        public int interactionsDiscovered;
        public int totalInteractionsFound;
        public int interactionTriggerCount;
        public int successfulInteractions;
        public float interactionSuccessRate;
        public float lastInteractionTime;
        
        [Header("Gameplay")]
        public int enemiesDefeated;
        public int collectiblesFound;
        public int objectivesCompleted;
        public int deaths;
        public int respawns;
    }
}
