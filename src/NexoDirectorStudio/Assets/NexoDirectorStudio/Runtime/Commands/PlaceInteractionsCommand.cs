using NexoDirectorStudio.DTO;
using UnityEngine;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Implementation of the place interactions command.
    /// Creates an interaction graph with triggers, events, and quests from a world layout.
    /// </summary>
    public sealed class PlaceInteractionsCommand : IPlaceInteractionsCommand
    {
        public async ValueTask<InteractionGraph> ExecuteAsync(WorldLayout input, CancellationToken ct)
        {
            // TODO: Replace with real interaction generation in later phases
            // For now, create a deterministic interaction graph based on the world layout
            
            await Task.Delay(100, ct); // Simulate processing time
            
            var random = new Random(input.Seed);
            
            // Generate interaction nodes based on world layout
            var nodes = GenerateInteractionNodes(input, random);
            
            // Generate connections between nodes
            var connections = GenerateConnections(nodes, random);
            
            // Generate global variables
            var variables = GenerateVariables(input, random);
            
            // Determine entry points
            var entryPointIds = DetermineEntryPoints(nodes);
            
            var graph = new InteractionGraph
            {
                WorldLayoutId = input.Id,
                Nodes = nodes,
                Connections = connections,
                Variables = variables,
                EntryPointIds = entryPointIds,
                Seed = input.Seed
            };
            
            return graph;
        }
        
        private static IReadOnlyList<InteractionNode> GenerateInteractionNodes(WorldLayout layout, Random random)
        {
            var nodes = new List<InteractionNode>();
            
            // Add spawn trigger
            nodes.Add(new InteractionNode
            {
                NodeType = "Spawn",
                WorldPosition = new Vector3(1, 1, 1),
                Name = "Player Spawn",
                Description = "Spawns the player at the start of the level",
                Actions = new[]
                {
                    new InteractionAction
                    {
                        ActionType = "SpawnObject",
                        Parameters = new Dictionary<string, object>
                        {
                            ["ObjectType"] = "Player",
                            ["Position"] = new Vector3(1, 1, 1)
                        }
                    }
                },
                IsRepeatable = false,
                Priority = 10
            });
            
            // Add goal trigger
            nodes.Add(new InteractionNode
            {
                NodeType = "Goal",
                WorldPosition = new Vector3(layout.Dimensions.x - 1, 1, 1),
                Name = "Level Goal",
                Description = "Completes the level when reached",
                Conditions = new[]
                {
                    new InteractionCondition
                    {
                        ConditionType = "PlayerAtPosition",
                        Parameters = new Dictionary<string, object>
                        {
                            ["Position"] = new Vector3(layout.Dimensions.x - 1, 1, 1),
                            ["Radius"] = 2.0f
                        }
                    }
                },
                Actions = new[]
                {
                    new InteractionAction
                    {
                        ActionType = "SetVariable",
                        Parameters = new Dictionary<string, object>
                        {
                            ["VariableName"] = "LevelComplete",
                            ["Value"] = true
                        }
                    },
                    new InteractionAction
                    {
                        ActionType = "ShowDialog",
                        Parameters = new Dictionary<string, object>
                        {
                            ["Message"] = "Level Complete!",
                            ["Duration"] = 3.0f
                        }
                    }
                },
                IsRepeatable = false,
                Priority = 5
            });
            
            // Add collectibles based on genre
            var collectibleCount = DetermineCollectibleCount(layout);
            for (int i = 0; i < collectibleCount; i++)
            {
                var position = GenerateCollectiblePosition(layout, random);
                nodes.Add(new InteractionNode
                {
                    NodeType = "Collectible",
                    WorldPosition = position,
                    Name = $"Collectible {i + 1}",
                    Description = "A collectible item",
                    Conditions = new[]
                    {
                        new InteractionCondition
                        {
                            ConditionType = "PlayerAtPosition",
                            Parameters = new Dictionary<string, object>
                            {
                                ["Position"] = position,
                                ["Radius"] = 1.0f
                            }
                        }
                    },
                    Actions = new[]
                    {
                        new InteractionAction
                        {
                            ActionType = "SetVariable",
                            Parameters = new Dictionary<string, object>
                            {
                                ["VariableName"] = "CollectiblesFound",
                                ["Value"] = i + 1
                            }
                        },
                        new InteractionAction
                        {
                            ActionType = "PlayAudio",
                            Parameters = new Dictionary<string, object>
                            {
                                ["AudioClip"] = "CollectibleSound",
                                ["Volume"] = 0.8f
                            }
                        },
                        new InteractionAction
                        {
                            ActionType = "DestroyObject",
                            Parameters = new Dictionary<string, object>
                            {
                                ["ObjectId"] = $"Collectible_{i + 1}"
                            }
                        }
                    },
                    IsRepeatable = false,
                    Priority = 3
                });
            }
            
            // Add enemy spawns for combat genres
            if (ShouldAddEnemySpawns(layout))
            {
                var enemyCount = DetermineEnemyCount(layout);
                for (int i = 0; i < enemyCount; i++)
                {
                    var position = GenerateEnemyPosition(layout, random);
                    nodes.Add(new InteractionNode
                    {
                        NodeType = "EnemySpawn",
                        WorldPosition = position,
                        Name = $"Enemy Spawn {i + 1}",
                        Description = "Spawns an enemy",
                        Conditions = new[]
                        {
                            new InteractionCondition
                            {
                                ConditionType = "PlayerNearby",
                                Parameters = new Dictionary<string, object>
                                {
                                    ["Position"] = position,
                                    ["Radius"] = 5.0f
                                }
                            }
                        },
                        Actions = new[]
                        {
                            new InteractionAction
                            {
                                ActionType = "SpawnObject",
                                Parameters = new Dictionary<string, object>
                                {
                                    ["ObjectType"] = "Enemy",
                                    ["Position"] = position
                                }
                            }
                        },
                        IsRepeatable = true,
                        Priority = 2
                    });
                }
            }
            
            // Add environmental triggers
            var environmentalCount = DetermineEnvironmentalCount(layout);
            for (int i = 0; i < environmentalCount; i++)
            {
                var position = GenerateEnvironmentalPosition(layout, random);
                nodes.Add(new InteractionNode
                {
                    NodeType = "Environmental",
                    WorldPosition = position,
                    Name = $"Environmental Trigger {i + 1}",
                    Description = "An environmental interaction",
                    Conditions = new[]
                    {
                        new InteractionCondition
                        {
                            ConditionType = "PlayerAtPosition",
                            Parameters = new Dictionary<string, object>
                            {
                                ["Position"] = position,
                                ["Radius"] = 1.5f
                            }
                        }
                    },
                    Actions = new[]
                    {
                        new InteractionAction
                        {
                            ActionType = "PlayAudio",
                            Parameters = new Dictionary<string, object>
                            {
                                ["AudioClip"] = "EnvironmentalSound",
                                ["Volume"] = 0.5f
                            }
                        },
                        new InteractionAction
                        {
                            ActionType = "ShowDialog",
                            Parameters = new Dictionary<string, object>
                            {
                                ["Message"] = "You feel a mysterious presence...",
                                ["Duration"] = 2.0f
                            }
                        }
                    },
                    IsRepeatable = true,
                    Priority = 1
                });
            }
            
            return nodes;
        }
        
        private static IReadOnlyList<InteractionConnection> GenerateConnections(IReadOnlyList<InteractionNode> nodes, Random random)
        {
            var connections = new List<InteractionConnection>();
            
            // Connect spawn to first collectible
            var spawnNode = nodes.FirstOrDefault(n => n.NodeType == "Spawn");
            var firstCollectible = nodes.FirstOrDefault(n => n.NodeType == "Collectible");
            
            if (spawnNode != null && firstCollectible != null)
            {
                connections.Add(new InteractionConnection
                {
                    SourceNodeId = spawnNode.Id,
                    TargetNodeId = firstCollectible.Id,
                    ConnectionType = "Success",
                    Weight = 1.0f
                });
            }
            
            // Connect collectibles in sequence
            var collectibles = nodes.Where(n => n.NodeType == "Collectible").ToList();
            for (int i = 0; i < collectibles.Count - 1; i++)
            {
                connections.Add(new InteractionConnection
                {
                    SourceNodeId = collectibles[i].Id,
                    TargetNodeId = collectibles[i + 1].Id,
                    ConnectionType = "Success",
                    Weight = 1.0f
                });
            }
            
            // Connect last collectible to goal
            var lastCollectible = collectibles.LastOrDefault();
            var goalNode = nodes.FirstOrDefault(n => n.NodeType == "Goal");
            
            if (lastCollectible != null && goalNode != null)
            {
                connections.Add(new InteractionConnection
                {
                    SourceNodeId = lastCollectible.Id,
                    TargetNodeId = goalNode.Id,
                    ConnectionType = "Success",
                    Weight = 1.0f
                });
            }
            
            return connections;
        }
        
        private static IReadOnlyList<InteractionVariable> GenerateVariables(WorldLayout layout, Random random)
        {
            var variables = new List<InteractionVariable>
            {
                new InteractionVariable
                {
                    Name = "LevelComplete",
                    VariableType = "Bool",
                    InitialValue = false,
                    IsGlobal = true,
                    Description = "Whether the level is complete"
                },
                new InteractionVariable
                {
                    Name = "CollectiblesFound",
                    VariableType = "Int",
                    InitialValue = 0,
                    IsGlobal = true,
                    Description = "Number of collectibles found"
                },
                new InteractionVariable
                {
                    Name = "PlayerHealth",
                    VariableType = "Int",
                    InitialValue = 100,
                    IsGlobal = true,
                    Description = "Player's current health"
                },
                new InteractionVariable
                {
                    Name = "Score",
                    VariableType = "Int",
                    InitialValue = 0,
                    IsGlobal = true,
                    Description = "Player's current score"
                }
            };
            
            return variables;
        }
        
        private static IReadOnlyList<string> DetermineEntryPoints(IReadOnlyList<InteractionNode> nodes)
        {
            var spawnNode = nodes.FirstOrDefault(n => n.NodeType == "Spawn");
            return spawnNode != null ? new[] { spawnNode.Id } : Array.Empty<string>();
        }
        
        private static int DetermineCollectibleCount(WorldLayout layout)
        {
            // Base count on world size
            var baseCount = Mathf.CeilToInt(layout.Dimensions.x * layout.Dimensions.z / 100f);
            return Mathf.Clamp(baseCount, 1, 10);
        }
        
        private static Vector3 GenerateCollectiblePosition(WorldLayout layout, Random random)
        {
            var x = (float)(random.NextDouble() * (layout.Dimensions.x - 2) + 1);
            var z = (float)(random.NextDouble() * (layout.Dimensions.z - 2) + 1);
            return new Vector3(x, 1, z);
        }
        
        private static bool ShouldAddEnemySpawns(WorldLayout layout)
        {
            // Add enemies for combat-focused genres
            return layout.Dimensions.x > 10 && layout.Dimensions.z > 10;
        }
        
        private static int DetermineEnemyCount(WorldLayout layout)
        {
            var baseCount = Mathf.CeilToInt(layout.Dimensions.x * layout.Dimensions.z / 200f);
            return Mathf.Clamp(baseCount, 0, 5);
        }
        
        private static Vector3 GenerateEnemyPosition(WorldLayout layout, Random random)
        {
            var x = (float)(random.NextDouble() * (layout.Dimensions.x - 4) + 2);
            var z = (float)(random.NextDouble() * (layout.Dimensions.z - 4) + 2);
            return new Vector3(x, 1, z);
        }
        
        private static int DetermineEnvironmentalCount(WorldLayout layout)
        {
            var baseCount = Mathf.CeilToInt(layout.Dimensions.x * layout.Dimensions.z / 300f);
            return Mathf.Clamp(baseCount, 0, 3);
        }
        
        private static Vector3 GenerateEnvironmentalPosition(WorldLayout layout, Random random)
        {
            var x = (float)(random.NextDouble() * (layout.Dimensions.x - 2) + 1);
            var z = (float)(random.NextDouble() * (layout.Dimensions.z - 2) + 1);
            return new Vector3(x, 1, z);
        }
    }
}
