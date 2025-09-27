using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Contracts;
using Nexo.Core.Configuration;
using Nexo.Observability.ActivitySources;
using Nexo.Observability.Metrics;

namespace Nexo.Core.Pipeline
{
    /// <summary>
    /// Typed pipeline orchestrator that composes generator, compilation gates, policy gates, and publishing.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    /// <typeparam name="TRequest">The type of request for generation</typeparam>
    /// <typeparam name="TArtifact">The type of artifact generated</typeparam>
    public sealed partial class ExtensionPipeline<TRequest, TArtifact>
    {
        private readonly IExtensionGenerator<TRequest, TArtifact> _generator;
        private readonly IEnumerable<ICompilationGate> _compilationGates;
        private readonly IEnumerable<IPolicyGate<TArtifact>> _policyGates;
        private readonly IArtifactPublisher<TArtifact> _publisher;
        private readonly ILogger<ExtensionPipeline<TRequest, TArtifact>> _logger;
        private readonly IRepairStrategy<TRequest, TArtifact>? _repairStrategy;
        private readonly ICanaryDeployer<TArtifact>? _canaryDeployer;
        private readonly IRollbackStrategy<TArtifact>? _rollbackStrategy;
        private readonly RepairLoopOptions _repairOptions;
        private readonly ActivitySource _pipelineActivitySource;
        private readonly ActivitySource _generationActivitySource;
        private readonly ActivitySource _validationActivitySource;
        private readonly ActivitySource _policyActivitySource;
        private readonly ActivitySource _repairActivitySource;
        private readonly PipelineMetrics _metrics;

        /// <summary>
        /// Initializes a new instance of the ExtensionPipeline class.
        /// </summary>
        /// <param name="generator">The extension generator</param>
        /// <param name="gates">The compilation gates to validate against</param>
        /// <param name="policies">The policy gates to evaluate against</param>
        /// <param name="publisher">The artifact publisher</param>
        /// <param name="logger">The logger instance</param>
        /// <param name="repairStrategy">Optional repair strategy for failed artifacts</param>
        /// <param name="canaryDeployer">Optional canary deployment strategy</param>
        /// <param name="rollbackStrategy">Optional rollback strategy for failed deployments</param>
        /// <param name="repairOptions">Configuration options for the repair loop</param>
        /// <param name="pipelineActivitySource">Activity source for pipeline operations</param>
        /// <param name="generationActivitySource">Activity source for generation operations</param>
        /// <param name="validationActivitySource">Activity source for validation operations</param>
        /// <param name="policyActivitySource">Activity source for policy operations</param>
        /// <param name="repairActivitySource">Activity source for repair operations</param>
        /// <param name="metrics">Pipeline metrics</param>
        public ExtensionPipeline(
            IExtensionGenerator<TRequest, TArtifact> generator,
            IEnumerable<ICompilationGate> gates,
            IEnumerable<IPolicyGate<TArtifact>> policies,
            IArtifactPublisher<TArtifact> publisher,
            ILogger<ExtensionPipeline<TRequest, TArtifact>> logger,
            IRepairStrategy<TRequest, TArtifact>? repairStrategy = null,
            ICanaryDeployer<TArtifact>? canaryDeployer = null,
            IRollbackStrategy<TArtifact>? rollbackStrategy = null,
            IOptions<RepairLoopOptions>? repairOptions = null,
            ActivitySource? pipelineActivitySource = null,
            ActivitySource? generationActivitySource = null,
            ActivitySource? validationActivitySource = null,
            ActivitySource? policyActivitySource = null,
            ActivitySource? repairActivitySource = null,
            PipelineMetrics? metrics = null)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _compilationGates = gates ?? throw new ArgumentNullException(nameof(gates));
            _policyGates = policies ?? throw new ArgumentNullException(nameof(policies));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repairStrategy = repairStrategy;
            _canaryDeployer = canaryDeployer;
            _rollbackStrategy = rollbackStrategy;
            _repairOptions = repairOptions?.Value ?? new RepairLoopOptions();
            _pipelineActivitySource = pipelineActivitySource ?? NexoActivitySources.Pipeline;
            _generationActivitySource = generationActivitySource ?? NexoActivitySources.Generation;
            _validationActivitySource = validationActivitySource ?? NexoActivitySources.Validation;
            _policyActivitySource = policyActivitySource ?? NexoActivitySources.Policy;
            _repairActivitySource = repairActivitySource ?? NexoActivitySources.Repair;
            _metrics = metrics ?? new PipelineMetrics(new Meter("Nexo.Pipeline"));
        }
        // This class acts as an orchestrator for various pipeline functionalities,
        // with specific categories defined in partial classes.
    }
}
