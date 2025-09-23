using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexoDoomGame.DomainLogic.Base;

namespace NexoDoomGame.DomainLogic
{
    /// <summary>
    /// Abstract domain logic for CombatLogic
    /// Domain: Combat
    /// Cross-domain usage: Unity, Unreal, WebGL, Mobile, Console, Desktop
    /// </summary>
    public abstract class CombatLogic : BaseDomainLogic
    {
        protected readonly IIHealthProvider;
        protected readonly IIAudioProvider;
        protected readonly IIAnimationProvider;
        protected readonly IIProjectileProvider;
        
        protected CombatLogic(IIHealthProvider ihealthprovider, IIAudioProvider iaudioprovider, IIAnimationProvider ianimationprovider, IIProjectileProvider iprojectileprovider)
        {
            this.ihealthprovider = ihealthprovider;
            this.iaudioprovider = iaudioprovider;
            this.ianimationprovider = ianimationprovider;
            this.iprojectileprovider = iprojectileprovider;
        }
        
        /// <summary>
        /// Abstract combat system with weapon management and damage calculation
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
    /// Interface for CombatLogic providers
    /// </summary>
    public interface ICombatLogicProvider : IBaseDomainLogicProvider
    {
        new Task<CombatLogic> CreateAsync();
    }
    
    /// <summary>
    /// Interface for CombatLogic validation
    /// </summary>
    public interface ICombatLogicValidator : IBaseDomainLogicValidator
    {
        Task<bool> ValidateAsync(CombatLogic component);
    }
}