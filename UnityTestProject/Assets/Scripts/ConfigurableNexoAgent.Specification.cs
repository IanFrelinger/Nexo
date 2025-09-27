using System;
using System.IO;
using UnityEngine;

namespace NexoDoomGame
{
    /// <summary>
    /// Specification handling functionality
    /// </summary>
    public partial class ConfigurableNexoAgent
    {
        private string GetSpecification()
        {
            if (specInputField != null && !string.IsNullOrEmpty(specInputField.text))
                return specInputField.text;
            
            return LoadDefaultSpecification();
        }

        private string LoadDefaultSpecification()
        {
            try
            {
                string specPath = Path.Combine(Application.dataPath, "GameSpecification.md");
                if (File.Exists(specPath))
                {
                    return File.ReadAllText(specPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error loading specification: {ex.Message}");
            }
            
            return GetDefaultSpecification();
        }

        private string GetDefaultSpecification()
        {
            return @"
# Default Doom-Style Game Specification

## Core Gameplay
- First-person shooter with fast-paced action
- Multiple weapons with different firing patterns
- Aggressive enemy AI with hunting behavior
- Atmospheric sci-fi horror environment

## Art Style
- Dark, gritty sci-fi aesthetic
- Red, orange, and dark gray color palette
- High-contrast textures with dramatic lighting
- Retro-futuristic UI design

## Technical Requirements
- 60 FPS target performance
- Smooth controls and responsive combat
- Optimized rendering with LOD systems
- 3D spatial audio support
";
        }
    }
}
