using UnityEngine;
using NexoDirectorStudio.Game;

namespace NexoDirectorStudio.Game
{
    /// <summary>
    /// Sets up the Doom FPS game scene in Unity.
    /// This script should be attached to a GameObject in the scene to initialize the game.
    /// </summary>
    public class DoomSceneSetup : MonoBehaviour
    {
        [Header("Scene Setup")]
        public bool setupOnStart = true;
        public bool createGameLauncher = true;
        
        void Start()
        {
            if (setupOnStart)
            {
                SetupScene();
            }
        }
        
        public void SetupScene()
        {
            Debug.Log("🎮 Setting up Doom FPS Game Scene...");
            
            // Create game launcher
            if (createGameLauncher)
            {
                var launcherObj = new GameObject("Doom Game Launcher");
                var launcher = launcherObj.AddComponent<DoomGameLauncher>();
                launcher.autoLaunch = true;
                launcher.showDebugInfo = true;
            }
            
            // Set up scene settings
            SetupSceneSettings();
            
            Debug.Log("✅ Doom FPS Game Scene setup complete!");
        }
        
        void SetupSceneSettings()
        {
            // Set up physics
            Physics.gravity = new Vector3(0, -9.81f, 0);
            
            // Set up rendering
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 4;
            
            // Set up audio
            AudioListener.volume = 1.0f;
            
            Debug.Log("✅ Scene settings configured");
        }
    }
}
