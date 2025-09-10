using SpaceDefender.Game;

namespace SpaceDefender.Game.Systems
{
    /// <summary>
    /// Input handling system demonstrating cross-platform compatibility
    /// Supports keyboard, mouse, touch, and gamepad input
    /// </summary>
    public class InputSystem : IGameSystem
    {
        public string Name => "Input System";
        public int Priority => 1; // Highest priority - input must be processed first

        private readonly Dictionary<InputAction, bool> _currentInputs = new();
        private readonly Dictionary<InputAction, bool> _previousInputs = new();

        public async Task UpdateAsync(GameFrame frame)
        {
            // Store previous frame inputs for edge detection
            foreach (var kvp in _currentInputs)
            {
                _previousInputs[kvp.Key] = kvp.Value;
            }

            // Process current frame inputs
            await ProcessInputAsync();

            // Update game state based on inputs
            await UpdateGameStateAsync(frame);
        }

        /// <summary>
        /// Process input from all supported input methods
        /// Demonstrates Nexo's cross-platform input handling
        /// </summary>
        private async Task ProcessInputAsync()
        {
            // Keyboard input
            _currentInputs[InputAction.MoveUp] = IsKeyPressed(ConsoleKey.W) || IsKeyPressed(ConsoleKey.UpArrow);
            _currentInputs[InputAction.MoveDown] = IsKeyPressed(ConsoleKey.S) || IsKeyPressed(ConsoleKey.DownArrow);
            _currentInputs[InputAction.MoveLeft] = IsKeyPressed(ConsoleKey.A) || IsKeyPressed(ConsoleKey.LeftArrow);
            _currentInputs[InputAction.MoveRight] = IsKeyPressed(ConsoleKey.D) || IsKeyPressed(ConsoleKey.RightArrow);
            _currentInputs[InputAction.Shoot] = IsKeyPressed(ConsoleKey.Spacebar);
            _currentInputs[InputAction.Pause] = IsKeyPressed(ConsoleKey.Escape);
            _currentInputs[InputAction.Restart] = IsKeyPressed(ConsoleKey.R);

            // Mouse input (for future web/mobile support)
            // _currentInputs[InputAction.MouseClick] = IsMouseButtonPressed(MouseButton.Left);
            // _currentInputs[InputAction.MousePosition] = GetMousePosition();

            // Gamepad input (for console support)
            // _currentInputs[InputAction.GamepadA] = IsGamepadButtonPressed(GamepadButton.A);
            // _currentInputs[InputAction.GamepadB] = IsGamepadButtonPressed(GamepadButton.B);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Check if a key is currently pressed
        /// </summary>
        private bool IsKeyPressed(ConsoleKey key)
        {
            return Console.KeyAvailable && Console.ReadKey(true).Key == key;
        }

        /// <summary>
        /// Update game state based on input
        /// </summary>
        private async Task UpdateGameStateAsync(GameFrame frame)
        {
            var gameState = frame.GameState;

            // Handle pause toggle
            if (IsInputJustPressed(InputAction.Pause))
            {
                gameState.IsPaused = !gameState.IsPaused;
                Console.WriteLine(gameState.IsPaused ? "⏸️ Game Paused" : "▶️ Game Resumed");
            }

            // Handle restart
            if (IsInputJustPressed(InputAction.Restart) && gameState.IsGameOver)
            {
                await RestartGameAsync(gameState);
            }

            // Handle movement (only when not paused)
            if (!gameState.IsPaused)
            {
                await HandleMovementAsync(frame);
                await HandleShootingAsync(frame);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Handle player movement input
        /// </summary>
        private async Task HandleMovementAsync(GameFrame frame)
        {
            var player = GetPlayer(frame.GameState);
            if (player == null) return;

            const float moveSpeed = 200.0f; // pixels per second
            var deltaTime = (float)frame.DeltaTime / 1000.0f; // convert to seconds

            if (_currentInputs[InputAction.MoveUp])
            {
                player.Y -= moveSpeed * deltaTime;
            }
            if (_currentInputs[InputAction.MoveDown])
            {
                player.Y += moveSpeed * deltaTime;
            }
            if (_currentInputs[InputAction.MoveLeft])
            {
                player.X -= moveSpeed * deltaTime;
            }
            if (_currentInputs[InputAction.MoveRight])
            {
                player.X += moveSpeed * deltaTime;
            }

            // Keep player within screen bounds
            player.X = Math.Max(0, Math.Min(800, player.X));
            player.Y = Math.Max(0, Math.Min(600, player.Y));

            await Task.CompletedTask;
        }

        /// <summary>
        /// Handle shooting input
        /// </summary>
        private async Task HandleShootingAsync(GameFrame frame)
        {
            if (IsInputJustPressed(InputAction.Shoot))
            {
                var player = GetPlayer(frame.GameState);
                if (player != null)
                {
                    await CreateBulletAsync(frame, player.X + player.Width / 2, player.Y);
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Create a new bullet
        /// </summary>
        private async Task CreateBulletAsync(GameFrame frame, float x, float y)
        {
            var bullet = new GameObject
            {
                X = x,
                Y = y,
                Width = 4,
                Height = 10,
                VelocityY = -300, // Move upward
                Properties = { ["Type"] = "Bullet", ["Damage"] = 1 }
            };

            frame.GameState.GameObjects.Add(bullet);
            Console.WriteLine($"🔫 Bullet fired at ({x:F1}, {y:F1})");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Restart the game
        /// </summary>
        private async Task RestartGameAsync(GameState gameState)
        {
            gameState.Score = 0;
            gameState.Lives = 3;
            gameState.Wave = 1;
            gameState.IsGameOver = false;
            gameState.IsPaused = false;
            gameState.GameObjects.Clear();

            Console.WriteLine("🔄 Game Restarted!");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Get the player object from game state
        /// </summary>
        private GameObject? GetPlayer(GameState gameState)
        {
            return gameState.GameObjects.FirstOrDefault(obj => 
                obj.Properties.ContainsKey("Type") && 
                obj.Properties["Type"].ToString() == "Player");
        }

        /// <summary>
        /// Check if input was just pressed this frame (edge detection)
        /// </summary>
        private bool IsInputJustPressed(InputAction action)
        {
            return _currentInputs.GetValueOrDefault(action, false) && 
                   !_previousInputs.GetValueOrDefault(action, false);
        }
    }

    /// <summary>
    /// Input actions supported by the game
    /// </summary>
    public enum InputAction
    {
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
        Shoot,
        Pause,
        Restart,
        MouseClick,
        MousePosition,
        GamepadA,
        GamepadB
    }
}
