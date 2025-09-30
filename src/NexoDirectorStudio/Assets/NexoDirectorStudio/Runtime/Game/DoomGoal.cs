using UnityEngine;

namespace NexoDirectorStudio.Game
{
    /// <summary>
    /// Represents the goal/exit in the Doom FPS game.
    /// Handles level completion and victory conditions.
    /// </summary>
    public class DoomGoal : MonoBehaviour
    {
        [Header("Goal Settings")]
        public bool isActivated = false;
        public string goalMessage = "Level Complete!";
        
        void Start()
        {
            Initialize();
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isActivated)
            {
                ActivateGoal();
            }
        }
        
        public void Initialize()
        {
            // Set up goal appearance
            var renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            // Create goal mesh
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            
            meshFilter.mesh = CreateGoalMesh();
            
            // Set goal material
            var material = new Material(Shader.Find("Standard"));
            material.color = Color.green;
            renderer.material = material;
            
            // Add collider for trigger
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
            }
            
            // Tag as goal
            gameObject.tag = "Goal";
        }
        
        void ActivateGoal()
        {
            isActivated = true;
            Debug.Log($"🎯 {goalMessage}");
            
            // Trigger victory condition
            var playerGame = FindObjectOfType<DoomFPSGame>();
            if (playerGame != null)
            {
                playerGame.Victory();
            }
        }
        
        Mesh CreateGoalMesh()
        {
            var mesh = new Mesh();
            
            // Create a simple goal mesh (door-like)
            var vertices = new Vector3[]
            {
                // Bottom
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 0.1f),
                new Vector3(0, 0, 0.1f),
                
                // Top
                new Vector3(0, 2, 0),
                new Vector3(1, 2, 0),
                new Vector3(1, 2, 0.1f),
                new Vector3(0, 2, 0.1f)
            };
            
            var triangles = new int[]
            {
                // Bottom face
                0, 2, 1, 0, 3, 2,
                // Top face
                4, 5, 6, 4, 6, 7,
                // Side faces
                0, 1, 5, 0, 5, 4,
                1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6,
                3, 0, 4, 3, 4, 7
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }
    }
}
