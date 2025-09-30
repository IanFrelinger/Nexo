using NexoDirectorStudio.DTO;
using UnityEngine;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Implementation of the build world layout command.
    /// Creates a spatial world layout from a game plan with grid-aligned tiles.
    /// </summary>
    public sealed class BuildWorldLayoutCommand : IBuildWorldLayoutCommand
    {
        public async ValueTask<WorldLayout> ExecuteAsync(IBuildWorldLayoutCommand.Input input, CancellationToken ct)
        {
            // TODO: Replace with real world generation in later phases
            // For now, create a deterministic world layout based on the game plan
            
            await Task.Delay(100, ct); // Simulate processing time
            
            var random = new Random(input.GamePlan.Seed);
            
            // Generate world dimensions based on genre and duration
            var dimensions = CalculateWorldDimensions(input.GamePlan);
            
            // Generate tile grid
            var tiles = GenerateTileGrid(dimensions, input.GamePlan, random);
            
            // Generate interactive objects
            var objects = GenerateObjects(input.GamePlan, random);
            
            // Generate navigation nodes
            var navigationNodes = GenerateNavigationNodes(input.GamePlan, random);
            
            // Generate lighting configuration
            var lighting = GenerateLighting(input.GamePlan, random);
            
            // Generate camera configuration
            var camera = GenerateCamera(input.GamePlan, random);
            
            var layout = new WorldLayout(
                Id: Guid.NewGuid().ToString(),
                Name: $"World for {input.GamePlan.Genre}",
                GamePlanId: input.GamePlan.Id,
                Dimensions: dimensions,
                Tiles: tiles,
                Objects: objects,
                NavigationNodes: navigationNodes,
                Lighting: lighting,
                Camera: camera,
                Seed: input.GamePlan.Seed,
                GeneratedAt: DateTimeOffset.UtcNow
            );
            
            return layout;
        }
        
        private static Vector3 CalculateWorldDimensions(GamePlan plan)
        {
            // Calculate world size based on genre and duration
            var baseSize = plan.EstimatedDurationMinutes * 10f; // 10 units per minute
            
            return plan.Genre switch
            {
                "FPS" => new Vector3(baseSize, 5f, baseSize), // Wide and open
                "Platformer" => new Vector3(baseSize * 0.8f, baseSize * 0.6f, 2f), // Tall and narrow
                "RPG" => new Vector3(baseSize, 3f, baseSize), // Balanced
                _ => new Vector3(baseSize, 5f, baseSize) // Default
            };
        }
        
        private static IReadOnlyList<TileData> GenerateTileGrid(Vector3 dimensions, GamePlan plan, Random random)
        {
            var tiles = new List<TileData>();
            var gridSize = new Vector2Int(
                Mathf.CeilToInt(dimensions.x),
                Mathf.CeilToInt(dimensions.z)
            );
            
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int z = 0; z < gridSize.y; z++)
                {
                    var gridPos = new Vector2Int(x, z);
                    var worldPos = new Vector3(x, 0, z);
                    
                    // Determine tile type based on position and genre
                    var tileType = DetermineTileType(gridPos, gridSize, plan, random);
                    var materialId = DetermineMaterialId(tileType, plan.Genre);
                    
                    tiles.Add(new TileData
                    {
                        GridPosition = gridPos,
                        WorldPosition = worldPos,
                        TileType = tileType,
                        MaterialId = materialId,
                        IsWalkable = IsWalkableTileType(tileType),
                        BlocksLineOfSight = BlocksLineOfSightTileType(tileType),
                        Properties = GenerateTileProperties(tileType, plan, random)
                    });
                }
            }
            
            return tiles;
        }
        
        private static string DetermineTileType(Vector2Int gridPos, Vector2Int gridSize, GamePlan plan, Random random)
        {
            // Edge tiles are walls
            if (gridPos.x == 0 || gridPos.x == gridSize.x - 1 || 
                gridPos.y == 0 || gridPos.y == gridSize.y - 1)
            {
                return "Wall";
            }
            
            // Genre-specific tile generation
            return plan.Genre switch
            {
                "FPS" => GenerateFPSTileType(gridPos, gridSize, random),
                "Platformer" => GeneratePlatformerTileType(gridPos, gridSize, random),
                "RPG" => GenerateRPGTileType(gridPos, gridSize, random),
                _ => "Ground"
            };
        }
        
        private static string GenerateFPSTileType(Vector2Int gridPos, Vector2Int gridSize, Random random)
        {
            // FPS levels have more open spaces with cover objects
            var centerDistance = Vector2.Distance(gridPos, new Vector2(gridSize.x / 2f, gridSize.y / 2f));
            var maxDistance = Vector2.Distance(Vector2.zero, new Vector2(gridSize.x, gridSize.y)) / 2f;
            var normalizedDistance = centerDistance / maxDistance;
            
            if (normalizedDistance < 0.3f && random.NextDouble() < 0.1)
                return "Cover";
            
            return "Ground";
        }
        
        private static string GeneratePlatformerTileType(Vector2Int gridPos, Vector2Int gridSize, Random random)
        {
            // Platformer levels have platforms at various heights
            var height = gridPos.y;
            var maxHeight = gridSize.y;
            
            if (height < maxHeight * 0.2f)
                return "Ground";
            if (height < maxHeight * 0.6f && random.NextDouble() < 0.3)
                return "Platform";
            if (height > maxHeight * 0.8f && random.NextDouble() < 0.2)
                return "Hazard";
            
            return "Empty";
        }
        
        private static string GenerateRPGTileType(Vector2Int gridPos, Vector2Int gridSize, Random random)
        {
            // RPG levels have more varied terrain
            if (random.NextDouble() < 0.05)
                return "Obstacle";
            if (random.NextDouble() < 0.1)
                return "Decoration";
            
            return "Ground";
        }
        
        private static string DetermineMaterialId(string tileType, string genre)
        {
            return tileType switch
            {
                "Wall" => $"{genre}_Wall_Material",
                "Ground" => $"{genre}_Ground_Material",
                "Platform" => $"{genre}_Platform_Material",
                "Cover" => $"{genre}_Cover_Material",
                "Hazard" => $"{genre}_Hazard_Material",
                _ => $"{genre}_Default_Material"
            };
        }
        
        private static bool IsWalkableTileType(string tileType)
        {
            return tileType switch
            {
                "Ground" => true,
                "Platform" => true,
                "Wall" => false,
                "Cover" => true,
                "Hazard" => false,
                "Empty" => false,
                _ => true
            };
        }
        
        private static bool BlocksLineOfSightTileType(string tileType)
        {
            return tileType switch
            {
                "Wall" => true,
                "Cover" => true,
                _ => false
            };
        }
        
        private static IReadOnlyDictionary<string, object> GenerateTileProperties(string tileType, GamePlan plan, Random random)
        {
            var properties = new Dictionary<string, object>();
            
            switch (tileType)
            {
                case "Platform":
                    properties["JumpHeight"] = 2.0f + (float)random.NextDouble() * 2.0f;
                    properties["Width"] = 1.0f + (float)random.NextDouble() * 3.0f;
                    break;
                case "Hazard":
                    properties["Damage"] = 1;
                    properties["DamageType"] = "Contact";
                    break;
                case "Cover":
                    properties["CoverValue"] = 0.5f + (float)random.NextDouble() * 0.5f;
                    break;
            }
            
            return properties;
        }
        
        private static IReadOnlyList<ObjectData> GenerateObjects(GamePlan plan, Random random)
        {
            var objects = new List<ObjectData>();
            
            // Add spawn point
            objects.Add(new ObjectData
            {
                Position = new Vector3(1, 1, 1),
                ObjectType = "SpawnPoint",
                PrefabId = $"{plan.Genre}_PlayerSpawn",
                Properties = new Dictionary<string, object>
                {
                    ["Team"] = "Player",
                    ["SpawnDelay"] = 0.0f
                }
            });
            
            // Add goal/exit point
            objects.Add(new ObjectData
            {
                Position = new Vector3(plan.EstimatedDurationMinutes * 8, 1, 1),
                ObjectType = "Goal",
                PrefabId = $"{plan.Genre}_Goal",
                Properties = new Dictionary<string, object>
                {
                    ["GoalType"] = "Exit",
                    ["RequiredItems"] = new string[0]
                }
            });
            
            // Genre-specific objects
            switch (plan.Genre)
            {
                case "FPS":
                    AddFPSObjects(objects, plan, random);
                    break;
                case "Platformer":
                    AddPlatformerObjects(objects, plan, random);
                    break;
                case "RPG":
                    AddRPGObjects(objects, plan, random);
                    break;
            }
            
            return objects;
        }
        
        private static void AddFPSObjects(List<ObjectData> objects, GamePlan plan, Random random)
        {
            // Add enemy spawn points
            for (int i = 0; i < 3; i++)
            {
                objects.Add(new ObjectData
                {
                    Position = new Vector3(5 + i * 3, 1, 2 + i),
                    ObjectType = "EnemySpawn",
                    PrefabId = "FPS_EnemySpawn",
                    Properties = new Dictionary<string, object>
                    {
                        ["EnemyType"] = "Basic",
                        ["SpawnDelay"] = 2.0f + i
                    }
                });
            }
        }
        
        private static void AddPlatformerObjects(List<ObjectData> objects, GamePlan plan, Random random)
        {
            // Add collectibles
            for (int i = 0; i < 5; i++)
            {
                objects.Add(new ObjectData
                {
                    Position = new Vector3(2 + i * 2, 2 + i, 1),
                    ObjectType = "Collectible",
                    PrefabId = "Platformer_Collectible",
                    Properties = new Dictionary<string, object>
                    {
                        ["CollectibleType"] = "Coin",
                        ["Value"] = 10
                    }
                });
            }
        }
        
        private static void AddRPGObjects(List<ObjectData> objects, GamePlan plan, Random random)
        {
            // Add NPC spawn points
            objects.Add(new ObjectData
            {
                Position = new Vector3(3, 1, 3),
                ObjectType = "NPC",
                PrefabId = "RPG_NPC",
                Properties = new Dictionary<string, object>
                {
                    ["NPCType"] = "QuestGiver",
                    ["DialogTree"] = "TutorialQuest"
                }
            });
        }
        
        private static IReadOnlyList<NavigationNode> GenerateNavigationNodes(GamePlan plan, Random random)
        {
            var nodes = new List<NavigationNode>();
            
            // Add start node
            nodes.Add(new NavigationNode
            {
                Position = new Vector3(1, 1, 1),
                NodeType = "Start",
                Properties = new Dictionary<string, object>
                {
                    ["IsEntryPoint"] = true
                }
            });
            
            // Add goal node
            nodes.Add(new NavigationNode
            {
                Position = new Vector3(plan.EstimatedDurationMinutes * 8, 1, 1),
                NodeType = "Goal",
                Properties = new Dictionary<string, object>
                {
                    ["IsExitPoint"] = true
                }
            });
            
            // Add intermediate waypoints
            for (int i = 1; i < plan.EstimatedDurationMinutes; i++)
            {
                nodes.Add(new NavigationNode
                {
                    Position = new Vector3(i * 5, 1, 1),
                    NodeType = "Waypoint",
                    Properties = new Dictionary<string, object>
                    {
                        ["Checkpoint"] = true
                    }
                });
            }
            
            // Connect nodes
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                var currentNode = nodes[i];
                var nextNode = nodes[i + 1];
                
                nodes[i] = currentNode with
                {
                    ConnectedNodeIds = new[] { nextNode.Id }
                };
            }
            
            return nodes;
        }
        
        private static LightingData GenerateLighting(GamePlan plan, Random random)
        {
            return plan.Genre switch
            {
                "FPS" => new LightingData
                {
                    AmbientColor = new Color(0.3f, 0.3f, 0.4f),
                    AmbientIntensity = 0.4f,
                    DirectionalLightDirection = new Vector3(-0.5f, -1f, -0.3f),
                    DirectionalLightColor = new Color(1f, 0.9f, 0.8f),
                    DirectionalLightIntensity = 1.2f
                },
                "Platformer" => new LightingData
                {
                    AmbientColor = new Color(0.4f, 0.4f, 0.5f),
                    AmbientIntensity = 0.6f,
                    DirectionalLightDirection = new Vector3(0f, -1f, 0f),
                    DirectionalLightColor = new Color(1f, 1f, 0.9f),
                    DirectionalLightIntensity = 1.0f
                },
                "RPG" => new LightingData
                {
                    AmbientColor = new Color(0.5f, 0.4f, 0.3f),
                    AmbientIntensity = 0.5f,
                    DirectionalLightDirection = new Vector3(-0.3f, -1f, -0.2f),
                    DirectionalLightColor = new Color(1f, 0.8f, 0.6f),
                    DirectionalLightIntensity = 0.8f
                },
                _ => new LightingData()
            };
        }
        
        private static CameraData GenerateCamera(GamePlan plan, Random random)
        {
            return plan.Genre switch
            {
                "FPS" => new CameraData
                {
                    InitialPosition = new Vector3(1, 1.6f, 1),
                    InitialRotation = Quaternion.identity,
                    FieldOfView = 90f,
                    Constraints = new CameraConstraints
                    {
                        MinPosition = new Vector3(-10, 0, -10),
                        MaxPosition = new Vector3(100, 10, 100),
                        CanRotate = true,
                        CanZoom = false
                    }
                },
                "Platformer" => new CameraData
                {
                    InitialPosition = new Vector3(0, 5, -5),
                    InitialRotation = Quaternion.Euler(15, 0, 0),
                    FieldOfView = 60f,
                    Constraints = new CameraConstraints
                    {
                        MinPosition = new Vector3(-20, 2, -20),
                        MaxPosition = new Vector3(100, 20, 20),
                        CanRotate = false,
                        CanZoom = true
                    }
                },
                "RPG" => new CameraData
                {
                    InitialPosition = new Vector3(0, 3, -3),
                    InitialRotation = Quaternion.Euler(30, 0, 0),
                    FieldOfView = 75f,
                    Constraints = new CameraConstraints
                    {
                        MinPosition = new Vector3(-50, 1, -50),
                        MaxPosition = new Vector3(100, 20, 50),
                        CanRotate = true,
                        CanZoom = true
                    }
                },
                _ => new CameraData
                {
                    InitialPosition = new Vector3(0, 2, -2),
                    InitialRotation = Quaternion.Euler(45, 0, 0),
                    FieldOfView = 60f
                }
            };
        }
    }
}
