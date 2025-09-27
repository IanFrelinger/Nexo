using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Main test script for Nexo Framework - orchestrates the entire generation process
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class NexoFrameworkTest : MonoBehaviour
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
        
        public void StopTest()
        {
            if (!_testRunning) return;
            
            _testRunning = false;
            LogTest("⏹️ Test stopped by user");
            UpdateTestStatus("Test Stopped");
        }
        
        // Public methods for external access
        public bool IsTestRunning => _testRunning;
        public TestResults GetTestResults => _testResults;
        public TimeSpan GetTestDuration => DateTime.Now - _testStartTime;
        // This class acts as an orchestrator for various framework test functionalities,
        // with specific categories defined in partial classes.
    }
}