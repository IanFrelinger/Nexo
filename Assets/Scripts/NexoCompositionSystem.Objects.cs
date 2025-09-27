using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace NexoDoomGame
{
    /// <summary>
    /// GameObject creation functionality for NexoCompositionSystem.
    /// </summary>
    public partial class NexoCompositionSystem
    {
        /// <summary>
        /// Creates all game objects
        /// </summary>
        private IEnumerator CreateGameObjects()
        {
            Debug.Log("🏗️ Creating game objects...");
            UpdateCompositionStatus("Creating Game Objects");
            
            try
            {
                // Create player object
                var player = CreatePlayerObject();
                composedObjects.Add(player);
                
                // Create weapon objects
                var shotgun = CreateWeaponObject("Shotgun");
                var plasmaRifle = CreateWeaponObject("PlasmaRifle");
                composedObjects.Add(shotgun);
                composedObjects.Add(plasmaRifle);
                
                // Create enemy objects
                var imp = CreateEnemyObject("Imp");
                var demon = CreateEnemyObject("Demon");
                var cacodemon = CreateEnemyObject("Cacodemon");
                composedObjects.Add(imp);
                composedObjects.Add(demon);
                composedObjects.Add(cacodemon);
                
                // Create UI object
                var ui = CreateUIObject();
                composedObjects.Add(ui);
                
                Debug.Log($"✅ Created {composedObjects.Count} game objects");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to create game objects: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }

        /// <summary>
        /// Creates player object
        /// </summary>
        private GameObject CreatePlayerObject()
        {
            var player = new GameObject("Player");
            
            // Add required components
            player.AddComponent<CharacterController>();
            player.AddComponent<FPSController>();
            player.AddComponent<HealthSystem>();
            player.AddComponent<PlayerController>();
            
            // Add camera
            var camera = new GameObject("Camera");
            camera.transform.SetParent(player.transform);
            camera.AddComponent<Camera>();
            camera.transform.localPosition = new Vector3(0, 1.8f, 0);
            
            Debug.Log("🏗️ Created player object");
            return player;
        }

        /// <summary>
        /// Creates weapon object
        /// </summary>
        private GameObject CreateWeaponObject(string weaponName)
        {
            var weapon = new GameObject(weaponName);
            
            // Add required components
            weapon.AddComponent<WeaponSystem>();
            weapon.AddComponent<AudioSource>();
            
            Debug.Log($"🏗️ Created {weaponName} object");
            return weapon;
        }

        /// <summary>
        /// Creates enemy object
        /// </summary>
        private GameObject CreateEnemyObject(string enemyName)
        {
            var enemy = new GameObject(enemyName);
            
            // Add required components
            enemy.AddComponent<NavMeshAgent>();
            enemy.AddComponent<CapsuleCollider>();
            enemy.AddComponent<EnemyAI>();
            enemy.AddComponent<HealthSystem>();
            enemy.AddComponent<AudioSource>();
            
            Debug.Log($"🏗️ Created {enemyName} object");
            return enemy;
        }

        /// <summary>
        /// Creates UI object
        /// </summary>
        private GameObject CreateUIObject()
        {
            var ui = new GameObject("UI");
            
            // Add required components
            ui.AddComponent<UIManager>();
            ui.AddComponent<Canvas>();
            ui.AddComponent<CanvasScaler>();
            ui.AddComponent<GraphicRaycaster>();
            
            Debug.Log("🏗️ Created UI object");
            return ui;
        }
    }
}
