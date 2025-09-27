using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexoDoomGame.DomainLogic.Base;

namespace NexoDoomGame.DomainLogic
{
    /// <summary>
    /// Abstract domain logic for AILogic
    /// Domain: AI
    /// Cross-domain usage: Unity, Unreal, WebGL, Mobile, Console, Desktop
    /// </summary>
    public abstract class AILogic : BaseDomainLogic
    {
        protected readonly IIWorldProvider;
        protected readonly IIStateProvider;
        protected readonly IIEventProvider;
        protected readonly IINavigationProvider;
        
        protected AILogic(IIWorldProvider iworldprovider, IIStateProvider istateprovider, IIEventProvider ieventprovider, IINavigationProvider inavigationprovider)
        {
            this.iworldprovider = iworldprovider;
            this.istateprovider = istateprovider;
            this.ieventprovider = ieventprovider;
            this.inavigationprovider = inavigationprovider;
        }
        
        /// <summary>
        /// Abstract AI behavior system with decision making and pathfinding
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
    /// Interface for AILogic providers
    /// </summary>
    public partial interface IAILogicProvider : IBaseDomainLogicProvider
    {
        new Task<AILogic> CreateAsync();
    }
    
    /// <summary>
    /// Interface for AILogic validation
    /// </summary>
    public partial interface IAILogicValidator : IBaseDomainLogicValidator
    {
        Task<bool> ValidateAsync(AILogic component);
    }
}