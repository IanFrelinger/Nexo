using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace SpaceDefender.Game
{
    /// <summary>
    /// Main game loop using Nexo's pipeline-first architecture
    /// Demonstrates universal composability and intelligent strategy selection
    /// </summary>
    public class GameLoop
    {
        private readonly IIterationStrategySelector _strategySelector;
        private readonly GameState _gameState;
        private readonly List<IGameSystem> _systems;
        private bool _isRunning;

        public GameLoop(IIterationStrategySelector strategySelector)
        {
            _strategySelector = strategySelector;
            _gameState = new GameState();
            _systems = new List<IGameSystem>();
            _isRunning = false;
        }

        /// <summary>
        /// Initialize the game loop with all systems
        /// This demonstrates Nexo's pipeline composition
        /// </summary>
        public void Initialize()
        {
            // Create the game pipeline using Nexo's iteration strategies
            var context = new IterationContext
            {
                DataSize = 1000, // Estimated frame data size
                Requirements = new IterationRequirements
                {
                    PrioritizeCpu = true, // Game loops need high CPU performance
                    PrioritizeMemory = false,
                    RequiresParallelization = true // Multi-threaded game systems
                },
                EnvironmentProfile = new EnvironmentProfile
                {
                    CurrentPlatform = PlatformType.Desktop,
                    AvailableMemory = 1024 * 1024 * 1024, // 1GB
                    CpuCores = Environment.ProcessorCount
                }
            };

            // Select optimal strategy for game loop iteration
            var strategy = _strategySelector.SelectStrategy<GameFrame>(context);
            
            // Register game systems in order of execution
            RegisterSystems();
            
            Console.WriteLine($"Game initialized with {_systems.Count} systems using {strategy.StrategyId} strategy");
        }

        /// <summary>
        /// Register all game systems in execution order
        /// Demonstrates Nexo's dependency injection and system composition
        /// </summary>
        private void RegisterSystems()
        {
            _systems.Add(new InputSystem());
            _systems.Add(new PhysicsSystem());
            _systems.Add(new AISystem());
            _systems.Add(new CollisionSystem());
            _systems.Add(new RenderSystem());
            _systems.Add(new AudioSystem());
            _systems.Add(new UISystem());
        }

        /// <summary>
        /// Start the main game loop
        /// Demonstrates real-time performance and monitoring
        /// </summary>
        public async Task StartAsync()
        {
            _isRunning = true;
            var frameCount = 0;
            var lastTime = DateTime.UtcNow;
            var targetFPS = 60;
            var frameTime = 1000.0 / targetFPS; // 16.67ms per frame

            Console.WriteLine("🚀 Space Defender Game Started!");
            Console.WriteLine("🎮 Controls: WASD to move, Space to shoot, ESC to pause");

            while (_isRunning)
            {
                var currentTime = DateTime.UtcNow;
                var deltaTime = (currentTime - lastTime).TotalMilliseconds;

                if (deltaTime >= frameTime)
                {
                    await UpdateFrameAsync(deltaTime);
                    frameCount++;
                    lastTime = currentTime;

                    // Show FPS every 60 frames (1 second at 60 FPS)
                    if (frameCount % 60 == 0)
                    {
                        var fps = 1000.0 / deltaTime;
                        Console.WriteLine($"🎯 FPS: {fps:F1} | Score: {_gameState.Score} | Lives: {_gameState.Lives}");
                    }
                }

                // Small delay to prevent 100% CPU usage
                await Task.Delay(1);
            }
        }

        /// <summary>
        /// Update a single game frame
        /// Demonstrates Nexo's intelligent iteration strategies
        /// </summary>
        private async Task UpdateFrameAsync(double deltaTime)
        {
            var frame = new GameFrame
            {
                DeltaTime = deltaTime,
                GameState = _gameState,
                Timestamp = DateTime.UtcNow
            };

            // Update all systems in order
            foreach (var system in _systems)
            {
                await system.UpdateAsync(frame);
            }

            // Check for game over conditions
            if (_gameState.Lives <= 0)
            {
                await GameOverAsync();
            }
        }

        /// <summary>
        /// Stop the game loop
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            Console.WriteLine("🛑 Game stopped");
        }

        /// <summary>
        /// Handle game over
        /// </summary>
        private async Task GameOverAsync()
        {
            _isRunning = false;
            Console.WriteLine($"💀 Game Over! Final Score: {_gameState.Score}");
            Console.WriteLine("🔄 Press R to restart or ESC to exit");
        }

        /// <summary>
        /// Get current game state for external access
        /// </summary>
        public GameState GetGameState() => _gameState;
    }

    /// <summary>
    /// Represents a single game frame
    /// Used with Nexo's iteration strategies for optimal performance
    /// </summary>
    public class GameFrame
    {
        public double DeltaTime { get; set; }
        public GameState GameState { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// Current state of the game
    /// </summary>
    public class GameState
    {
        public int Score { get; set; } = 0;
        public int Lives { get; set; } = 3;
        public int Wave { get; set; } = 1;
        public bool IsPaused { get; set; } = false;
        public bool IsGameOver { get; set; } = false;
        public List<GameObject> GameObjects { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// Base interface for all game systems
    /// Demonstrates Nexo's interface-based architecture
    /// </summary>
    public interface IGameSystem
    {
        Task UpdateAsync(GameFrame frame);
        string Name { get; }
        int Priority { get; }
    }

    /// <summary>
    /// Base class for game objects
    /// </summary>
    public class GameObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public float X { get; set; }
        public float Y { get; set; }
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
