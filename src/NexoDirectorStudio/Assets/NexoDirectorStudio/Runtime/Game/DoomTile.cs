using UnityEngine;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Game
{
    /// <summary>
    /// Represents a tile in the Doom FPS game world.
    /// Handles tile-specific behavior and interactions.
    /// </summary>
    public class DoomTile : MonoBehaviour
    {
        public TileData tileData;
        public bool isWalkable;
        public bool blocksLineOfSight;
        public string tileType;
        
        void Start()
        {
            // Add collider based on tile type
            if (tileType == "Wall" || tileType == "Hazard")
            {
                var collider = gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = tileType == "Hazard";
            }
        }
        
        public void Initialize(TileData data)
        {
            tileData = data;
            tileType = data.TileType;
            isWalkable = data.IsWalkable;
            blocksLineOfSight = data.BlocksLineOfSight;
            
            // Set up tile properties
            SetupTileProperties();
        }
        
        void SetupTileProperties()
        {
            // Add tile-specific components based on type
            switch (tileType)
            {
                case "Hazard":
                    gameObject.AddComponent<DoomHazard>();
                    break;
                case "Platform":
                    gameObject.AddComponent<DoomPlatform>();
                    break;
            }
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (tileType == "Hazard" && other.CompareTag("Player"))
            {
                var player = other.GetComponent<DoomFPSGame>();
                if (player != null)
                {
                    player.TakeDamage(10);
                    Debug.Log("💥 Player hit hazard!");
                }
            }
        }
    }
}
