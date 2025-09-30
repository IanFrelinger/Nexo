using UnityEngine;

namespace NexoDirectorStudio.Game
{
    /// <summary>
    /// Represents a power-up in the Doom FPS game.
    /// Handles power-up behavior and pickup mechanics.
    /// </summary>
    public class DoomPowerUp : MonoBehaviour
    {
        [Header("Power-up Stats")]
        public PowerUpType powerUpType = PowerUpType.Health;
        public int value = 25;
        public bool isPickedUp = false;
        
        public enum PowerUpType
        {
            Health,
            Armor,
            Ammo,
            Speed,
            Damage
        }
        
        void Start()
        {
            Initialize();
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isPickedUp)
            {
                PickupPowerUp();
            }
        }
        
        public void Initialize()
        {
            // Set up power-up appearance
            var renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            // Create power-up mesh
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            
            meshFilter.mesh = CreatePowerUpMesh();
            
            // Set power-up material
            var material = new Material(Shader.Find("Standard"));
            material.color = GetPowerUpColor();
            renderer.material = material;
            
            // Add collider for pickup
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
            }
            
            // Tag as power-up
            gameObject.tag = "PowerUp";
        }
        
        void PickupPowerUp()
        {
            var playerGame = FindObjectOfType<DoomFPSGame>();
            if (playerGame != null)
            {
                isPickedUp = true;
                
                // Apply power-up effect
                switch (powerUpType)
                {
                    case PowerUpType.Health:
                        playerGame.AddHealth(value);
                        Debug.Log($"❤️ Health restored! +{value} health");
                        break;
                    case PowerUpType.Armor:
                        playerGame.AddArmor(value);
                        Debug.Log($"🛡️ Armor added! +{value} armor");
                        break;
                    case PowerUpType.Ammo:
                        playerGame.AddAmmo(value);
                        Debug.Log($"🔫 Ammo added! +{value} ammo");
                        break;
                    case PowerUpType.Speed:
                        // Speed boost would be implemented here
                        Debug.Log($"⚡ Speed boost! +{value}% speed");
                        break;
                    case PowerUpType.Damage:
                        // Damage boost would be implemented here
                        Debug.Log($"💥 Damage boost! +{value}% damage");
                        break;
                }
                
                // Hide power-up
                gameObject.SetActive(false);
            }
        }
        
        Mesh CreatePowerUpMesh()
        {
            var mesh = new Mesh();
            
            // Create a simple power-up mesh (sphere-like)
            var vertices = new Vector3[]
            {
                // Bottom
                new Vector3(0, 0, 0),
                new Vector3(0.2f, 0, 0),
                new Vector3(0.2f, 0, 0.2f),
                new Vector3(0, 0, 0.2f),
                
                // Top
                new Vector3(0, 0.2f, 0),
                new Vector3(0.2f, 0.2f, 0),
                new Vector3(0.2f, 0.2f, 0.2f),
                new Vector3(0, 0.2f, 0.2f)
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
        
        Color GetPowerUpColor()
        {
            switch (powerUpType)
            {
                case PowerUpType.Health:
                    return Color.red;
                case PowerUpType.Armor:
                    return Color.blue;
                case PowerUpType.Ammo:
                    return Color.yellow;
                case PowerUpType.Speed:
                    return Color.green;
                case PowerUpType.Damage:
                    return Color.magenta;
                default:
                    return Color.white;
            }
        }
    }
}
