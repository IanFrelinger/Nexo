using System;
using UnityEngine;

namespace NexoDirectorStudio.Interactions
{
    /// <summary>
    /// Simple test interaction for smoke tests and validation.
    /// Implements IInteraction with direct invocation capability.
    /// </summary>
    public class TestInteraction : MonoBehaviour, IInteraction
    {
        [SerializeField] private string _interactionId = "test-interaction";
        [SerializeField] private bool _isArmed = true;
        
        private bool _hasTriggered = false;
        private int _triggerCount = 0;
        private float _lastTriggerTime = 0f;
        
        public event Action<IInteraction> Triggered;
        
        public string InteractionId => _interactionId;
        public bool IsArmed => _isArmed;
        public bool HasTriggered => _hasTriggered;
        
        public void Initialize()
        {
            // Test interaction is always ready
        }
        
        public void Arm()
        {
            _isArmed = true;
        }
        
        public void Trigger()
        {
            if (!_isArmed) return;
            
            _hasTriggered = true;
            _triggerCount++;
            _lastTriggerTime = Time.time;
            
            Debug.Log($"[TestInteraction] {_interactionId} triggered (count: {_triggerCount})");
            Triggered?.Invoke(this);
        }
        
        public bool TryInvoke()
        {
            if (!_isArmed) return false;
            
            Trigger();
            return true;
        }
        
        public InteractionMetadata GetMetadata()
        {
            return new InteractionMetadata
            {
                Type = "TestInteraction",
                Name = _interactionId,
                Position = transform.position,
                IsArmed = _isArmed,
                HasTriggered = _hasTriggered,
                TriggerTime = _lastTriggerTime,
                TriggerCount = _triggerCount
            };
        }
    }
}
