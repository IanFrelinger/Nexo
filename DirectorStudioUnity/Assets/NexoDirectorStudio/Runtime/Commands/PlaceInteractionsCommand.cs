using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Command to place interactions in the world layout.
    /// </summary>
    public class PlaceInteractionsCommand : IPlaceInteractionsCommand
    {
        public async Task<InteractionGraph> ExecuteAsync(IPlaceInteractionsCommand.Input input, CancellationToken cancellationToken)
        {
            // Generate FPS interaction graph based on world layout and game plan
            await Task.Delay(100, cancellationToken); // Simulate async work
            
            var random = new System.Random(input.WorldLayout.Seed);
            var nodes = GenerateFPSInteractionNodes(input.WorldLayout, input.GamePlan, random);
            var connections = GenerateFPSConnections(nodes, random);
            var variables = GenerateFPSVariables(input.GamePlan);
            var entryPoints = DetermineEntryPoints(nodes);
            
            return new InteractionGraph
            {
                Id = Guid.NewGuid().ToString(),
                WorldLayoutId = input.WorldLayout.Id,
                Nodes = nodes,
                Connections = connections,
                Variables = variables,
                EntryPointIds = entryPoints,
                Seed = input.WorldLayout.Seed,
                GeneratedAt = DateTimeOffset.UtcNow
            };
        }

        private List<InteractionNode> GenerateFPSInteractionNodes(WorldLayout world, GamePlan gamePlan, System.Random random)
        {
            var nodes = new List<InteractionNode>();
            
            // Enemy interaction nodes
            var enemyObjects = world.Objects.Where(o => o.ObjectType == "Enemy").ToList();
            foreach (var enemy in enemyObjects)
            {
                nodes.Add(new InteractionNode
                {
                    Id = $"Enemy_{enemy.Id}",
                    WorldPosition = enemy.Position,
                    NodeType = "Enemy",
                    Name = $"Enemy {enemy.Id}",
                    Description = "Hostile enemy unit"
                });
            }
            
            // Keycard interaction nodes
            var keycardObjects = world.Objects.Where(o => o.ObjectType == "Collectible").ToList();
            foreach (var keycard in keycardObjects)
            {
                nodes.Add(new InteractionNode
                {
                    Id = $"Keycard_{keycard.Id}",
                    WorldPosition = keycard.Position,
                    NodeType = "Collectible",
                    Name = $"Keycard {keycard.Id}",
                    Description = "Collectible keycard"
                });
            }
            
            // Door interaction nodes
            var doorObjects = world.Objects.Where(o => o.ObjectType == "Door").ToList();
            foreach (var door in doorObjects)
            {
                nodes.Add(new InteractionNode
                {
                    Id = $"Door_{door.Id}",
                    WorldPosition = door.Position,
                    NodeType = "Door",
                    Name = $"Door {door.Id}",
                    Description = "Locked door"
                });
            }
            
            // Boss interaction node
            var bossObjects = world.Objects.Where(o => o.ObjectType == "Boss").ToList();
            foreach (var boss in bossObjects)
            {
                nodes.Add(new InteractionNode
                {
                    Id = $"Boss_{boss.Id}",
                    WorldPosition = boss.Position,
                    NodeType = "Boss",
                    Name = $"Boss {boss.Id}",
                    Description = "Final boss enemy"
                });
            }
            
            // Power-up nodes
            for (int i = 0; i < 3; i++)
            {
                var x = 5 + random.Next(0, 10);
                var z = 5 + random.Next(0, 10);
                
                nodes.Add(new InteractionNode
                {
                    Id = $"PowerUp_{i}",
                    WorldPosition = new Vector3(x, 1, z),
                    NodeType = "PowerUp",
                    Name = $"PowerUp {i}",
                    Description = "Health or ammo pickup"
                });
            }
            
            return nodes;
        }

        private List<InteractionConnection> GenerateFPSConnections(List<InteractionNode> nodes, System.Random random)
        {
            var connections = new List<InteractionConnection>();
            
            // Connect enemies to power-ups (enemies drop power-ups when killed)
            var enemies = nodes.Where(n => n.NodeType == "Enemy").ToList();
            var powerUps = nodes.Where(n => n.NodeType == "PowerUp").ToList();
            
            foreach (var enemy in enemies)
            {
                if (powerUps.Any())
                {
                    var powerUp = powerUps[random.Next(powerUps.Count)];
                    connections.Add(new InteractionConnection
                    {
                        Id = $"EnemyToPowerUp_{enemy.Id}_{powerUp.Id}",
                        SourceNodeId = enemy.Id,
                        TargetNodeId = powerUp.Id,
                        ConnectionType = "Drop"
                    });
                }
            }
            
            // Connect keycards to doors
            var keycards = nodes.Where(n => n.NodeType == "Collectible").ToList();
            var doors = nodes.Where(n => n.NodeType == "Door").ToList();
            
            foreach (var keycard in keycards)
            {
                if (doors.Any())
                {
                    var door = doors[random.Next(doors.Count)];
                    connections.Add(new InteractionConnection
                    {
                        Id = $"KeycardToDoor_{keycard.Id}_{door.Id}",
                        SourceNodeId = keycard.Id,
                        TargetNodeId = door.Id,
                        ConnectionType = "Unlock"
                    });
                }
            }
            
            return connections;
        }

        private List<InteractionVariable> GenerateFPSVariables(GamePlan gamePlan)
        {
            return new List<InteractionVariable>
            {
                new InteractionVariable { Name = "PlayerHealth", InitialValue = 100, VariableType = "Int" },
                new InteractionVariable { Name = "PlayerAmmo", InitialValue = 30, VariableType = "Int" },
                new InteractionVariable { Name = "KeysCollected", InitialValue = 0, VariableType = "Int" },
                new InteractionVariable { Name = "EnemiesKilled", InitialValue = 0, VariableType = "Int" },
                new InteractionVariable { Name = "BossDefeated", InitialValue = false, VariableType = "Bool" },
                new InteractionVariable { Name = "LevelComplete", InitialValue = false, VariableType = "Bool" },
                new InteractionVariable { Name = "Score", InitialValue = 0, VariableType = "Int" }
            };
        }

        private List<string> DetermineEntryPoints(List<InteractionNode> nodes)
        {
            return new List<string> { "PlayerSpawn" };
        }
    }
}
