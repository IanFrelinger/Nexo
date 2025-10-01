using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.DTO;
using UnityEngine;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Command to build world layout from a game plan.
    /// </summary>
    public class BuildWorldLayoutCommand : IBuildWorldLayoutCommand
    {
        public async Task<WorldLayout> ExecuteAsync(IBuildWorldLayoutCommand.Input input, CancellationToken cancellationToken)
        {
            // Generate FPS world layout based on game plan
            await Task.Delay(100, cancellationToken); // Simulate async work
            
            var random = new System.Random(input.GamePlan.Seed);
            var tiles = GenerateFPSWorldTiles(input.GamePlan, random);
            var objects = GenerateFPSWorldObjects(input.GamePlan, random);
            var navigationNodes = GenerateNavigationNodes(input.GamePlan, random);
            
            return new WorldLayout(
                Id: Guid.NewGuid().ToString(),
                Name: $"FPS Arena - {input.GamePlan.Description}",
                GamePlanId: input.GamePlan.Id,
                Dimensions: new Vector3(20, 10, 20), // Larger arena for FPS
                Tiles: tiles,
                Objects: objects,
                NavigationNodes: navigationNodes,
                Lighting: GenerateFPSLighting(input.GamePlan),
                Camera: GenerateFPSCamera(input.GamePlan),
                Seed: input.GamePlan.Seed,
                GeneratedAt: DateTimeOffset.UtcNow
            );
        }

        private List<TileData> GenerateFPSWorldTiles(GamePlan gamePlan, System.Random random)
        {
            var tiles = new List<TileData>();
            var width = 20;
            var height = 20;
            
            // Generate floor tiles
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    var tileType = "Ground";
                    var isWalkable = true;
                    var blocksLOS = false;
                    
                    // Create walls around the perimeter
                    if (x == 0 || x == width - 1 || z == 0 || z == height - 1)
                    {
                        tileType = "Wall";
                        isWalkable = false;
                        blocksLOS = true;
                    }
                    // Add some internal walls for cover
                    else if (random.NextDouble() < 0.1) // 10% chance for internal walls
                    {
                        tileType = "Wall";
                        isWalkable = false;
                        blocksLOS = true;
                    }
                    // Add platforms for vertical arenas
                    else if (gamePlan.CoreMechanics.Any(m => m.Contains("Vertical")) && random.NextDouble() < 0.05)
                    {
                        tileType = "Platform";
                        isWalkable = true;
                        blocksLOS = false;
                    }
                    
                    tiles.Add(new TileData
                    {
                        GridPosition = new Vector2Int(x, z),
                        WorldPosition = new Vector3(x * 2, 0, z * 2),
                        TileType = tileType,
                        MaterialId = GetMaterialForTileType(tileType),
                        IsWalkable = isWalkable,
                        BlocksLineOfSight = blocksLOS
                    });
                }
            }
            
            return tiles;
        }

        private List<ObjectData> GenerateFPSWorldObjects(GamePlan gamePlan, System.Random random)
        {
            var objects = new List<ObjectData>();
            
            // Spawn points
            objects.Add(new ObjectData
            {
                Id = "PlayerSpawn",
                Position = new Vector3(2, 1, 2),
                ObjectType = "SpawnPoint",
                PrefabId = "PlayerSpawn"
            });
            
            // Enemy spawn points - generate 5 enemies for FPS
            var enemyCount = 5;
            for (int i = 0; i < enemyCount; i++)
            {
                var angle = (2 * Math.PI * i) / enemyCount;
                var radius = 8 + random.Next(0, 4);
                var x = 10 + (float)(radius * Math.Cos(angle));
                var z = 10 + (float)(radius * Math.Sin(angle));
                
                objects.Add(new ObjectData
                {
                    Id = $"EnemySpawn_{i}",
                    Position = new Vector3(x, 1, z),
                    ObjectType = "Enemy",
                    PrefabId = $"Enemy_{i}"
                });
            }
            
            // Keycard spawn points
            if (gamePlan.CoreMechanics.Any(m => m.Contains("Keycard")))
            {
                var keycardTypes = new[] { "BlueKeycard", "RedKeycard", "YellowKeycard" };
                for (int i = 0; i < keycardTypes.Length; i++)
                {
                    var x = 5 + random.Next(0, 10);
                    var z = 5 + random.Next(0, 10);
                    
                    objects.Add(new ObjectData
                    {
                        Id = $"Keycard_{i}",
                        Position = new Vector3(x, 1, z),
                        ObjectType = "Collectible",
                        PrefabId = keycardTypes[i]
                    });
                }
            }
            
            // Boss spawn point
            if (gamePlan.CoreMechanics.Any(m => m.Contains("Boss")))
            {
                objects.Add(new ObjectData
                {
                    Id = "BossSpawn",
                    Position = new Vector3(18, 1, 18),
                    ObjectType = "Boss",
                    PrefabId = "Boss"
                });
            }
            
            // Doors
            if (gamePlan.CoreMechanics.Any(m => m.Contains("Door")))
            {
                objects.Add(new ObjectData
                {
                    Id = "Door1",
                    Position = new Vector3(10, 1, 19),
                    ObjectType = "Door",
                    PrefabId = "Door"
                });
            }
            
            return objects;
        }

        private List<NavigationNode> GenerateNavigationNodes(GamePlan gamePlan, System.Random random)
        {
            var nodes = new List<NavigationNode>();
            
            // Add waypoints around the arena
            var waypointCount = 8;
            for (int i = 0; i < waypointCount; i++)
            {
                var angle = (2 * Math.PI * i) / waypointCount;
                var radius = 6 + random.Next(0, 3);
                var x = 10 + (float)(radius * Math.Cos(angle));
                var z = 10 + (float)(radius * Math.Sin(angle));
                
                nodes.Add(new NavigationNode
                {
                    Id = $"Waypoint_{i}",
                    Position = new Vector3(x, 1, z),
                    NodeType = "Waypoint"
                });
            }
            
            return nodes;
        }

        private LightingData GenerateFPSLighting(GamePlan gamePlan)
        {
            return new LightingData
            {
                AmbientColor = Color.gray,
                AmbientIntensity = 0.3f,
                DirectionalLightDirection = new Vector3(-1, -1, -1).normalized,
                DirectionalLightColor = Color.white,
                DirectionalLightIntensity = 1.0f
            };
        }

        private CameraData GenerateFPSCamera(GamePlan gamePlan)
        {
            return new CameraData
            {
                InitialPosition = new Vector3(2, 1.6f, 2),
                InitialRotation = Quaternion.identity,
                FieldOfView = 75f,
                Constraints = new CameraConstraints
                {
                    CanRotate = true,
                    CanZoom = true
                }
            };
        }

        private string GetMaterialForTileType(string tileType)
        {
            return tileType switch
            {
                "Ground" => "Concrete",
                "Wall" => "Metal",
                "Platform" => "Steel",
                _ => "Default"
            };
        }
    }
}