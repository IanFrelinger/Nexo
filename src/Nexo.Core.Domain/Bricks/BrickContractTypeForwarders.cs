using System.Runtime.CompilerServices;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

// Type forwarders preserve backward-compatible assembly references after brick authoring
// types moved into Nexo.Brick.Contracts. Consumers referencing Nexo.Core.Domain types
// continue to resolve without recompilation.
[assembly: TypeForwardedTo(typeof(AgenticImplementation))]
[assembly: TypeForwardedTo(typeof(Brick))]
[assembly: TypeForwardedTo(typeof(BrickCategory))]
[assembly: TypeForwardedTo(typeof(BrickImplementations))]
[assembly: TypeForwardedTo(typeof(BrickInput))]
[assembly: TypeForwardedTo(typeof(BrickInputDefinition))]
[assembly: TypeForwardedTo(typeof(BrickInterface))]
[assembly: TypeForwardedTo(typeof(BrickMetadata))]
[assembly: TypeForwardedTo(typeof(BrickOutput))]
[assembly: TypeForwardedTo(typeof(BrickOutputDefinition))]
[assembly: TypeForwardedTo(typeof(DeterministicImplementation))]
[assembly: TypeForwardedTo(typeof(DomainKnowledge))]
[assembly: TypeForwardedTo(typeof(DomainRule))]
[assembly: TypeForwardedTo(typeof(IExecutionContext))]
[assembly: TypeForwardedTo(typeof(ImplementationCharacteristics))]
[assembly: TypeForwardedTo(typeof(ImplementationSelector))]
[assembly: TypeForwardedTo(typeof(ImplementationType))]
[assembly: TypeForwardedTo(typeof(LearnedPattern))]
[assembly: TypeForwardedTo(typeof(LLMConfig))]
[assembly: TypeForwardedTo(typeof(ProviderConfig))]
[assembly: TypeForwardedTo(typeof(ResourceUsage))]
