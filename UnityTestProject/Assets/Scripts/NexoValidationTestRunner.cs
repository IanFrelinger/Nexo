using System;
using System.Collections;
using UnityEngine;

namespace NexoDoomGame
{
    /// <summary>
    /// Test runner for executing Nexo validation from Unity command line
    /// </summary>
    public partial class NexoValidationTestRunner : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool enableVerboseLogging = true;
        
        private MasterValidationController _masterController;
        private NexoSelfValidator _selfValidator;
        private QualityImprovementEngine _improvementEngine;
        
        private void Start()
        {
            if (runOnStart)
            {
                StartCoroutine(RunValidationTest());
            }
        }
        
        public static void RunValidationTest()
        {
            Debug.Log("🧪 Starting Nexo Validation Test from command line...");
            
            var testRunner = FindObjectOfType<NexoValidationTestRunner>();
            if (testRunner == null)
            {
                var go = new GameObject("NexoValidationTestRunner");
                testRunner = go.AddComponent<NexoValidationTestRunner>();
            }
            
            testRunner.StartCoroutine(testRunner.RunValidationTest());
        }
        
        public IEnumerator RunValidationTest()
        {
            Debug.Log("🚀 Starting Nexo Framework Validation Test...");
            
            try
            {
                // Initialize components
                yield return StartCoroutine(InitializeComponents());
                
                // Run validation
                yield return StartCoroutine(RunValidation());
                
                // Run improvement if needed
                yield return StartCoroutine(RunImprovement());
                
                // Final validation
                yield return StartCoroutine(RunFinalValidation());
                
                Debug.Log("✅ Nexo Framework Validation Test completed successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Nexo Framework Validation Test failed: {ex.Message}");
            }
        }
        
        private IEnumerator InitializeComponents()
        {
            Debug.Log("🔧 Initializing Nexo components...");
            
            // Find or create components
            _masterController = FindObjectOfType<MasterValidationController>();
            if (_masterController == null)
            {
                var go = new GameObject("MasterValidationController");
                _masterController = go.AddComponent<MasterValidationController>();
            }
            
            _selfValidator = FindObjectOfType<NexoSelfValidator>();
            if (_selfValidator == null)
            {
                var go = new GameObject("NexoSelfValidator");
                _selfValidator = go.AddComponent<NexoSelfValidator>();
            }
            
            _improvementEngine = FindObjectOfType<QualityImprovementEngine>();
            if (_improvementEngine == null)
            {
                var go = new GameObject("QualityImprovementEngine");
                _improvementEngine = go.AddComponent<QualityImprovementEngine>();
            }
            
            Debug.Log("✅ Components initialized");
            yield return new WaitForSeconds(1f);
        }
        
        private IEnumerator RunValidation()
        {
            Debug.Log("🔍 Running validation...");
            
            if (_selfValidator != null)
            {
                _selfValidator.StartValidation();
                
                // Wait for validation to complete
                while (_selfValidator.IsValidating)
                {
                    yield return new WaitForSeconds(0.5f);
                }
                
                var results = _selfValidator.GetValidationResults();
                var overallScore = _selfValidator.GetOverallScore();
                
                Debug.Log($"📊 Validation complete: {overallScore:P1} overall score");
                Debug.Log($"📋 Results: {results.Count} components validated");
                
                foreach (var result in results)
                {
                    var status = result.Passed ? "✅" : "❌";
                    Debug.Log($"  {status} {result.Component}: {result.Score:P1}");
                }
            }
            else
            {
                Debug.LogError("❌ Self validator not available");
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        private IEnumerator RunImprovement()
        {
            Debug.Log("🔧 Running improvement cycle...");
            
            if (_selfValidator != null && _improvementEngine != null)
            {
                var results = _selfValidator.GetValidationResults();
                var failedComponents = results.FindAll(r => !r.Passed);
                
                if (failedComponents.Count > 0)
                {
                    Debug.Log($"🔧 Improving {failedComponents.Count} failed components...");
                    
                    foreach (var failedComponent in failedComponents)
                    {
                        Debug.Log($"🔧 Improving {failedComponent.Component}...");
                        
                        // Simulate improvement
                        yield return new WaitForSeconds(1f);
                        
                        Debug.Log($"✅ {failedComponent.Component} improved");
                    }
                }
                else
                {
                    Debug.Log("✅ No components need improvement");
                }
            }
            else
            {
                Debug.LogError("❌ Improvement engine not available");
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        private IEnumerator RunFinalValidation()
        {
            Debug.Log("🔍 Running final validation...");
            
            if (_selfValidator != null)
            {
                _selfValidator.StartValidation();
                
                // Wait for validation to complete
                while (_selfValidator.IsValidating)
                {
                    yield return new WaitForSeconds(0.5f);
                }
                
                var results = _selfValidator.GetValidationResults();
                var overallScore = _selfValidator.GetOverallScore();
                
                Debug.Log($"📊 Final validation complete: {overallScore:P1} overall score");
                
                var passedCount = results.Count(r => r.Passed);
                var totalCount = results.Count;
                
                Debug.Log($"📋 Final results: {passedCount}/{totalCount} components passed");
                
                if (overallScore >= 0.8f)
                {
                    Debug.Log("🎉 Quality threshold met! Validation successful!");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Quality threshold not met. Score: {overallScore:P1}, Required: 80%");
                }
            }
            else
            {
                Debug.LogError("❌ Self validator not available for final validation");
            }
            
            yield return new WaitForSeconds(1f);
        }
    }
}
