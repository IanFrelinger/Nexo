using UnityEngine;

namespace NexoDirectorStudio.Game
{
    /// <summary>
    /// Represents a platform tile in the Doom FPS game.
    /// Handles platform behavior and player interaction.
    /// </summary>
    public class DoomPlatform : MonoBehaviour
    {
        [Header("Platform Settings")]
        public float jumpHeight = 2f;
        public float width = 1f;
        public bool isMoving = false;
        public float moveSpeed = 1f;
        public Vector3 moveDirection = Vector3.forward;
        
        private Vector3 startPosition;
        private float moveTime;
        
        void Start()
        {
            Initialize();
        }
        
        void Update()
        {
            if (isMoving)
            {
                MovePlatform();
            }
        }
        
        public void Initialize()
        {
            startPosition = transform.position;
            
            // Set up platform appearance
            var renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            // Set platform material
            var material = new Material(Shader.Find("Standard"));
            material.color = Color.gray;
            renderer.material = material;
            
            // Add collider for platform
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
            }
            
            // Tag as platform
            gameObject.tag = "Platform";
        }
        
        void MovePlatform()
        {
            moveTime += Time.deltaTime * moveSpeed;
            transform.position = startPosition + moveDirection * Mathf.Sin(moveTime);
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log("🦘 Player landed on platform!");
            }
        }
    }
}
