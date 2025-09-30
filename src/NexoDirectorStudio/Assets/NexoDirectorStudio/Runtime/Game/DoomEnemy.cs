using UnityEngine;
using System.Collections;

namespace NexoDirectorStudio.Game
{
    /// <summary>
    /// Represents an enemy in the Doom FPS game.
    /// Handles enemy AI, movement, and combat behavior.
    /// </summary>
    public class DoomEnemy : MonoBehaviour
    {
        [Header("Enemy Stats")]
        public int health = 50;
        public int maxHealth = 50;
        public float moveSpeed = 2f;
        public float attackRange = 5f;
        public float attackDamage = 15f;
        public float attackCooldown = 2f;
        
        [Header("AI Behavior")]
        public float detectionRange = 10f;
        public float patrolRange = 5f;
        public float rotationSpeed = 5f;
        
        private Transform player;
        private Vector3 startPosition;
        private Vector3 patrolTarget;
        private float lastAttackTime;
        private bool isDead = false;
        private bool isAttacking = false;
        private bool isPatrolling = true;
        
        void Start()
        {
            startPosition = transform.position;
            player = FindObjectOfType<DoomFPSGame>()?.transform;
            SetNewPatrolTarget();
        }
        
        void Update()
        {
            if (!isDead && player != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                
                if (distanceToPlayer <= detectionRange)
                {
                    // Player detected, switch to attack mode
                    isPatrolling = false;
                    AttackPlayer();
                }
                else if (isPatrolling)
                {
                    // Continue patrolling
                    Patrol();
                }
            }
        }
        
        void AttackPlayer()
        {
            // Look at player
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0; // Keep enemy on ground level
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
            
            // Move towards player
            if (Vector3.Distance(transform.position, player.position) > attackRange)
            {
                transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
            }
            else
            {
                // Attack player
                if (Time.time - lastAttackTime > attackCooldown)
                {
                    PerformAttack();
                    lastAttackTime = Time.time;
                }
            }
        }
        
        void PerformAttack()
        {
            if (!isAttacking)
            {
                StartCoroutine(AttackCoroutine());
            }
        }
        
        IEnumerator AttackCoroutine()
        {
            isAttacking = true;
            
            // Attack animation/effect
            Debug.Log($"👹 Enemy attacks player for {attackDamage} damage!");
            
            // Deal damage to player
            var playerGame = FindObjectOfType<DoomFPSGame>();
            if (playerGame != null)
            {
                playerGame.TakeDamage((int)attackDamage);
            }
            
            yield return new WaitForSeconds(0.5f);
            isAttacking = false;
        }
        
        void Patrol()
        {
            // Move towards patrol target
            Vector3 direction = (patrolTarget - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
            transform.Translate(Vector3.forward * moveSpeed * 0.5f * Time.deltaTime);
            
            // Check if reached patrol target
            if (Vector3.Distance(transform.position, patrolTarget) < 1f)
            {
                SetNewPatrolTarget();
            }
        }
        
        void SetNewPatrolTarget()
        {
            // Set random patrol target within patrol range
            Vector3 randomDirection = Random.insideUnitSphere * patrolRange;
            randomDirection.y = 0; // Keep on ground level
            patrolTarget = startPosition + randomDirection;
        }
        
        public void TakeDamage(int damage)
        {
            if (!isDead)
            {
                health -= damage;
                Debug.Log($"👹 Enemy takes {damage} damage! Health: {health}/{maxHealth}");
                
                if (health <= 0)
                {
                    Die();
                }
            }
        }
        
        void Die()
        {
            isDead = true;
            Debug.Log("💀 Enemy defeated!");
            
            // Death animation/effect
            StartCoroutine(DeathCoroutine());
        }
        
        IEnumerator DeathCoroutine()
        {
            // Simple death effect - fade out
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.material.color;
                for (float alpha = 1f; alpha >= 0f; alpha -= Time.deltaTime)
                {
                    color.a = alpha;
                    renderer.material.color = color;
                    yield return null;
                }
            }
            
            // Destroy enemy after death animation
            Destroy(gameObject);
        }
        
        public bool IsDead()
        {
            return isDead;
        }
        
        public void Initialize()
        {
            // Set up enemy appearance
            var renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            // Create enemy mesh
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            
            meshFilter.mesh = CreateEnemyMesh();
            
            // Set enemy material
            var material = new Material(Shader.Find("Standard"));
            material.color = Color.red;
            renderer.material = material;
            
            // Add collider
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<CapsuleCollider>();
            }
            
            // Tag as enemy
            gameObject.tag = "Enemy";
        }
        
        Mesh CreateEnemyMesh()
        {
            var mesh = new Mesh();
            
            // Create a simple enemy mesh (capsule-like)
            var vertices = new Vector3[]
            {
                // Bottom
                new Vector3(0, 0, 0),
                new Vector3(0.5f, 0, 0),
                new Vector3(0.5f, 0, 0.5f),
                new Vector3(0, 0, 0.5f),
                
                // Top
                new Vector3(0, 1, 0),
                new Vector3(0.5f, 1, 0),
                new Vector3(0.5f, 1, 0.5f),
                new Vector3(0, 1, 0.5f)
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
