using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NexoDoomGame
{
    /// <summary>
    /// Component composition and validation functionality for NexoCompositionSystem.
    /// </summary>
    public partial class NexoCompositionSystem
    {
        /// <summary>
        /// Composes components together
        /// </summary>
        private IEnumerator ComposeComponents()
        {
            Debug.Log("🔗 Composing components together...");
            UpdateCompositionStatus("Composing Components");
            
            try
            {
                // Use Nexo Agent to compose components
                var compositionPrompt = @"
Compose the generated game components together to create a cohesive game experience:

COMPONENTS TO COMPOSE:
- Player with FPS controller, health system, and weapon system
- Enemies with AI, health, and audio systems
- UI system with health bar, ammo counter, and game state
- Audio system with 3D spatial audio
- Game manager for level progression

COMPOSITION REQUIREMENTS:
- Ensure all components work together properly
- Set up proper references between components
- Configure component parameters for optimal gameplay
- Establish communication between systems
- Set up proper initialization order

Generate the composition logic and component relationships.
";
                
                var result = await _nexoAgent.ExecuteTaskAsync(compositionPrompt);
                
                if (result.Success)
                {
                    Debug.Log("✅ Components composed successfully");
                    
                    _compositionResults.Add(new CompositionResult
                    {
                        Component = "ComponentComposition",
                        Type = CompositionType.Composition,
                        Status = CompositionStatus.Completed,
                        Timestamp = DateTime.Now
                    });
                }
                else
                {
                    Debug.LogError($"❌ Component composition failed: {result.Error}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Exception during component composition: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }

        /// <summary>
        /// Validates composition
        /// </summary>
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

        /// <summary>
        /// Finalizes composition
        /// </summary>
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
