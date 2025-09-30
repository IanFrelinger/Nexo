using UnityEngine;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;
using System.Threading.Tasks;

namespace NexoDirectorStudio.Game
{
    /// <summary>
    /// Launches the Doom FPS game using Director Studio generated content.
    /// This script initializes the game and starts the gameplay.
    /// </summary>
    public class DoomGameLauncher : MonoBehaviour
    {
        [Header("Game Launch Settings")]
        public bool autoLaunch = true;
        public bool showDebugInfo = true;
        
        private DoomFPSGame gameController;
        private DirectorStudioService service;
        
        async void Start()
        {
            if (autoLaunch)
            {
                await LaunchDoomGame();
            }
        }
        
        public async Task LaunchDoomGame()
        {
            Debug.Log("🎮 Launching Doom FPS Game...");
            
            try
            {
                // Initialize Director Studio
                service = new DirectorStudioService();
                Debug.Log("✅ Director Studio initialized");
                
                // Create game brief
                var gameBrief = CreateDoomGameBrief();
                Debug.Log($"📝 Game brief created: {gameBrief.Description}");
                
                // Generate game plan
                var planCommand = service.GetService<IPlanGameSliceCommand>();
                var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(gameBrief), System.Threading.CancellationToken.None);
                Debug.Log($"🧠 Game plan generated: {gamePlan.Genre} - {gamePlan.Description}");
                
                // Build world layout
                var buildCommand = service.GetService<IBuildWorldLayoutCommand>();
                var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), System.Threading.CancellationToken.None);
                Debug.Log($"🏗️ World layout built: {worldLayout.Dimensions.x}x{worldLayout.Dimensions.y}x{worldLayout.Dimensions.z}");
                
                // Place interactions
                var interactionsCommand = service.GetService<IPlaceInteractionsCommand>();
                var interactionGraph = await interactionsCommand.ExecuteAsync(new IPlaceInteractionsCommand.Input(worldLayout, gamePlan), System.Threading.CancellationToken.None);
                Debug.Log($"⚔️ Interactions placed: {interactionGraph.InteractionPoints.Count} points");
                
                // Create content bundle
                var contentCommand = service.GetService<ICreateContentBundleCommand>();
                var contentBundle = await contentCommand.ExecuteAsync(new ICreateContentBundleCommand.Input(interactionGraph, gamePlan), System.Threading.CancellationToken.None);
                Debug.Log($"📦 Content bundle created: {contentBundle.Textures.Count} textures, {contentBundle.AudioClips.Count} audio clips");
                
                // Initialize game controller
                InitializeGameController(gamePlan, worldLayout, interactionGraph, contentBundle);
                
                Debug.Log("🚀 Doom FPS Game launched successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Failed to launch game: {ex.Message}");
            }
        }
        
        DesignBrief CreateDoomGameBrief()
        {
            return new DesignBrief(
                Description: "A fast-paced, action-packed first-person shooter inspired by classic Doom. The player must navigate through demon-infested corridors, collect weapons and ammo, and survive waves of enemies. The game should feature intense combat, dark atmospheric environments, and classic FPS mechanics like strafing, circle-strafing, and weapon switching.",
                GenreHint: "FPS",
                TargetDurationMinutes: 15,
                DifficultyLevel: 4,
                Constraints: "Must include: Multiple weapon types, enemy variety, health/armor systems, key-based progression, atmospheric lighting, fast-paced movement",
                Seed: 666,
                Version: 1
            );
        }
        
        void InitializeGameController(GamePlan gamePlan, WorldLayout worldLayout, InteractionGraph interactionGraph, ContentBundle contentBundle)
        {
            // Create game controller
            var gameObj = new GameObject("Doom FPS Game Controller");
            gameController = gameObj.AddComponent<DoomFPSGame>();
            
            // Set game data
            gameController.gamePlan = gamePlan;
            gameController.worldLayout = worldLayout;
            gameController.interactionGraph = interactionGraph;
            gameController.contentBundle = contentBundle;
            
            // Set up player
            SetupPlayer();
            
            // Set up camera
            SetupCamera();
            
            // Set up lighting
            SetupLighting();
            
            Debug.Log("✅ Game controller initialized");
        }
        
        void SetupPlayer()
        {
            // Create player GameObject
            var playerObj = new GameObject("Player");
            playerObj.tag = "Player";
            
            // Add CharacterController
            var controller = playerObj.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0, 0.9f, 0);
            
            // Add game controller to player
            var gameController = playerObj.AddComponent<DoomFPSGame>();
            
            // Position player
            playerObj.transform.position = new Vector3(1, 1, 1);
            
            Debug.Log("✅ Player setup complete");
        }
        
        void SetupCamera()
        {
            // Create main camera
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 90f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            
            // Position camera
            cameraObj.transform.position = new Vector3(1, 1.6f, 1);
            cameraObj.transform.rotation = Quaternion.identity;
            
            Debug.Log("✅ Camera setup complete");
        }
        
        void SetupLighting()
        {
            // Set up atmospheric lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.1f, 0.1f, 0.2f);
            RenderSettings.ambientEquatorColor = new Color(0.2f, 0.2f, 0.3f);
            RenderSettings.ambientGroundColor = new Color(0.1f, 0.1f, 0.1f);
            
            // Add directional light
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.8f, 0.7f, 0.6f);
            light.intensity = 0.5f;
            lightObj.transform.rotation = Quaternion.Euler(45, 45, 0);
            
            Debug.Log("✅ Lighting setup complete");
        }
        
        void OnDestroy()
        {
            // Clean up service
            service?.Dispose();
        }
    }
}
