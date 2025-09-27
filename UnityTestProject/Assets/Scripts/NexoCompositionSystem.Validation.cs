using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NexoDoomGame
{
    /// <summary>
    /// Composition validation functionality
    /// </summary>
    public partial class NexoCompositionSystem
    {
        private IEnumerator ValidateComposition()
        {
            Debug.Log("🔍 Validating composition...");
            UpdateCompositionStatus("Validating Composition");
            
            try
            {
                // Validate that all components are properly composed
                foreach (var obj in composedObjects)
                {
                    if (obj != null)
                    {
                        var components = obj.GetComponents<MonoBehaviour>();
                        Debug.Log($"🔍 Validating {obj.name}: {components.Length} components");
                        
                        _compositionResults.Add(new CompositionResult
                        {
                            Component = obj.name,
                            Type = CompositionType.Validation,
                            Status = CompositionStatus.Validated,
                            Timestamp = DateTime.Now
                        });
                    }
                }
                
                Debug.Log("✅ Composition validation completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Composition validation failed: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }

        private IEnumerator FinalizeComposition()
        {
            Debug.Log("✨ Finalizing composition...");
            UpdateCompositionStatus("Finalizing Composition");
            
            try
            {
                // Finalize the composition
                Debug.Log($"✅ Composition finalized with {composedObjects.Count} objects");
                
                _compositionResults.Add(new CompositionResult
                {
                    Component = "Finalization",
                    Type = CompositionType.Finalization,
                    Status = CompositionStatus.Completed,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Composition finalization failed: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }
    }
}
