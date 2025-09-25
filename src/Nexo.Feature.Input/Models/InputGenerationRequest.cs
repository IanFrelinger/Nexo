// This file has been refactored into specialized model files:
// - Core/InputGenerationRequest.cs - Core request/result models and InputType enum
// - Settings/SensitivitySettings.cs - Input sensitivity configuration
// - Settings/AccessibilitySettings.cs - Accessibility configuration
// - Configuration/InputConfiguration.cs - Input configuration model
// - Actions/InputAction.cs - Input action models and enums
// - Bindings/InputBinding.cs - Input binding models and enums
// - Maps/InputMap.cs - Input map and condition models

// Re-export all models for backward compatibility
using Nexo.Feature.Input.Models.Core;
using Nexo.Feature.Input.Models.Settings;
using Nexo.Feature.Input.Models.Configuration;
using Nexo.Feature.Input.Models.Actions;
using Nexo.Feature.Input.Models.Bindings;
using Nexo.Feature.Input.Models.Maps;

namespace Nexo.Feature.Input.Models
{
    // All models are now available through the specialized files above
    // This maintains backward compatibility while providing focused, maintainable model classes
}

