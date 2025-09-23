using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexoDoomGame.DomainLogic.Base;

namespace NexoDoomGame.DomainLogic
{
    /// <summary>
    /// Abstract domain logic for AudioLogic
    /// Domain: Audio
    /// Cross-domain usage: Unity, Unreal, WebGL, Mobile, Console, Desktop
    /// </summary>
    public abstract class AudioLogic : BaseDomainLogic
    {
        protected readonly IIResourceProvider;
        protected readonly IISpatialProvider;
        protected readonly IIVolumeProvider;
        
        protected AudioLogic(IIResourceProvider iresourceprovider, IISpatialProvider ispatialprovider, IIVolumeProvider ivolumeprovider)
        {
            this.iresourceprovider = iresourceprovider;
            this.ispatialprovider = ispatialprovider;
            this.ivolumeprovider = ivolumeprovider;
        }
        
        /// <summary>
        /// Abstract audio management system with spatial audio support
        /// </summary>
        public override abstract Task<bool> ValidateAsync();
        
        /// <summary>
        /// Execute the core domain logic
        /// </summary>
        public override abstract Task<object> ExecuteAsync(object input);
        
        /// <summary>
        /// Get the current state of the component
        /// </summary>
        public override abstract Task<Dictionary<string, object>> GetStateAsync();
    }
    
    /// <summary>
    /// Interface for AudioLogic providers
    /// </summary>
    public interface IAudioLogicProvider : IBaseDomainLogicProvider
    {
        new Task<AudioLogic> CreateAsync();
    }
    
    /// <summary>
    /// Interface for AudioLogic validation
    /// </summary>
    public interface IAudioLogicValidator : IBaseDomainLogicValidator
    {
        Task<bool> ValidateAsync(AudioLogic component);
    }
}