using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace NexoDirectorStudio.Interactions
{
    /// <summary>
    /// Centralized bus for managing all interactions in DirectorStudio.
    /// Registers, arms, and tracks all interactive objects.
    /// </summary>
    public class InteractionBus : MonoBehaviour
    {
        public static InteractionBus Instance { get; private set; }
        
        public int TotalInteractions => _all.Count;
        public int TriggeredCount { get; private set; }
        public int ArmedCount => _all.Count(i => i.IsArmed);
        
        private readonly List<IInteraction> _all = new();
        private readonly Dictionary<string, int> _triggerCountsByType = new();
        
        void Awake()
        {
            if (Instance != null) 
            { 
                Destroy(gameObject); 
                return; 
            }
            
            Instance = this; 
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("✅ InteractionBus initialized");
        }
        
        /// <summary>
        /// Register a new interaction with the bus
        /// </summary>
        public void Register(IInteraction interaction)
        {
            if (interaction == null || _all.Contains(interaction)) return;
            
            _all.Add(interaction);
            interaction.Triggered += OnInteractionTriggered;
            interaction.Initialize();
            interaction.Arm();
            
            Debug.Log($"✅ Registered interaction: {interaction.GetMetadata().Name} ({interaction.GetMetadata().Type})");
        }
        
        /// <summary>
        /// Unregister an interaction from the bus
        /// </summary>
        public void Unregister(IInteraction interaction)
        {
            if (interaction == null || !_all.Contains(interaction)) return;
            
            _all.Remove(interaction);
            interaction.Triggered -= OnInteractionTriggered;
            
            Debug.Log($"❌ Unregistered interaction: {interaction.GetMetadata().Name}");
        }
        
        /// <summary>
        /// Get all interactions of a specific type
        /// </summary>
        public IEnumerable<IInteraction> GetInteractionsByType(string type)
        {
            return _all.Where(i => i.GetMetadata().Type == type);
        }
        
        /// <summary>
        /// Get all armed interactions that haven't been triggered yet
        /// </summary>
        public IEnumerable<IInteraction> GetArmedUntriggeredInteractions()
        {
            return _all.Where(i => i.IsArmed && !i.HasTriggered);
        }
        
        /// <summary>
        /// Get all triggered interactions
        /// </summary>
        public IEnumerable<IInteraction> GetTriggeredInteractions()
        {
            return _all.Where(i => i.HasTriggered);
        }
        
        /// <summary>
        /// Get interaction metrics for reporting
        /// </summary>
        public InteractionMetrics GetMetrics()
        {
            return new InteractionMetrics
            {
                TotalInteractions = _all.Count,
                TriggeredCount = TriggeredCount,
                ArmedCount = ArmedCount,
                TriggerCountsByType = new Dictionary<string, int>(_triggerCountsByType),
                Interactions = _all.Select(i => i.GetMetadata()).ToArray()
            };
        }
        
        /// <summary>
        /// Reset all metrics (useful for test runs)
        /// </summary>
        public void ResetMetrics()
        {
            TriggeredCount = 0;
            _triggerCountsByType.Clear();
            
            foreach (var interaction in _all)
            {
                // Reset individual interaction state if it supports it
                if (interaction is IResettableInteraction resettable)
                {
                    resettable.Reset();
                }
            }
            
            Debug.Log("🔄 InteractionBus metrics reset");
        }
        
        private void OnInteractionTriggered(IInteraction interaction)
        {
            TriggeredCount++;
            
            var metadata = interaction.GetMetadata();
            if (!_triggerCountsByType.ContainsKey(metadata.Type))
            {
                _triggerCountsByType[metadata.Type] = 0;
            }
            _triggerCountsByType[metadata.Type]++;
            
            Debug.Log($"🎯 Interaction triggered: {metadata.Name} ({metadata.Type}) - Total: {TriggeredCount}");
        }
        
        void OnDestroy()
        {
            // Clean up event subscriptions
            foreach (var interaction in _all)
            {
                if (interaction != null)
                {
                    interaction.Triggered -= OnInteractionTriggered;
                }
            }
            _all.Clear();
        }
    }
    
    /// <summary>
    /// Interface for interactions that can be reset
    /// </summary>
    public interface IResettableInteraction : IInteraction
    {
        void Reset();
    }
    
    /// <summary>
    /// Metrics data for interaction reporting
    /// </summary>
    [System.Serializable]
    public struct InteractionMetrics
    {
        public int TotalInteractions;
        public int TriggeredCount;
        public int ArmedCount;
        public Dictionary<string, int> TriggerCountsByType;
        public InteractionMetadata[] Interactions;
    }
}
