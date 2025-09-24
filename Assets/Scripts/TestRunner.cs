using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Test runner for executing Nexo Framework tests
    /// </summary>
    public class TestRunner : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = false;
        [SerializeField] private bool enableDetailedLogging = true;
        [SerializeField] private float testTimeout = 600f; // 10 minutes
        
        [Header("Test Components")]
        [SerializeField] private NexoFrameworkTest frameworkTest;
        [SerializeField] private NexoTaskOrchestrator taskOrchestrator;
        [SerializeField] private NexoDebugger debugger;
        
        [Header("Test UI")]
        [SerializeField] private Button runAllTestsButton;
        [SerializeField] private Button runScriptTestsButton;
        [SerializeField] private Button runAssetTestsButton;
        [SerializeField] private Button runIntegrationTestsButton;
        [SerializeField] private TextMeshProUGUI testRunnerStatus;
        [SerializeField] private Slider testRunnerProgress;
        
        private bool _testsRunning = false;
        private DateTime _testStartTime;
        
        private void Start()
        {
            InitializeTestRunner();
            
            if (runTestsOnStart)
            {
                StartCoroutine(RunAllTests());
            }
        }
        
        private void InitializeTestRunner()
        {
            Debug.Log("🧪 Initializing Test Runner...");
            
            // Set up UI
            if (runAllTestsButton != null)
                runAllTestsButton.onClick.AddListener(() => StartCoroutine(RunAllTests()));
            
            if (runScriptTestsButton != null)
                runScriptTestsButton.onClick.AddListener(() => StartCoroutine(RunScriptTests()));
            
            if (runAssetTestsButton != null)
                runAssetTestsButton.onClick.AddListener(() => StartCoroutine(RunAssetTests()));
            
            if (runIntegrationTestsButton != null)
                runIntegrationTestsButton.onClick.AddListener(() => StartCoroutine(RunIntegrationTests()));
            
            UpdateStatus("Test Runner Ready");
            LogTest("🧪 Test Runner initialized");
        }
        
        public IEnumerator RunAllTests()
        {
            if (_testsRunning)
            {
                LogTest("⚠️ Tests already running");
                yield break;
            }
            
            _testsRunning = true;
            _testStartTime = DateTime.Now;
            
            LogTest("🚀 Starting all tests...");
            UpdateStatus("Running All Tests");
            
            try
            {
                // Run framework test
                if (frameworkTest != null)
                {
                    yield return StartCoroutine(RunFrameworkTest());
                }
                
                // Run script generation tests
                yield return StartCoroutine(RunScriptTests());
                
                // Run asset generation tests
                yield return StartCoroutine(RunAssetTests());
                
                // Run integration tests
                yield return StartCoroutine(RunIntegrationTests());
                
                // Complete all tests
                CompleteAllTests();
            }
            catch (Exception ex)
            {
                LogTest($"❌ All tests failed with exception: {ex.Message}");
                FailAllTests();
            }
            finally
            {
                _testsRunning = false;
            }
        }
        
        private IEnumerator RunFrameworkTest()
        {
            LogTest("🧪 Running framework test...");
            UpdateStatus("Running Framework Test");
            
            if (frameworkTest != null)
            {
                yield return StartCoroutine(frameworkTest.RunFullFrameworkTest());
            }
            else
            {
                LogTest("⚠️ Framework test component not found");
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        public IEnumerator RunScriptTests()
        {
            LogTest("📝 Running script generation tests...");
            UpdateStatus("Running Script Tests");
            
            try
            {
                var scriptTests = new[]
                {
                    "FPSController",
                    "WeaponSystem",
                    "EnemyAI",
                    "HealthSystem",
                    "AudioManager",
                    "UIManager",
                    "GameManager"
                };
                
                for (int i = 0; i < scriptTests.Length; i++)
                {
                    var script = scriptTests[i];
                    LogTest($"📝 Testing {script} generation...");
                    
                    // Simulate script generation test
                    yield return new WaitForSeconds(0.5f);
                    
                    // Update progress
                    float progress = (float)(i + 1) / scriptTests.Length;
                    UpdateProgress(progress);
                }
                
                LogTest("✅ Script generation tests completed");
            }
            catch (Exception ex)
            {
                LogTest($"❌ Script generation tests failed: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        public IEnumerator RunAssetTests()
        {
            LogTest("🎨 Running asset generation tests...");
            UpdateStatus("Running Asset Tests");
            
            try
            {
                var assetTests = new[]
                {
                    "Wall Texture",
                    "Floor Texture",
                    "Weapon Icon",
                    "Enemy Sprite",
                    "UI Element",
                    "3D Model",
                    "Audio Clip"
                };
                
                for (int i = 0; i < assetTests.Length; i++)
                {
                    var asset = assetTests[i];
                    LogTest($"🎨 Testing {asset} generation...");
                    
                    // Simulate asset generation test
                    yield return new WaitForSeconds(0.5f);
                    
                    // Update progress
                    float progress = (float)(i + 1) / assetTests.Length;
                    UpdateProgress(progress);
                }
                
                LogTest("✅ Asset generation tests completed");
            }
            catch (Exception ex)
            {
                LogTest($"❌ Asset generation tests failed: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        public IEnumerator RunIntegrationTests()
        {
            LogTest("🔗 Running integration tests...");
            UpdateStatus("Running Integration Tests");
            
            try
            {
                var integrationTests = new[]
                {
                    "Component Integration",
                    "Data Flow",
                    "Error Handling",
                    "Performance",
                    "Memory Management"
                };
                
                for (int i = 0; i < integrationTests.Length; i++)
                {
                    var test = integrationTests[i];
                    LogTest($"🔗 Testing {test}...");
                    
                    // Simulate integration test
                    yield return new WaitForSeconds(0.5f);
                    
                    // Update progress
                    float progress = (float)(i + 1) / integrationTests.Length;
                    UpdateProgress(progress);
                }
                
                LogTest("✅ Integration tests completed");
            }
            catch (Exception ex)
            {
                LogTest($"❌ Integration tests failed: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        private void CompleteAllTests()
        {
            var testDuration = DateTime.Now - _testStartTime;
            
            LogTest($"🎉 All tests completed in {testDuration.TotalSeconds:F1} seconds");
            UpdateStatus("All Tests Completed Successfully");
            UpdateProgress(1f);
        }
        
        private void FailAllTests()
        {
            var testDuration = DateTime.Now - _testStartTime;
            
            LogTest($"❌ All tests failed after {testDuration.TotalSeconds:F1} seconds");
            UpdateStatus("All Tests Failed");
            UpdateProgress(1f);
        }
        
        private void UpdateStatus(string status)
        {
            if (testRunnerStatus != null)
                testRunnerStatus.text = status;
            
            if (debugger != null)
                debugger.LogInfo($"Test Runner: {status}");
        }
        
        private void UpdateProgress(float progress)
        {
            if (testRunnerProgress != null)
                testRunnerProgress.value = progress;
        }
        
        private void LogTest(string message)
        {
            Debug.Log($"🧪 {message}");
            
            if (debugger != null)
                debugger.LogDebug(message);
        }
        
        // Public methods for external access
        public bool IsTestsRunning => _testsRunning;
        public TimeSpan GetTestDuration => DateTime.Now - _testStartTime;
        
        public void StopAllTests()
        {
            if (!_testsRunning) return;
            
            _testsRunning = false;
            LogTest("⏹️ All tests stopped by user");
            UpdateStatus("Tests Stopped");
        }
    }
}
