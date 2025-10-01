using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System.Collections;
using System.IO;
using System;
using NexoDirectorStudio.Agents;
using NexoDirectorStudio.Interactions;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Tests.PlayMode
{
    /// <summary>
    /// End-to-end test for DirectorStudio in headless/batch mode.
    /// Tests the complete pipeline: prompt → plan → build → play → interactions.
    /// </summary>
    public class Director_E2E_Headless
    {
        private const float TestTimeoutSeconds = 30f;
        private const int MinExpectedInteractions = 1;
        
        [UnityTest, Order(1)]
        public IEnumerator generates_plays_and_triggers_interactions()
        {
            Debug.Log("🚀 Starting DirectorStudio E2E test...");
            
            // 1) Find or create DirectorStudioService
            var director = Object.FindFirstObjectByType<DirectorStudioService>();
            if (director == null)
            {
                var directorGO = new GameObject("DirectorStudioService");
                director = directorGO.AddComponent<DirectorStudioService>();
            }
            Assert.IsNotNull(director, "DirectorStudioService not found");
            
            // 2) Create AgentDirector for the test
            var agentDirectorGO = new GameObject("AgentDirector");
            var agentDirector = agentDirectorGO.AddComponent<AgentDirector>();
            
            // Configure test parameters
            agentDirector.prompt = "short FPS room with a switch and a door";
            agentDirector.genreHint = "FPS";
            agentDirector.targetMinutes = 5;
            agentDirector.difficulty = 3;
            agentDirector.seed = 12345;
            agentDirector.autoLaunch = false; // We'll control the launch manually
            agentDirector.attachAutoplayer = true;
            
            // 3) Run the generation pipeline
            yield return RunGenerationPipeline(agentDirector);
            
            // 4) Ensure InteractionBus exists
            var bus = Object.FindFirstObjectByType<InteractionBus>();
            if (bus == null)
            {
                var busGO = new GameObject("InteractionBus");
                bus = busGO.AddComponent<InteractionBus>();
            }
            Assert.IsNotNull(bus, "InteractionBus not found");
            
            // 5) Run the autoplayer for a short budget
            yield return RunAutoplayerTest(bus);
            
            // 6) Assert something happened
            var metrics = bus.GetMetrics();
            Assert.Greater(metrics.TriggeredCount, 0, 
                $"No interactions were triggered by the autoplayer. Total interactions: {metrics.TotalInteractions}, Armed: {metrics.ArmedCount}");
            
            // 7) Generate test report
            yield return GenerateTestReport(metrics);
            
            Debug.Log("✅ DirectorStudio E2E test completed successfully!");
        }
        
        private IEnumerator RunGenerationPipeline(AgentDirector agentDirector)
        {
            Debug.Log("📋 Running generation pipeline...");
            
            var startTime = Time.realtimeSinceStartup;
            
            // Run the agent director
            var task = agentDirector.RunAsync(System.Threading.CancellationToken.None);
            
            // Wait for completion with timeout
            while (!task.IsCompleted && (Time.realtimeSinceStartup - startTime) < TestTimeoutSeconds)
            {
                yield return null;
            }
            
            if (!task.IsCompleted)
            {
                Assert.Fail($"Generation pipeline timed out after {TestTimeoutSeconds} seconds");
            }
            
            if (task.IsFaulted)
            {
                Assert.Fail($"Generation pipeline failed: {task.Exception?.GetBaseException().Message}");
            }
            
            Debug.Log("✅ Generation pipeline completed");
        }
        
        private IEnumerator RunAutoplayerTest(InteractionBus bus)
        {
            Debug.Log("🤖 Running autoplayer test...");
            
            var startTime = Time.realtimeSinceStartup;
            var endTime = startTime + 10f; // 10 second budget for autoplayer
            
            // Find the autoplayer
            var autoplayer = Object.FindFirstObjectByType<AIAutoplayer>();
            Assert.IsNotNull(autoplayer, "AIAutoplayer not found");
            
            // Run the autoplayer
            while (Time.realtimeSinceStartup < endTime)
            {
                // Let the autoplayer run
                yield return null;
                
                // Check if we have enough interactions
                var metrics = bus.GetMetrics();
                if (metrics.TriggeredCount >= MinExpectedInteractions)
                {
                    Debug.Log($"✅ Target interactions reached: {metrics.TriggeredCount}");
                    break;
                }
            }
            
            var finalMetrics = bus.GetMetrics();
            Debug.Log($"🤖 Autoplayer test completed. Interactions triggered: {finalMetrics.TriggeredCount}");
        }
        
        private IEnumerator GenerateTestReport(InteractionMetrics metrics)
        {
            Debug.Log("📊 Generating test report...");
            
            var report = new TestReport
            {
                TestName = "Director_E2E_Headless",
                Timestamp = DateTime.Now,
                Duration = Time.realtimeSinceStartup,
                Success = metrics.TriggeredCount > 0,
                Metrics = metrics,
                Environment = new TestEnvironment
                {
                    UnityVersion = Application.unityVersion,
                    Platform = Application.platform.ToString(),
                    IsBatchMode = Application.isBatchMode,
                    IsHeadless = Application.isBatchMode
                }
            };
            
            // Save report to file
            var reportPath = Path.Combine(Application.dataPath, "Generated", "test_reports");
            Directory.CreateDirectory(reportPath);
            
            var fileName = $"director_e2e_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(reportPath, fileName);
            
            var json = JsonUtility.ToJson(report, true);
            File.WriteAllText(filePath, json);
            
            Debug.Log($"📊 Test report saved to: {filePath}");
            
            yield return null;
        }
        
        [TearDown]
        public void TearDown()
        {
            // Clean up test objects
            var testObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var obj in testObjects)
            {
                if (obj.name.Contains("Test") || obj.name.Contains("AgentDirector") || obj.name.Contains("DirectorStudioService"))
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }
    }
    
    /// <summary>
    /// Test report data structure
    /// </summary>
    [System.Serializable]
    public struct TestReport
    {
        public string TestName;
        public DateTime Timestamp;
        public float Duration;
        public bool Success;
        public InteractionMetrics Metrics;
        public TestEnvironment Environment;
    }
    
    /// <summary>
    /// Test environment information
    /// </summary>
    [System.Serializable]
    public struct TestEnvironment
    {
        public string UnityVersion;
        public string Platform;
        public bool IsBatchMode;
        public bool IsHeadless;
    }
}
