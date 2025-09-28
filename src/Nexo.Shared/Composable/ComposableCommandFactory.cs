using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;
using Nexo.Shared.Operations;

namespace Nexo.Shared.Composable
{
    /// <summary>
    /// Factory for creating composable commands with dependency injection
    /// </summary>
    public class ComposableCommandFactory
    {
        private readonly IDependencyContainer _container;
        
        public ComposableCommandFactory(IDependencyContainer? container = null)
        {
            _container = container ?? new DependencyContainer();
        }
        
        /// <summary>
        /// Creates a command with dependency injection
        /// </summary>
        public async Task<TCommand> CreateCommandAsync<TCommand>() where TCommand : class, IComposableCommand
        {
            // Resolve dependencies
            var collectionOps = await _container.ResolveAsync<ICollectionOperations>();
            var loopOps = await _container.ResolveAsync<ILoopOperations>();
            var stringOps = await _container.ResolveAsync<IStringOperations>();
            
            // Create command instance
            var command = Activator.CreateInstance(typeof(TCommand), collectionOps, loopOps, stringOps) as TCommand;
            return command;
        }
        
        /// <summary>
        /// Creates a command with custom dependencies
        /// </summary>
        public async Task<TCommand> CreateCommandWithDependenciesAsync<TCommand>(
            ICollectionOperations collectionOps,
            ILoopOperations loopOps,
            IStringOperations stringOps) where TCommand : class, IComposableCommand
        {
            var command = Activator.CreateInstance(typeof(TCommand), collectionOps, loopOps, stringOps) as TCommand;
            return command;
        }
        
        /// <summary>
        /// Creates a command with partial dependencies
        /// </summary>
        public async Task<TCommand> CreateCommandWithPartialDependenciesAsync<TCommand>(
            ICollectionOperations? collectionOps = null,
            ILoopOperations? loopOps = null,
            IStringOperations? stringOps = null) where TCommand : class, IComposableCommand
        {
            var resolvedCollectionOps = collectionOps ?? await _container.ResolveAsync<ICollectionOperations>();
            var resolvedLoopOps = loopOps ?? await _container.ResolveAsync<ILoopOperations>();
            var resolvedStringOps = stringOps ?? await _container.ResolveAsync<IStringOperations>();
            
            var command = Activator.CreateInstance(typeof(TCommand), resolvedCollectionOps, resolvedLoopOps, resolvedStringOps) as TCommand;
            return command;
        }
        
        /// <summary>
        /// Registers default operations with the container
        /// </summary>
        public async Task RegisterDefaultOperationsAsync()
        {
            await _container.RegisterInstanceAsync<ICollectionOperations>(new CollectionOperations());
            await _container.RegisterInstanceAsync<ILoopOperations>(new LoopOperations());
            await _container.RegisterInstanceAsync<IStringOperations>(new StringOperations());
        }
        
        /// <summary>
        /// Registers custom operations with the container
        /// </summary>
        public async Task RegisterOperationsAsync(
            ICollectionOperations collectionOps,
            ILoopOperations loopOps,
            IStringOperations stringOps)
        {
            await _container.RegisterInstanceAsync(collectionOps);
            await _container.RegisterInstanceAsync(loopOps);
            await _container.RegisterInstanceAsync(stringOps);
        }
    }
}
