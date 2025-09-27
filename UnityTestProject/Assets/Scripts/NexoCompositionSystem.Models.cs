using System;

namespace NexoDoomGame
{
    /// <summary>
    /// Composition result data
    /// </summary>
    [System.Serializable]
    public class CompositionResult
    {
        public string Component;
        public CompositionType Type;
        public CompositionStatus Status;
        public DateTime Timestamp;
        public string Error;
    }
    
    /// <summary>
    /// Composition types
    /// </summary>
    public enum CompositionType
    {
        Script,
        Texture,
        Model,
        Audio,
        Composition,
        Validation,
        Finalization
    }
    
    /// <summary>
    /// Composition status
    /// </summary>
    public enum CompositionStatus
    {
        Pending,
        Loading,
        Loaded,
        Composing,
        Completed,
        Validated,
        Failed
    }
}
