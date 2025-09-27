using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexoDoomGame.DomainLogic.Base;

namespace NexoDoomGame.DomainLogic
{
    /// <summary>
    /// Abstract domain logic for MovementLogic
    /// Domain: Movement
    /// Cross-domain usage: Unity, Unreal, WebGL, Mobile, Console
    /// </summary>
    public abstract class MovementLogic : BaseDomainLogic
    {
        protected readonly IIInputProvider;
        protected readonly IIPhysicsProvider;
        protected readonly IICollisionProvider;
        
        protected MovementLogic(IIInputProvider iinputprovider, IIPhysicsProvider iphysicsprovider, IICollisionProvider icollisionprovider)
        {
            this.iinputprovider = iinputprovider;
            this.iphysicsprovider = iphysicsprovider;
            this.icollisionprovider = icollisionprovider;
        }
        
        /// <summary>
        /// Abstract movement logic that can work across platforms
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
    /// Interface for MovementLogic providers
    /// </summary>
    public partial interface IMovementLogicProvider : IBaseDomainLogicProvider
    {
        new Task<MovementLogic> CreateAsync();
    }
    
    /// <summary>
    /// Interface for MovementLogic validation
    /// </summary>
    public partial interface IMovementLogicValidator : IBaseDomainLogicValidator
    {
        Task<bool> ValidateAsync(MovementLogic component);
    }
}