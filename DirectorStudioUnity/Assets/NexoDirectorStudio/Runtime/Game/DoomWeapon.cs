using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace NexoDirectorStudio.Game
{
    /// <summary>
    /// Represents a weapon in the Doom FPS game.
    /// Handles weapon behavior, ammo, and pickup mechanics.
    /// </summary>
    public class DoomWeapon : MonoBehaviour
    {
        [Header("Weapon Stats")]
        public string weaponName = "Plasma Rifle";
        public int ammoCount = 30;
        public int maxAmmo = 30;
        public float damage = 25f;
        public float fireRate = 0.1f;
        public float range = 50f;
        
        [Header("Weapon Type")]
        public WeaponType weaponType = WeaponType.Rifle;
        public bool isAutomatic = true;
        public bool isPickedUp = false;
        
        private float lastFireTime;
        private DoomFPSGame playerGame;
        
        public enum WeaponType
        {
            Pistol,
            Rifle,
            Shotgun,
            RocketLauncher,
            PlasmaGun
        }
        
        void Start()
        {
            playerGame = FindObjectOfType<DoomFPSGame>();
            Initialize();
        }
        
        void Update()
        {
            if (isPickedUp && playerGame != null)
            {
                // Weapon is in player's inventory
                HandleWeaponUse();
            }
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isPickedUp)
            {
                PickupWeapon();
            }
        }
        
        public void Initialize()
        {
            // Set up weapon appearance
            var renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            // Create weapon mesh
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            
            meshFilter.mesh = CreateWeaponMesh();
            
            // Set weapon material
            var material = new Material(Shader.Find("Standard"));
            material.color = GetWeaponColor();
            renderer.material = material;
            
            // Add collider for pickup
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
            }
            
            // Tag as weapon
            gameObject.tag = "Weapon";
        }
        
        void PickupWeapon()
        {
            if (playerGame != null)
            {
                isPickedUp = true;
                playerGame.AddAmmo(ammoCount);
                Debug.Log($"🔫 Picked up {weaponName}! +{ammoCount} ammo");
                
                // Hide weapon model (it's now in inventory)
                gameObject.SetActive(false);
            }
        }
        
        void HandleWeaponUse()
        {
            // Check if player is firing
            if (Input.GetButton("Fire1") && Time.time - lastFireTime > fireRate)
            {
                FireWeapon();
                lastFireTime = Time.time;
            }
        }
        
        void FireWeapon()
        {
            if (playerGame != null && playerGame.currentAmmo > 0)
            {
                // Consume ammo
                playerGame.currentAmmo--;
                
                // Fire weapon
                Debug.Log($"🔫 {weaponName} fired! Damage: {damage}");
                
                // Raycast for hit detection
                Camera playerCamera = Camera.main;
                if (playerCamera != null)
                {
                    Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
                    RaycastHit hit;
                    
                    if (Physics.Raycast(ray, out hit, range))
                    {
                        var enemy = hit.collider.GetComponent<DoomEnemy>();
                        if (enemy != null)
                        {
                            enemy.TakeDamage((int)damage);
                            Debug.Log($"💥 Hit enemy for {damage} damage!");
                        }
                    }
                }
            }
        }
        
        Mesh CreateWeaponMesh()
        {
            var mesh = new Mesh();
            
            // Create weapon-specific mesh based on type
            switch (weaponType)
            {
                case WeaponType.Pistol:
                    mesh = CreatePistolMesh();
                    break;
                case WeaponType.Rifle:
                    mesh = CreateRifleMesh();
                    break;
                case WeaponType.Shotgun:
                    mesh = CreateShotgunMesh();
                    break;
                case WeaponType.RocketLauncher:
                    mesh = CreateRocketLauncherMesh();
                    break;
                case WeaponType.PlasmaGun:
                    mesh = CreatePlasmaGunMesh();
                    break;
                default:
                    mesh = CreateRifleMesh();
                    break;
            }
            
            return mesh;
        }
        
        Mesh CreatePistolMesh()
        {
            var mesh = new Mesh();
            var vertices = new Vector3[]
            {
                // Simple pistol shape
                new Vector3(0, 0, 0),
                new Vector3(0.2f, 0, 0),
                new Vector3(0.2f, 0, 0.1f),
                new Vector3(0, 0, 0.1f),
                new Vector3(0, 0.1f, 0),
                new Vector3(0.2f, 0.1f, 0),
                new Vector3(0.2f, 0.1f, 0.1f),
                new Vector3(0, 0.1f, 0.1f)
            };
            
            var triangles = new int[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
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
        
        Mesh CreateRifleMesh()
        {
            var mesh = new Mesh();
            var vertices = new Vector3[]
            {
                // Simple rifle shape
                new Vector3(0, 0, 0),
                new Vector3(0.3f, 0, 0),
                new Vector3(0.3f, 0, 0.1f),
                new Vector3(0, 0, 0.1f),
                new Vector3(0, 0.1f, 0),
                new Vector3(0.3f, 0.1f, 0),
                new Vector3(0.3f, 0.1f, 0.1f),
                new Vector3(0, 0.1f, 0.1f)
            };
            
            var triangles = new int[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
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
        
        Mesh CreateShotgunMesh()
        {
            return CreateRifleMesh(); // Simplified for demo
        }
        
        Mesh CreateRocketLauncherMesh()
        {
            return CreateRifleMesh(); // Simplified for demo
        }
        
        Mesh CreatePlasmaGunMesh()
        {
            return CreateRifleMesh(); // Simplified for demo
        }
        
        Color GetWeaponColor()
        {
            switch (weaponType)
            {
                case WeaponType.Pistol:
                    return Color.gray;
                case WeaponType.Rifle:
                    return Color.black;
                case WeaponType.Shotgun:
                    return new Color(0.6f, 0.4f, 0.2f); // Brown color
                case WeaponType.RocketLauncher:
                    return Color.red;
                case WeaponType.PlasmaGun:
                    return Color.cyan;
                default:
                    return Color.gray;
            }
        }
    }
}
