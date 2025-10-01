using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace NexoDirectorStudio.Game
{
    /// <summary>
    /// Represents a hazard tile in the Doom FPS game.
    /// Handles damage and environmental hazards.
    /// </summary>
    public class DoomHazard : MonoBehaviour
    {
        [Header("Hazard Settings")]
        public int damage = 10;
        public float damageInterval = 1f;
        public bool isActive = true;
        
        private float lastDamageTime;
        
        void Start()
        {
            Initialize();
        }
        
        void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player") && isActive && Time.time - lastDamageTime > damageInterval)
            {
                DamagePlayer(other);
                lastDamageTime = Time.time;
            }
        }
        
        public void Initialize()
        {
            // Set up hazard appearance
            var renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            // Set hazard material
            var material = new Material(Shader.Find("Standard"));
            material.color = Color.red;
            renderer.material = material;
            
            // Add collider for damage
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
            }
            
            // Tag as hazard
            gameObject.tag = "Hazard";
        }
        
        void DamagePlayer(Collider player)
        {
            var playerGame = player.GetComponent<DoomFPSGame>();
            if (playerGame != null)
            {
                playerGame.TakeDamage(damage);
                Debug.Log($"💥 Player hit hazard! -{damage} health");
            }
        }
    }
}
