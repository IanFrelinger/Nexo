using System;
using UnityEngine;
using NexoDirectorStudio.Interactions;

namespace NexoDirectorStudio.Editor
{
    // Simple test interaction for playtesting
    public class SimpleTestInteraction : MonoBehaviour, IInteraction
    {
        public event Action<IInteraction> Triggered;
        public bool IsArmed { get; private set; }
        public bool HasTriggered { get; private set; }
        public string InteractionId => $"simple-test-{GetInstanceID()}";
        private float _triggerTime;
        private int _triggerCount;
        
        void Start()
        {
            Initialize();
            Arm();
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (IsArmed && !HasTriggered && other.CompareTag("Player"))
            {
                Trigger();
            }
        }
        
        public void Initialize()
        {
            IsArmed = false;
            HasTriggered = false;
            _triggerTime = 0f;
            _triggerCount = 0;
        }
        
        public void Arm()
        {
            IsArmed = true;
        }
        
        public void Trigger()
        {
            if (!IsArmed || HasTriggered) return;
            
            HasTriggered = true;
            _triggerTime = Time.time;
            _triggerCount++;
            
            Triggered?.Invoke(this);
            Debug.Log($"🎯 Simple interaction triggered: {name}");
        }
        
        public bool TryInvoke()
        {
            if (IsArmed && !HasTriggered)
            {
                Trigger();
                return true;
            }
            return false;
        }
        
        public InteractionMetadata GetMetadata()
        {
            return new InteractionMetadata
            {
                Type = "SimpleTest",
                Name = name,
                Position = transform.position,
                IsArmed = IsArmed,
                HasTriggered = HasTriggered,
                TriggerTime = _triggerTime,
                TriggerCount = _triggerCount
            };
        }
    }
}
