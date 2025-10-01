using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using NexoDirectorStudio.Interactions;

namespace NexoDirectorStudio.Agents
{
    /// <summary>
    /// Simple AI autoplayer that drives the player through the slice.
    /// </summary>
    public class AIAutoplayer : MonoBehaviour
    {
        public float moveSpeed = 4f;
        public float turnSpeed = 240f;
        public float detectionRadius = 20f;
        public LayerMask enemyMask;

        private CharacterController controller;
        private Camera cam;
        private Vector3 currentTarget;
        private float lastDecisionTime;
        private float decisionInterval = 0.25f;
        private float lastInteractionTime;
        private float interactionInterval = 1f;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            cam = Camera.main;
            PickNewTarget();
        }
        
        void PickNewTarget()
        {
            // Pick a random target position for wandering
            var randomDirection = UnityEngine.Random.insideUnitSphere * 10f;
            randomDirection.y = 0;
            currentTarget = transform.position + randomDirection;
        }

        void Update()
        {
            if (Time.time - lastDecisionTime > decisionInterval)
            {
                Think();
                lastDecisionTime = Time.time;
            }

            Act();
            
            // Try to interact with nearby objects
            if (Time.time - lastInteractionTime > interactionInterval)
            {
                TryInteractWithNearbyObjects();
                lastInteractionTime = Time.time;
            }
        }

        void Think()
        {
            // Priority: closest enemy → power-up → goal → wander
            var enemy = FindClosestWithTag("Enemy");
            if (enemy != null)
            {
                currentTarget = enemy.position;
                AimAt(enemy.position);
                TryFire();
                return;
            }

            var power = FindClosestWithTag("PowerUp");
            if (power != null)
            {
                currentTarget = power.position;
                return;
            }

            var goal = FindClosestWithTag("Goal");
            if (goal != null)
            {
                currentTarget = goal.position;
                return;
            }

            // Wander forward
            currentTarget = transform.position + transform.forward * 5f;
        }

        void Act()
        {
            var dir = (currentTarget - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.25f)
            {
                var rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, turnSpeed * Time.deltaTime);

                var move = transform.forward * moveSpeed;
                if (controller != null)
                {
                    move.y += Physics.gravity.y * Time.deltaTime;
                    controller.Move(move * Time.deltaTime);
                }
                else
                {
                    transform.position += move * Time.deltaTime;
                }
            }
        }

        void AimAt(Vector3 worldPos)
        {
            if (cam == null) return;
            var dir = (worldPos - cam.transform.position).normalized;
            var rot = Quaternion.LookRotation(new Vector3(dir.x, dir.y, dir.z));
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, rot, 0.2f);
        }

        void TryFire()
        {
            // Simulate left click by directly raycasting and damaging enemies if centered
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out var hit, 50f))
            {
                var enemy = hit.collider.GetComponent<NexoDirectorStudio.Game.DoomEnemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(10);
                }
            }
        }

        Transform FindClosestWithTag(string tag)
        {
            var objs = GameObject.FindGameObjectsWithTag(tag);
            Transform best = null;
            float bestSqr = float.MaxValue;
            var pos = transform.position;
            foreach (var go in objs)
            {
                var d = (go.transform.position - pos).sqrMagnitude;
                if (d < bestSqr)
                {
                    bestSqr = d;
                    best = go.transform;
                }
            }
            return best;
        }
        
        void TryInteractWithNearbyObjects()
        {
            // Find nearby interactive objects
            var nearbyObjects = Physics.OverlapSphere(transform.position, detectionRadius);
            
            foreach (var collider in nearbyObjects)
            {
                // Try to find IInteraction components
                var interaction = collider.GetComponent<IInteraction>();
                if (interaction != null && interaction.IsArmed && !interaction.HasTriggered)
                {
                    // Simulate interaction by calling Trigger directly
                    interaction.Trigger();
                    Debug.Log($"🤖 Autoplayer triggered interaction: {interaction.GetMetadata().Name}");
                }
                
                // Also try clicking on the object for UI-based interactions
                if (collider.CompareTag("PowerUp") || collider.CompareTag("Goal"))
                {
                    Clicking.Click(collider.gameObject);
                }
            }
        }
    }
}
