using UnityEngine;
using System.Collections.Generic;

namespace NexoDirectorDemo
{
    /// <summary>
    /// Demo script that showcases Nexo validation patterns
    /// This script demonstrates various architectural patterns that Nexo can validate
    /// </summary>
    public class NexoValidationDemo : MonoBehaviour
    {
        [Header("Nexo Validation Demo")]
        [SerializeField] private string projectName = "Nexo Director Demo";
        [SerializeField] private int version = 1;
        [SerializeField] private bool isProductionReady = false;
        
        [Header("Architecture Patterns")]
        [SerializeField] private List<ValidationRule> validationRules = new List<ValidationRule>();
        [SerializeField] private Dictionary<string, object> configuration = new Dictionary<string, object>();
        
        [Header("Performance Metrics")]
        [SerializeField] private float frameRate = 60f;
        [SerializeField] private int memoryUsage = 0;
        [SerializeField] private bool performanceOptimized = true;
        
        private void Start()
        {
            Debug.Log($"Nexo Director Demo started: {projectName} v{version}");
            InitializeValidationRules();
            LogArchitectureStatus();
        }
        
        private void InitializeValidationRules()
        {
            // Add some sample validation rules that Nexo can check
            validationRules.Add(new ValidationRule
            {
                Name = "Singleton Pattern",
                Description = "Ensure only one instance of critical managers",
                IsValid = true,
                Category = "Architecture"
            });
            
            validationRules.Add(new ValidationRule
            {
                Name = "Dependency Injection",
                Description = "Use proper DI container for service resolution",
                IsValid = true,
                Category = "Architecture"
            });
            
            validationRules.Add(new ValidationRule
            {
                Name = "Event System",
                Description = "Use centralized event system for loose coupling",
                IsValid = true,
                Category = "Architecture"
            });
            
            validationRules.Add(new ValidationRule
            {
                Name = "Performance Budget",
                Description = "Maintain 60fps target performance",
                IsValid = frameRate >= 60f,
                Category = "Performance"
            });
        }
        
        private void LogArchitectureStatus()
        {
            Debug.Log("=== Nexo Architecture Validation ===");
            foreach (var rule in validationRules)
            {
                string status = rule.IsValid ? "✅ PASS" : "❌ FAIL";
                Debug.Log($"{status} {rule.Category}: {rule.Name} - {rule.Description}");
            }
        }
        
        /// <summary>
        /// Method that demonstrates proper async/await patterns
        /// </summary>
        public async System.Threading.Tasks.Task PerformAsyncOperation()
        {
            Debug.Log("Starting async operation...");
            await System.Threading.Tasks.Task.Delay(1000);
            Debug.Log("Async operation completed");
        }
        
        /// <summary>
        /// Method that demonstrates proper error handling
        /// </summary>
        public bool TryExecuteOperation(string operationName)
        {
            try
            {
                Debug.Log($"Executing operation: {operationName}");
                // Simulate some work
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Operation failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Method that demonstrates proper resource management
        /// </summary>
        public void CleanupResources()
        {
            Debug.Log("Cleaning up resources...");
            // Proper cleanup implementation
        }
        
        private void OnDestroy()
        {
            CleanupResources();
        }
    }
    
    [System.Serializable]
    public class ValidationRule
    {
        public string Name;
        public string Description;
        public bool IsValid;
        public string Category;
    }
}
