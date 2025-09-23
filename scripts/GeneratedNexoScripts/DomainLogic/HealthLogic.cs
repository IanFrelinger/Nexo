using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexoDoomGame.DomainLogic.Base;

namespace NexoDoomGame.DomainLogic
{
    /// <summary>
    /// Abstract domain logic for HealthLogic
    /// Domain: Health
    /// Cross-domain usage: Unity, Unreal, WebGL, Mobile, Console, Desktop
    /// </summary>
    public abstract class HealthLogic : BaseDomainLogic
    {
        protected readonly IIEventProvider;
        protected readonly IIStateProvider;
        protected readonly IINotificationProvider;
        
        protected HealthLogic(IIEventProvider ieventprovider, IIStateProvider istateprovider, IINotificationProvider inotificationprovider)
        {
            this.ieventprovider = ieventprovider;
            this.istateprovider = istateprovider;
            this.inotificationprovider = inotificationprovider;
        }
        
        /// <summary>
        /// Abstract health and damage system with state management
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
    /// Interface for HealthLogic providers
    /// </summary>
    public interface IHealthLogicProvider : IBaseDomainLogicProvider
    {
        new Task<HealthLogic> CreateAsync();
    }
    
    /// <summary>
    /// Interface for HealthLogic validation
    /// </summary>
    public interface IHealthLogicValidator : IBaseDomainLogicValidator
    {
        Task<bool> ValidateAsync(HealthLogic component);
    }
}