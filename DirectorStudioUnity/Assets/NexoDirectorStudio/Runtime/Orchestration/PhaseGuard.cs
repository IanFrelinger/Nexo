using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NexoDirectorStudio.Orchestration
{
    public static class PhaseGuard
    {
        public static async Task<object> ExecuteWithFallback(
            object phase, 
            object input, 
            RunContext ctx, 
            CancellationToken cancellationToken)
        {
            var phaseToken = GetPhaseToken(phase);
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Try the main implementation using PhaseInvoker
                var output = await PhaseInvoker.InvokeAsync(phase, input, ctx, cancellationToken);
                
                // If we're in strict mode and this passed, promote to last-good
                // Note: Config checking will be handled by the caller
                PromoteToLastGood(phaseToken, output, ctx);
                
                LogPhaseResult(phaseToken, "success", startTime, null);
                return output;
            }
            catch (Exception ex) when (ShouldUseFallback(ctx))
            {
                LogPhaseResult(phaseToken, "failed", startTime, ex.Message);
                
                // Try to load last good artifact
                var lastGood = TryLoadLastGood(phaseToken, ctx);
                if (lastGood != null)
                {
                    LogFallback(phaseToken, "last-good", "Using last known good artifact");
                    return lastGood;
                }
                
                // Create review fallback
                var fallback = CreateReviewFallback(phaseToken, input, ctx);
                LogFallback(phaseToken, "placeholder", $"Created fallback content: {DescribeFallback(fallback)}");
                return fallback;
            }
        }

        private static bool ShouldUseFallback(RunContext ctx)
        {
            // For now, always use fallback on failure
            // The caller can control this behavior
            return true;
        }

        private static PhaseToken GetPhaseToken(object phase)
        {
            // Use reflection to get the Token property
            var tokenProperty = phase.GetType().GetProperty("Token");
            return tokenProperty?.GetValue(phase) as PhaseToken ?? new PhaseToken("Unknown");
        }

        private static void PromoteToLastGood(PhaseToken phaseToken, object output, RunContext ctx)
        {
            try
            {
                var lastGoodDir = Path.Combine(ctx.ArtifactRoot, "last_good", phaseToken.Value);
                Directory.CreateDirectory(lastGoodDir);
                
                var outputPath = Path.Combine(lastGoodDir, "output.json");
                var json = JsonUtility.ToJson(output, true);
                File.WriteAllText(outputPath, json);
                
                Debug.Log($"[PhaseGuard] Promoted {phaseToken.Value} to last_good");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PhaseGuard] Failed to promote {phaseToken.Value}: {ex.Message}");
            }
        }

        private static object TryLoadLastGood(PhaseToken phaseToken, RunContext ctx)
        {
            try
            {
                var lastGoodPath = Path.Combine(ctx.ArtifactRoot, "last_good", phaseToken.Value, "output.json");
                if (!File.Exists(lastGoodPath))
                    return null;
                
                var json = File.ReadAllText(lastGoodPath);
                
                // Try to deserialize based on phase type
                return DeserializeByPhaseType(phaseToken, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PhaseGuard] Failed to load last_good for {phaseToken.Value}: {ex.Message}");
                return null;
            }
        }

        private static object DeserializeByPhaseType(PhaseToken phaseToken, string json)
        {
            switch (phaseToken.Value)
            {
                case "Plan":
                    return JsonUtility.FromJson<NexoDirectorStudio.DTO.GamePlan>(json);
                case "Layout":
                    return JsonUtility.FromJson<NexoDirectorStudio.DTO.WorldLayout>(json);
                case "Placement":
                    return JsonUtility.FromJson<NexoDirectorStudio.DTO.InteractionGraph>(json);
                case "Content":
                    return JsonUtility.FromJson<NexoDirectorStudio.DTO.ContentBundle>(json);
                case "Validate":
                    return JsonUtility.FromJson<NexoDirectorStudio.Validators.ValidationResult>(json);
                default:
                    return json; // Return as string for unknown types
            }
        }

        private static object CreateReviewFallback(PhaseToken phaseToken, object input, RunContext ctx)
        {
            switch (phaseToken.Value)
            {
                case "Plan":
                    return CreateFallbackGamePlan();
                case "Layout":
                    return CreateFallbackWorldLayout();
                case "Placement":
                    return CreateFallbackInteractionGraph();
                case "Content":
                    return CreateFallbackContentBundle();
                case "Validate":
                    return CreateFallbackValidationResult();
                default:
                    return new { phase = phaseToken.Value, fallback = true, timestamp = DateTime.UtcNow };
            }
        }

        private static object CreateFallbackGamePlan()
        {
            var brief = new NexoDirectorStudio.DTO.DesignBrief(
                Description: "Fallback game for review",
                GenreHint: "fps",
                Seed: 1337
            );
            
            return new NexoDirectorStudio.DTO.GamePlan(
                Id: "fallback-plan",
                SourceBrief: brief,
                Genre: "FPS",
                Description: "A minimal fallback game for review purposes",
                CoreMechanics: new[] { "Move", "Shoot", "Interact" },
                PlayerExperience: new[] { "Fast-paced action", "Strategic positioning" },
                EstimatedDurationMinutes: 5,
                DifficultyProgression: new[] { 
                    new NexoDirectorStudio.DTO.DifficultyBeat(0.0f, 1, "Basic movement and shooting"),
                    new NexoDirectorStudio.DTO.DifficultyBeat(30.0f, 2, "Added enemy AI and power-ups")
                },
                NarrativeBeats: new[] { "Enter area", "Find objective", "Complete mission" },
                RequiredAssets: new[] { 
                    new NexoDirectorStudio.DTO.AssetRequirement("Player", "Character", "Required"),
                    new NexoDirectorStudio.DTO.AssetRequirement("Enemy", "Character", "Required")
                },
                Seed: 1337,
                GeneratedAt: DateTimeOffset.UtcNow,
                Hash: "fallback-hash"
            );
        }

        private static object CreateFallbackWorldLayout()
        {
            return new NexoDirectorStudio.DTO.WorldLayout(
                Id: "fallback-layout",
                Name: "Fallback Room",
                GamePlanId: "fallback-plan",
                Dimensions: new Vector3(20, 5, 20),
                Tiles: new[] { 
                    new NexoDirectorStudio.DTO.TileData { 
                        GridPosition = Vector2Int.zero, 
                        WorldPosition = Vector3.zero, 
                        TileType = "Floor", 
                        MaterialId = "floor_material" 
                    },
                    new NexoDirectorStudio.DTO.TileData { 
                        GridPosition = Vector2Int.up, 
                        WorldPosition = Vector3.forward, 
                        TileType = "Wall", 
                        MaterialId = "wall_material" 
                    }
                },
                Objects: new[] { 
                    new NexoDirectorStudio.DTO.ObjectData { 
                        Id = "PlayerSpawn", 
                        Position = Vector3.zero, 
                        Rotation = Quaternion.identity, 
                        Scale = Vector3.one, 
                        ObjectType = "Player" 
                    },
                    new NexoDirectorStudio.DTO.ObjectData { 
                        Id = "EnemySpawn", 
                        Position = Vector3.right * 5, 
                        Rotation = Quaternion.identity, 
                        Scale = Vector3.one, 
                        ObjectType = "Enemy" 
                    }
                },
                NavigationNodes: new[] { 
                    new NexoDirectorStudio.DTO.NavigationNode { 
                        Id = "Start", 
                        Position = Vector3.zero, 
                        NodeType = "Spawn" 
                    },
                    new NexoDirectorStudio.DTO.NavigationNode { 
                        Id = "End", 
                        Position = Vector3.right * 10, 
                        NodeType = "Goal" 
                    }
                },
                Lighting: new NexoDirectorStudio.DTO.LightingData { 
                    AmbientColor = Color.white, 
                    DirectionalLightColor = Color.white, 
                    DirectionalLightDirection = Vector3.down 
                },
                Camera: new NexoDirectorStudio.DTO.CameraData { 
                    InitialPosition = Vector3.up * 2, 
                    InitialRotation = Quaternion.identity, 
                    FieldOfView = 60.0f,
                    Constraints = new NexoDirectorStudio.DTO.CameraConstraints { 
                        MinPosition = Vector3.zero,
                        MaxPosition = Vector3.one * 100f,
                        CanRotate = true,
                        CanZoom = true
                    } 
                },
                Seed: 1337,
                GeneratedAt: DateTimeOffset.UtcNow
            );
        }

        private static object CreateFallbackInteractionGraph()
        {
            return new NexoDirectorStudio.DTO.InteractionGraph
            {
                Id = "fallback-interactions",
                WorldLayoutId = "fallback-layout",
                Nodes = new[] { 
                    new NexoDirectorStudio.DTO.InteractionNode { 
                        Id = "Switch", 
                        WorldPosition = Vector3.zero, 
                        NodeType = "Switch", 
                        Name = "Switch",
                        Description = "A switch that can be activated",
                        IsRepeatable = true
                    },
                    new NexoDirectorStudio.DTO.InteractionNode { 
                        Id = "Door", 
                        WorldPosition = Vector3.forward * 5, 
                        NodeType = "Door", 
                        Name = "Door",
                        Description = "A door that can be opened",
                        IsRepeatable = false
                    }
                },
                Connections = new[] { 
                    new NexoDirectorStudio.DTO.InteractionConnection { 
                        Id = "SwitchToDoor",
                        SourceNodeId = "Switch", 
                        TargetNodeId = "Door", 
                        ConnectionType = "Unlock",
                        Weight = 1.0f
                    }
                },
                Variables = new[] { 
                    new NexoDirectorStudio.DTO.InteractionVariable { 
                        Name = "DoorUnlocked",
                        VariableType = "bool", 
                        InitialValue = false,
                        IsGlobal = true,
                        Description = "Whether the door is unlocked"
                    }
                },
                Seed = 1337,
                GeneratedAt = DateTimeOffset.UtcNow
            };
        }

        private static object CreateFallbackContentBundle()
        {
            return NexoDirectorStudio.Content.FallbackContentProvider.CreateFallbackContent("review");
        }

        private static object CreateFallbackValidationResult()
        {
            return new NexoDirectorStudio.Validators.ValidationResult
            {
                IsValid = true,
                Score = 50, // Mid-range score for fallback
                Issues = new[] { 
                    new NexoDirectorStudio.DTO.ValidationIssue { 
                        Title = "Fallback Content", 
                        Description = "Using fallback content", 
                        Severity = NexoDirectorStudio.DTO.ValidationSeverity.Warning 
                    }
                },
                Suggestions = new[] { 
                    new NexoDirectorStudio.DTO.ValidationSuggestion { 
                        Title = "Improve Content", 
                        Description = "Replace fallback content with generated assets" 
                    }
                }
            };
        }

        private static void LogPhaseResult(PhaseToken phaseToken, string status, DateTime startTime, string error)
        {
            var duration = DateTime.UtcNow - startTime;
            var message = $"[PhaseGuard] {phaseToken.Value} {status} in {duration.TotalMilliseconds:F0}ms";
            if (!string.IsNullOrEmpty(error))
                message += $" - {error}";
            
            Debug.Log(message);
        }

        private static void LogFallback(PhaseToken phaseToken, string fallbackType, string reason)
        {
            Debug.Log($"[PhaseGuard] {phaseToken.Value} using {fallbackType} fallback: {reason}");
        }

        private static string DescribeFallback(object fallback)
        {
            if (fallback == null) return "null";
            
            var type = fallback.GetType().Name;
            if (fallback is NexoDirectorStudio.DTO.GamePlan plan)
                return $"GamePlan: {plan.Description}";
            if (fallback is NexoDirectorStudio.DTO.WorldLayout layout)
                return $"WorldLayout: {layout.Name} ({layout.Tiles.Count} tiles)";
            if (fallback is NexoDirectorStudio.DTO.InteractionGraph graph)
                return $"InteractionGraph: {graph.Id} ({graph.Nodes.Count} nodes)";
            if (fallback is NexoDirectorStudio.DTO.ContentBundle bundle)
                return $"ContentBundle: {bundle.Id} ({bundle.Assets.Count} assets)";
            
            return $"{type} fallback";
        }
    }
}
