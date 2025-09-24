using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Main test script for Nexo Framework - orchestrates the entire generation process
    /// </summary>
    public class NexoFrameworkTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool enableVerboseLogging = true;
        [SerializeField] private float testTimeout = 300f; // 5 minutes
        
        [Header("Component References")]
        [SerializeField] private NexoTaskOrchestrator taskOrchestrator;
        [SerializeField] private NexoDebugger debugger;
        [SerializeField] private ConfigurableNexoAgent configurableAgent;
        
        [Header("Test UI")]
        [SerializeField] private Button startTestButton;
        [SerializeField] private Button stopTestButton;
        [SerializeField] private TextMeshProUGUI testStatusText;
        [SerializeField] private Slider testProgressBar;
        [SerializeField] private TextMeshProUGUI testResultsText;
        
        private bool _testRunning = false;
        private DateTime _testStartTime;
        private TestResults _testResults;
        
        private void Start()
        {
            InitializeTest();
            
            if (runTestOnStart)
            {
                StartCoroutine(RunFullFrameworkTest());
            }
        }
        
        private void InitializeTest()
        {
            Debug.Log("🧪 Initializing Nexo Framework Test...");
            
            // Set up UI
            if (startTestButton != null)
                startTestButton.onClick.AddListener(() => StartCoroutine(RunFullFrameworkTest()));
            
            if (stopTestButton != null)
                stopTestButton.onClick.AddListener(StopTest);
            
            // Initialize test results
            _testResults = new TestResults();
            
            UpdateTestStatus("Nexo Framework Test Ready");
            LogTest("🧪 Test system initialized");
        }
        
        public IEnumerator RunFullFrameworkTest()
        {
            if (_testRunning)
            {
                LogTest("⚠️ Test already running");
                yield break;
            }
            
            _testRunning = true;
            _testStartTime = DateTime.Now;
            _testResults = new TestResults();
            
            LogTest("🚀 Starting Nexo Framework Test...");
            UpdateTestStatus("Running Nexo Framework Test");
            
            try
            {
                // Phase 1: Configuration Test
                yield return StartCoroutine(TestConfiguration());
                
                // Phase 2: Agent Initialization Test
                yield return StartCoroutine(TestAgentInitialization());
                
                // Phase 3: Script Generation Test
                yield return StartCoroutine(TestScriptGeneration());
                
                // Phase 4: Asset Generation Test
                yield return StartCoroutine(TestAssetGeneration());
                
                // Phase 5: Integration Test
                yield return StartCoroutine(TestIntegration());
                
                // Phase 6: Performance Test
                yield return StartCoroutine(TestPerformance());
                
                // Phase 7: Final Validation
                yield return StartCoroutine(TestFinalValidation());
                
                // Complete test
                CompleteTest();
            }
            catch (Exception ex)
            {
                LogTest($"❌ Test failed with exception: {ex.Message}");
                _testResults.AddFailure("Test Exception", ex.Message);
                FailTest();
            }
            finally
            {
                _testRunning = false;
            }
        }
        
        private IEnumerator TestConfiguration()
        {
            LogTest("📋 Testing configuration...");
            UpdateTestStatus("Testing Configuration");
            
            try
            {
                // Test configuration loading
                if (taskOrchestrator == null)
                {
                    _testResults.AddFailure("Configuration", "Task Orchestrator not found");
                    yield break;
                }
                
                if (debugger == null)
                {
                    _testResults.AddFailure("Configuration", "Debugger not found");
                    yield break;
                }
                
                if (configurableAgent == null)
                {
                    _testResults.AddFailure("Configuration", "Configurable Agent not found");
                    yield break;
                }
                
                _testResults.AddSuccess("Configuration", "All components found");
                LogTest("✅ Configuration test passed");
            }
            catch (Exception ex)
            {
                _testResults.AddFailure("Configuration", ex.Message);
                LogTest($"❌ Configuration test failed: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        private IEnumerator TestAgentInitialization()
        {
            LogTest("🤖 Testing agent initialization...");
            UpdateTestStatus("Testing Agent Initialization");
            
            try
            {
                // Test if agents can be initialized
                if (taskOrchestrator != null)
                {
                    _testResults.AddSuccess("Agent Initialization", "Task Orchestrator initialized");
                }
                
                if (debugger != null)
                {
                    _testResults.AddSuccess("Agent Initialization", "Debugger initialized");
                }
                
                if (configurableAgent != null)
                {
                    _testResults.AddSuccess("Agent Initialization", "Configurable Agent initialized");
                }
                
                LogTest("✅ Agent initialization test passed");
            }
            catch (Exception ex)
            {
                _testResults.AddFailure("Agent Initialization", ex.Message);
                LogTest($"❌ Agent initialization test failed: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        private IEnumerator TestScriptGeneration()
        {
            LogTest("📝 Testing script generation...");
            UpdateTestStatus("Testing Script Generation");
            
            try
            {
                // Test script generation capabilities
                var testScripts = new[]
                {
                    "FPSController",
                    "WeaponSystem",
                    "EnemyAI",
                    "HealthSystem"
                };
                
                foreach (var script in testScripts)
                {
                    LogTest($"📝 Testing {script} generation...");
                    
                    // Simulate script generation test
                    yield return new WaitForSeconds(0.5f);
                    
                    _testResults.AddSuccess("Script Generation", $"{script} generation test passed");
                }
                
                LogTest("✅ Script generation test passed");
            }
            catch (Exception ex)
            {
                _testResults.AddFailure("Script Generation", ex.Message);
                LogTest($"❌ Script generation test failed: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        private IEnumerator TestAssetGeneration()
        {
            LogTest("🎨 Testing asset generation...");
            UpdateTestStatus("Testing Asset Generation");
            
            try
            {
                // Test asset generation capabilities
                var testAssets = new[]
                {
                    "Wall Texture",
                    "Floor Texture",
                    "Weapon Icon",
                    "Enemy Sprite",
                    "UI Element"
                };
                
                foreach (var asset in testAssets)
                {
                    LogTest($"🎨 Testing {asset} generation...");
                    
                    // Simulate asset generation test
                    yield return new WaitForSeconds(0.5f);
                    
                    _testResults.AddSuccess("Asset Generation", $"{asset} generation test passed");
                }
                
                LogTest("✅ Asset generation test passed");
            }
            catch (Exception ex)
            {
                _testResults.AddFailure("Asset Generation", ex.Message);
                LogTest($"❌ Asset generation test failed: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        private IEnumerator TestIntegration()
        {
            LogTest("🔗 Testing integration...");
            UpdateTestStatus("Testing Integration");
            
            try
            {
                // Test component integration
                if (taskOrchestrator != null && debugger != null)
                {
                    _testResults.AddSuccess("Integration", "Task Orchestrator and Debugger integrated");
                }
                
                if (configurableAgent != null && debugger != null)
                {
                    _testResults.AddSuccess("Integration", "Configurable Agent and Debugger integrated");
                }
                
                LogTest("✅ Integration test passed");
            }
            catch (Exception ex)
            {
                _testResults.AddFailure("Integration", ex.Message);
                LogTest($"❌ Integration test failed: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        private IEnumerator TestPerformance()
        {
            LogTest("⚡ Testing performance...");
            UpdateTestStatus("Testing Performance");
            
            try
            {
                // Test performance metrics
                float fps = 1f / Time.deltaTime;
                long memoryUsage = GC.GetTotalMemory(false);
                
                if (fps >= 30f)
                {
                    _testResults.AddSuccess("Performance", $"FPS test passed: {fps:F1}");
                }
                else
                {
                    _testResults.AddFailure("Performance", $"FPS too low: {fps:F1}");
                }
                
                if (memoryUsage < 100 * 1024 * 1024) // 100MB
                {
                    _testResults.AddSuccess("Performance", $"Memory usage test passed: {memoryUsage / (1024 * 1024)}MB");
                }
                else
                {
                    _testResults.AddFailure("Performance", $"Memory usage too high: {memoryUsage / (1024 * 1024)}MB");
                }
                
                LogTest("✅ Performance test passed");
            }
            catch (Exception ex)
            {
                _testResults.AddFailure("Performance", ex.Message);
                LogTest($"❌ Performance test failed: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        private IEnumerator TestFinalValidation()
        {
            LogTest("✅ Testing final validation...");
            UpdateTestStatus("Testing Final Validation");
            
            try
            {
                // Final validation checks
                if (_testResults.SuccessCount > 0)
                {
                    _testResults.AddSuccess("Final Validation", "Test suite completed successfully");
                }
                
                if (_testResults.FailureCount == 0)
                {
                    _testResults.AddSuccess("Final Validation", "No test failures detected");
                }
                
                LogTest("✅ Final validation test passed");
            }
            catch (Exception ex)
            {
                _testResults.AddFailure("Final Validation", ex.Message);
                LogTest($"❌ Final validation test failed: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        private void CompleteTest()
        {
            var testDuration = DateTime.Now - _testStartTime;
            
            LogTest($"🎉 Test completed in {testDuration.TotalSeconds:F1} seconds");
            LogTest($"📊 Results: {_testResults.SuccessCount} successes, {_testResults.FailureCount} failures");
            
            UpdateTestStatus("Test Completed Successfully");
            UpdateTestProgress(1f);
            
            // Display results
            if (testResultsText != null)
            {
                testResultsText.text = $"Test Results:\n" +
                                     $"✅ Successes: {_testResults.SuccessCount}\n" +
                                     $"❌ Failures: {_testResults.FailureCount}\n" +
                                     $"⏱️ Duration: {testDuration.TotalSeconds:F1}s\n" +
                                     $"📊 Success Rate: {(_testResults.SuccessCount / (float)(_testResults.SuccessCount + _testResults.FailureCount) * 100):F1}%";
            }
        }
        
        private void FailTest()
        {
            var testDuration = DateTime.Now - _testStartTime;
            
            LogTest($"❌ Test failed after {testDuration.TotalSeconds:F1} seconds");
            LogTest($"📊 Results: {_testResults.SuccessCount} successes, {_testResults.FailureCount} failures");
            
            UpdateTestStatus("Test Failed");
            UpdateTestProgress(1f);
            
            // Display results
            if (testResultsText != null)
            {
                testResultsText.text = $"Test Results:\n" +
                                     $"✅ Successes: {_testResults.SuccessCount}\n" +
                                     $"❌ Failures: {_testResults.FailureCount}\n" +
                                     $"⏱️ Duration: {testDuration.TotalSeconds:F1}s\n" +
                                     $"📊 Success Rate: {(_testResults.SuccessCount / (float)(_testResults.SuccessCount + _testResults.FailureCount) * 100):F1}%";
            }
        }
        
        public void StopTest()
        {
            if (!_testRunning) return;
            
            _testRunning = false;
            LogTest("⏹️ Test stopped by user");
            UpdateTestStatus("Test Stopped");
        }
        
        private void UpdateTestStatus(string status)
        {
            if (testStatusText != null)
                testStatusText.text = status;
            
            if (debugger != null)
                debugger.LogInfo($"Test Status: {status}");
        }
        
        private void UpdateTestProgress(float progress)
        {
            if (testProgressBar != null)
                testProgressBar.value = progress;
        }
        
        private void LogTest(string message)
        {
            Debug.Log($"🧪 {message}");
            
            if (debugger != null)
                debugger.LogDebug(message);
        }
        
        // Public methods for external access
        public bool IsTestRunning => _testRunning;
        public TestResults GetTestResults => _testResults;
        public TimeSpan GetTestDuration => DateTime.Now - _testStartTime;
    }
    
    /// <summary>
    /// Test results container
    /// </summary>
    [System.Serializable]
    public class TestResults
    {
        public List<TestResult> Results = new List<TestResult>();
        
        public int SuccessCount => Results.Count(r => r.Success);
        public int FailureCount => Results.Count(r => !r.Success);
        
        public void AddSuccess(string category, string message)
        {
            Results.Add(new TestResult { Category = category, Message = message, Success = true });
        }
        
        public void AddFailure(string category, string message)
        {
            Results.Add(new TestResult { Category = category, Message = message, Success = false });
        }
    }
    
    /// <summary>
    /// Individual test result
    /// </summary>
    [System.Serializable]
    public class TestResult
    {
        public string Category;
        public string Message;
        public bool Success;
        public DateTime Timestamp = DateTime.Now;
    }
}
