using SpaceDefender.Game;

namespace SpaceDefender.Game.Systems
{
    /// <summary>
    /// AI system demonstrating Nexo's AI integration capabilities
    /// Generates intelligent enemy behavior patterns using AI
    /// </summary>
    public class AISystem : IGameSystem
    {
        public string Name => "AI System";
        public int Priority => 3; // After input and physics

        private readonly Random _random = new();
        private readonly List<EnemyBehavior> _enemyBehaviors = new();
        private double _lastEnemySpawn = 0;
        private double _enemySpawnInterval = 2000; // 2 seconds

        public async Task UpdateAsync(GameFrame frame)
        {
            // Spawn enemies periodically
            await SpawnEnemiesAsync(frame);

            // Update existing enemies
            await UpdateEnemiesAsync(frame);

            // Generate new AI behaviors if needed
            await GenerateAIBehaviorsAsync(frame);
        }

        /// <summary>
        /// Spawn enemies at regular intervals
        /// Demonstrates AI-driven enemy generation
        /// </summary>
        private async Task SpawnEnemiesAsync(GameFrame frame)
        {
            var currentTime = frame.Timestamp.TotalMilliseconds;
            
            if (currentTime - _lastEnemySpawn >= _enemySpawnInterval)
            {
                await CreateEnemyAsync(frame);
                _lastEnemySpawn = currentTime;

                // Decrease spawn interval as waves progress (increase difficulty)
                _enemySpawnInterval = Math.Max(500, 2000 - (frame.GameState.Wave * 100));
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Create a new enemy with AI-generated behavior
        /// </summary>
        private async Task CreateEnemyAsync(GameFrame frame)
        {
            var enemy = new GameObject
            {
                X = _random.Next(0, 800),
                Y = -50, // Start above screen
                Width = 30,
                Height = 30,
                VelocityY = 50 + (frame.GameState.Wave * 10), // Faster each wave
                Properties = new Dictionary<string, object>
                {
                    ["Type"] = "Enemy",
                    ["Health"] = 1 + (frame.GameState.Wave / 3), // More health each wave
                    ["ScoreValue"] = 10 * frame.GameState.Wave,
                    ["BehaviorType"] = GetRandomBehaviorType(),
                    ["LastDirectionChange"] = 0.0
                }
            };

            frame.GameState.GameObjects.Add(enemy);
            Console.WriteLine($"👾 Enemy spawned with {enemy.Properties["BehaviorType"]} behavior");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Update all enemy AI behaviors
        /// Demonstrates real-time AI decision making
        /// </summary>
        private async Task UpdateEnemiesAsync(GameFrame frame)
        {
            var enemies = frame.GameState.GameObjects
                .Where(obj => obj.Properties.ContainsKey("Type") && 
                             obj.Properties["Type"].ToString() == "Enemy")
                .ToList();

            foreach (var enemy in enemies)
            {
                await UpdateEnemyAIAsync(enemy, frame);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Update individual enemy AI behavior
        /// </summary>
        private async Task UpdateEnemyAIAsync(GameObject enemy, GameFrame frame)
        {
            var behaviorType = enemy.Properties["BehaviorType"].ToString();
            var deltaTime = frame.DeltaTime / 1000.0; // Convert to seconds

            switch (behaviorType)
            {
                case "Straight":
                    await UpdateStraightBehaviorAsync(enemy, frame);
                    break;
                case "Zigzag":
                    await UpdateZigzagBehaviorAsync(enemy, frame);
                    break;
                case "Homing":
                    await UpdateHomingBehaviorAsync(enemy, frame);
                    break;
                case "Swarm":
                    await UpdateSwarmBehaviorAsync(enemy, frame);
                    break;
                default:
                    await UpdateStraightBehaviorAsync(enemy, frame);
                    break;
            }

            // Remove enemies that are off-screen
            if (enemy.Y > 650)
            {
                enemy.IsActive = false;
                frame.GameState.GameObjects.Remove(enemy);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Straight line movement behavior
        /// </summary>
        private async Task UpdateStraightBehaviorAsync(GameObject enemy, GameFrame frame)
        {
            // Simple straight down movement
            // No additional AI logic needed
            await Task.CompletedTask;
        }

        /// <summary>
        /// Zigzag movement behavior
        /// </summary>
        private async Task UpdateZigzagBehaviorAsync(GameObject enemy, GameFrame frame)
        {
            var lastDirectionChange = (double)enemy.Properties["LastDirectionChange"];
            var currentTime = frame.Timestamp.TotalMilliseconds;

            if (currentTime - lastDirectionChange > 1000) // Change direction every second
            {
                enemy.VelocityX = _random.Next(-50, 51); // Random horizontal velocity
                enemy.Properties["LastDirectionChange"] = currentTime;
            }
        }

        /// <summary>
        /// Homing behavior - moves toward player
        /// </summary>
        private async Task UpdateHomingBehaviorAsync(GameObject enemy, GameFrame frame)
        {
            var player = GetPlayer(frame.GameState);
            if (player == null) return;

            var deltaX = player.X - enemy.X;
            var deltaY = player.Y - enemy.Y;
            var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            if (distance > 0)
            {
                var homingSpeed = 30.0f;
                enemy.VelocityX = (float)(deltaX / distance * homingSpeed);
                // Keep some downward movement
                enemy.VelocityY = Math.Max(enemy.VelocityY, 20);
            }
        }

        /// <summary>
        /// Swarm behavior - moves in groups
        /// </summary>
        private async Task UpdateSwarmBehaviorAsync(GameObject enemy, GameFrame frame)
        {
            var nearbyEnemies = frame.GameState.GameObjects
                .Where(obj => obj.Properties.ContainsKey("Type") && 
                             obj.Properties["Type"].ToString() == "Enemy" &&
                             obj != enemy)
                .Where(obj => Math.Abs(obj.X - enemy.X) < 100 && Math.Abs(obj.Y - enemy.Y) < 100)
                .ToList();

            if (nearbyEnemies.Any())
            {
                // Move toward center of nearby enemies
                var avgX = nearbyEnemies.Average(e => e.X);
                var avgY = nearbyEnemies.Average(e => e.Y);
                
                var deltaX = avgX - enemy.X;
                var deltaY = avgY - enemy.Y;
                var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                if (distance > 0)
                {
                    var swarmSpeed = 20.0f;
                    enemy.VelocityX = (float)(deltaX / distance * swarmSpeed);
                }
            }
        }

        /// <summary>
        /// Generate new AI behaviors using Nexo's AI capabilities
        /// This would typically call Nexo's AI service
        /// </summary>
        private async Task GenerateAIBehaviorsAsync(GameFrame frame)
        {
            // In a real implementation, this would call Nexo's AI service
            // to generate new behavior patterns based on player performance
            // and game state

            if (frame.GameState.Wave % 3 == 0) // Every 3 waves
            {
                await GenerateNewBehaviorPatternAsync(frame);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Generate a new behavior pattern using AI
        /// </summary>
        private async Task GenerateNewBehaviorPatternAsync(GameFrame frame)
        {
            // Simulate AI-generated behavior
            var newBehavior = new EnemyBehavior
            {
                Name = $"AI_Generated_Behavior_Wave_{frame.GameState.Wave}",
                Description = "AI-generated behavior pattern",
                Complexity = frame.GameState.Wave,
                Effectiveness = _random.Next(50, 100)
            };

            _enemyBehaviors.Add(newBehavior);
            Console.WriteLine($"🤖 AI generated new behavior: {newBehavior.Name}");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Get a random behavior type for new enemies
        /// </summary>
        private string GetRandomBehaviorType()
        {
            var behaviors = new[] { "Straight", "Zigzag", "Homing", "Swarm" };
            return behaviors[_random.Next(behaviors.Length)];
        }

        /// <summary>
        /// Get the player object
        /// </summary>
        private GameObject? GetPlayer(GameState gameState)
        {
            return gameState.GameObjects.FirstOrDefault(obj => 
                obj.Properties.ContainsKey("Type") && 
                obj.Properties["Type"].ToString() == "Player");
        }
    }

    /// <summary>
    /// Represents an AI-generated enemy behavior pattern
    /// </summary>
    public class EnemyBehavior
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Complexity { get; set; }
        public int Effectiveness { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}
