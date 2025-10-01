using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
#if UNITY_AI_NAVIGATION
using Unity.AI.Navigation; // NavMeshSurface (AI Navigation package)
#endif

namespace NexoDirectorStudio.Boot
{
    /// <summary>
    /// Deterministic bootstrap that guarantees runtime prerequisites for DirectorStudio.
    /// Ensures EventSystem, MainCamera, Autoplayer, and NavMesh are properly set up.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class AutoBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            // Set headless-friendly defaults
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            // Ensure all prerequisites exist
            EnsureEventSystem();
            EnsureMainCamera();
            EnsureAutoplayer();
            
            // Rebuild NavMesh when scenes load
            SceneManager.sceneLoaded += (_, __) => RebuildNavMeshesInScene();
            RebuildNavMeshesInScene();
        }

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            Object.DontDestroyOnLoad(go);
            
            Debug.Log("✅ EventSystem created for headless compatibility");
        }

        static void EnsureMainCamera()
        {
            if (Camera.main != null) return;
            
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0, 5, -10);
            cam.transform.LookAt(Vector3.zero);
            Object.DontDestroyOnLoad(cam.gameObject);
            
            Debug.Log("✅ MainCamera created for headless compatibility");
        }

        static void EnsureAutoplayer()
        {
            if (Object.FindFirstObjectByType<Agents.AIAutoplayer>() != null) return;
            
            var go = new GameObject("AIAutoplayer");
            go.AddComponent<Agents.AIAutoplayer>();
            Object.DontDestroyOnLoad(go);
            
            Debug.Log("✅ AIAutoplayer created for headless compatibility");
        }

        static void RebuildNavMeshesInScene()
        {
#if UNITY_AI_NAVIGATION
            foreach (var s in Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.InstanceID))
            {
                s.BuildNavMesh();
                Debug.Log($"✅ NavMesh rebuilt for {s.name}");
            }
#else
            Debug.Log("⚠️ NavMesh rebuilding skipped - AI Navigation package not available");
#endif
        }
    }
}
