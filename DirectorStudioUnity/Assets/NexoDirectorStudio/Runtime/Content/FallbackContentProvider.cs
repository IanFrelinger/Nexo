using System;
using System.Collections.Generic;
using UnityEngine;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Content
{
    public static class FallbackContentProvider
    {
        private static readonly Dictionary<string, Color> DesignTokens = new Dictionary<string, Color>
        {
            { "primary", new Color(0.2f, 0.6f, 1.0f) },      // Blue
            { "secondary", new Color(0.8f, 0.4f, 0.2f) },    // Orange
            { "accent", new Color(0.9f, 0.9f, 0.1f) },       // Yellow
            { "success", new Color(0.2f, 0.8f, 0.3f) },      // Green
            { "danger", new Color(0.9f, 0.2f, 0.2f) },       // Red
            { "neutral", new Color(0.5f, 0.5f, 0.5f) },      // Gray
            { "background", new Color(0.1f, 0.1f, 0.1f) },   // Dark
            { "surface", new Color(0.2f, 0.2f, 0.2f) }       // Dark Gray
        };

        public static ContentBundle CreateFallbackContent(string context = "review")
        {
            var assets = new List<AssetData>();
            
            // Add texture assets
            foreach (var texture in GenerateFallbackTextures())
            {
                assets.Add(new AssetData
                {
                    Id = texture,
                    AssetType = "Texture",
                    Name = texture,
                    AssetPath = $"Assets/FallbackContent/Textures/{texture}.png",
                    AssetDataJson = "{\"fallback\": true, \"type\": \"texture\"}"
                });
            }
            
            // Add model assets
            foreach (var model in GenerateFallbackModels())
            {
                assets.Add(new AssetData
                {
                    Id = model,
                    AssetType = "Model",
                    Name = model,
                    AssetPath = $"Assets/FallbackContent/Models/{model}.fbx",
                    AssetDataJson = "{\"fallback\": true, \"type\": \"model\"}"
                });
            }
            
            // Add audio assets
            foreach (var audio in GenerateFallbackAudio())
            {
                assets.Add(new AssetData
                {
                    Id = audio,
                    AssetType = "Audio",
                    Name = audio,
                    AssetPath = $"Assets/FallbackContent/Audio/{audio}.wav",
                    AssetDataJson = "{\"fallback\": true, \"type\": \"audio\"}"
                });
            }
            
            // Add prefab assets
            foreach (var prefab in GenerateFallbackPrefabs())
            {
                assets.Add(new AssetData
                {
                    Id = prefab,
                    AssetType = "Prefab",
                    Name = prefab,
                    AssetPath = $"Assets/FallbackContent/Prefabs/{prefab}.prefab",
                    AssetDataJson = "{\"fallback\": true, \"type\": \"prefab\"}"
                });
            }
            
            return new ContentBundle
            {
                Id = $"fallback-{context}-{Guid.NewGuid():N}",
                InteractionGraphId = "fallback-interactions",
                Assets = assets,
                AddressablesGroup = new AddressablesGroupData
                {
                    GroupName = "FallbackContent",
                    Labels = new[] { "fallback", "review" }
                },
                Metadata = new BundleMetadata
                {
                    Description = $"Generated fallback content for review purposes ({context})",
                    Version = "1.0.0",
                    Author = "DirectorStudio Fallback"
                },
                Seed = 1337,
                GeneratedAt = DateTimeOffset.UtcNow
            };
        }

        private static string[] GenerateFallbackTextures()
        {
            return new[]
            {
                "fallback_texture_floor_001",      // Floor texture
                "fallback_texture_wall_001",       // Wall texture
                "fallback_texture_ceiling_001",    // Ceiling texture
                "fallback_texture_metal_001",      // Metal surface
                "fallback_texture_wood_001",       // Wood surface
                "fallback_texture_glass_001",      // Glass surface
                "fallback_texture_fabric_001",     // Fabric surface
                "fallback_texture_concrete_001"    // Concrete surface
            };
        }

        private static string[] GenerateFallbackModels()
        {
            return new[]
            {
                "fallback_model_cube_001",         // Basic cube
                "fallback_model_sphere_001",       // Basic sphere
                "fallback_model_cylinder_001",     // Basic cylinder
                "fallback_model_capsule_001",      // Basic capsule
                "fallback_model_plane_001",        // Basic plane
                "fallback_model_character_001",    // Simple character
                "fallback_model_prop_001",         // Simple prop
                "fallback_model_weapon_001"        // Simple weapon
            };
        }

        private static string[] GenerateFallbackAudio()
        {
            return new[]
            {
                "fallback_audio_ui_click",         // UI click sound
                "fallback_audio_ui_hover",         // UI hover sound
                "fallback_audio_ui_notification",  // UI notification
                "fallback_audio_footstep_001",     // Footstep sound
                "fallback_audio_footstep_002",     // Footstep sound variant
                "fallback_audio_ambient_001",      // Ambient sound
                "fallback_audio_weapon_fire",      // Weapon fire sound
                "fallback_audio_weapon_reload",    // Weapon reload sound
                "fallback_audio_door_open",        // Door open sound
                "fallback_audio_door_close",       // Door close sound
                "fallback_audio_pickup_001",       // Pickup sound
                "fallback_audio_pickup_002",       // Pickup sound variant
                "fallback_audio_explosion_001",    // Explosion sound
                "fallback_audio_voice_001",        // Voice line
                "fallback_audio_voice_002"         // Voice line variant
            };
        }

        private static string[] GenerateFallbackPrefabs()
        {
            return new[]
            {
                "fallback_prefab_player",          // Player prefab
                "fallback_prefab_enemy",           // Enemy prefab
                "fallback_prefab_pickup",          // Pickup prefab
                "fallback_prefab_weapon",          // Weapon prefab
                "fallback_prefab_door",            // Door prefab
                "fallback_prefab_switch",          // Switch prefab
                "fallback_prefab_platform",        // Platform prefab
                "fallback_prefab_light",           // Light prefab
                "fallback_prefab_ui_hud",          // HUD prefab
                "fallback_prefab_ui_menu"          // Menu prefab
            };
        }

        private static long CalculateTotalSize()
        {
            // Estimate fallback content size
            var textureSize = 8 * 1024 * 1024;      // 8MB for textures
            var modelSize = 2 * 1024 * 1024;        // 2MB for models
            var audioSize = 5 * 1024 * 1024;        // 5MB for audio
            var prefabSize = 1 * 1024 * 1024;       // 1MB for prefabs
            
            return textureSize + modelSize + audioSize + prefabSize;
        }

        public static Color GetDesignToken(string tokenName)
        {
            return DesignTokens.TryGetValue(tokenName, out var color) ? color : DesignTokens["neutral"];
        }

        public static string[] GetAvailableTokens()
        {
            return new List<string>(DesignTokens.Keys).ToArray();
        }

        public static string GenerateTTSAudio(string text, string voiceId = "fallback")
        {
            // In a real implementation, this would call a TTS service
            // For now, return a placeholder audio ID
            return $"tts_{voiceId}_{text.GetHashCode():X8}";
        }

        public static string CreateTokenizedMaterial(string baseName, string primaryToken, string secondaryToken = null)
        {
            var primary = GetDesignToken(primaryToken);
            var secondary = secondaryToken != null ? GetDesignToken(secondaryToken) : primary;
            
            return $"material_{baseName}_{primaryToken}_{secondaryToken ?? "none"}_{DateTime.UtcNow.Ticks:X8}";
        }

        public static string CreateLowPolyModel(string type, string colorToken)
        {
            var color = GetDesignToken(colorToken);
            return $"lowpoly_{type}_{colorToken}_{color.GetHashCode():X8}";
        }

        public static string CreateStandardHUD(string theme = "dark")
        {
            var primary = theme == "dark" ? "primary" : "accent";
            var background = theme == "dark" ? "background" : "surface";
            
            return $"hud_standard_{theme}_{primary}_{background}_{DateTime.UtcNow.Ticks:X8}";
        }
    }
}
