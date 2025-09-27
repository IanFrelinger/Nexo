using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.FeatureFactory.DomainLogic;
using Nexo.Core.Application.Services.FeatureFactory.TestGeneration;
using Nexo.Core.Application.Services.FeatureFactory.Validation;
using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;
using Nexo.Core.Domain.Results;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.FeatureFactory.Orchestration.Components;

namespace Nexo.Core.Application.Services.FeatureFactory.Orchestration
{
    /// <summary>
    /// Orchestrator for domain logic generation workflow that delegates to specialized orchestration components.
    /// </summary>
    public partial class DomainLogicOrchestrator : IDomainLogicOrchestrator
    {
        private readonly ILogger<DomainLogicOrchestrator> _logger;
        private readonly IDomainLogicGenerator _domainLogicGenerator;
        private readonly ITestSuiteGenerator _testSuiteGenerator;
        private readonly IDomainLogicValidator _domainLogicValidator;
        private readonly ConcurrentDictionary<string, GenerationProgress> _activeSessions;
        private readonly ConcurrentDictionary<string, GenerationReport> _completedSessions;
        private readonly GenerationSessionManager _sessionManager;
        private readonly DomainLogicWorkflowExecutor _workflowExecutor;
        private readonly GenerationProgressTracker _progressTracker;
        private readonly GenerationReportGenerator _reportGenerator;

        public DomainLogicOrchestrator(
            ILogger<DomainLogicOrchestrator> logger,
            IDomainLogicGenerator domainLogicGenerator,
            ITestSuiteGenerator testSuiteGenerator,
            IDomainLogicValidator domainLogicValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _domainLogicGenerator = domainLogicGenerator ?? throw new ArgumentNullException(nameof(domainLogicGenerator));
            _testSuiteGenerator = testSuiteGenerator ?? throw new ArgumentNullException(nameof(testSuiteGenerator));
            _domainLogicValidator = domainLogicValidator ?? throw new ArgumentNullException(nameof(domainLogicValidator));
            _activeSessions = new ConcurrentDictionary<string, GenerationProgress>();
            _completedSessions = new ConcurrentDictionary<string, GenerationReport>();
            
            _sessionManager = new GenerationSessionManager(_logger, _activeSessions, _completedSessions);
            _workflowExecutor = new DomainLogicWorkflowExecutor(_logger, _domainLogicGenerator, _testSuiteGenerator, _domainLogicValidator);
            _progressTracker = new GenerationProgressTracker(_logger, _activeSessions);
            _reportGenerator = new GenerationReportGenerator(_logger, _completedSessions);
        }

        /// <summary>
        /// Generates complete domain logic from validated requirements
        /// </summary>
        public async Task<DomainLogicGenerationResult> GenerateCompleteDomainLogicAsync(ValidatedRequirements requirements, CancellationToken cancellationToken = default)
        {
            var sessionId = await _sessionManager.CreateSessionAsync(requirements);
            
            try
            {
                _logger.LogInformation("Starting domain logic generation for session {SessionId}", sessionId);
                
                var result = await _workflowExecutor.ExecuteWorkflowAsync(sessionId, requirements, cancellationToken);
                
                await _sessionManager.CompleteSessionAsync(sessionId, result);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Domain logic generation failed for session {SessionId}", sessionId);
                await _sessionManager.FailSessionAsync(sessionId, ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the current progress of a generation session
        /// </summary>
        public async Task<GenerationProgress> GetGenerationProgressAsync(string sessionId)
        {
            return await _progressTracker.GetProgressAsync(sessionId);
        }

        /// <summary>
        /// Gets the report for a completed generation session
        /// </summary>
        public async Task<GenerationReport> GetGenerationReportAsync(string sessionId)
        {
            return await _reportGenerator.GetReportAsync(sessionId);
        }

        /// <summary>
        /// Cancels an active generation session
        /// </summary>
        public async Task<bool> CancelGenerationAsync(string sessionId)
        {
            return await _sessionManager.CancelSessionAsync(sessionId);
        }

        /// <summary>
        /// Gets all active generation sessions
        /// </summary>
        public async Task<List<GenerationProgress>> GetActiveSessionsAsync()
        {
            return await _sessionManager.GetActiveSessionsAsync();
        }

        /// <summary>
        /// Gets all completed generation sessions
        /// </summary>
        public async Task<List<GenerationReport>> GetCompletedSessionsAsync()
        {
            return await _reportGenerator.GetAllReportsAsync();
        }

        /// <summary>
        /// Cleans up old sessions and reports
        /// </summary>
        public async Task CleanupOldSessionsAsync(TimeSpan maxAge)
        {
            await _sessionManager.CleanupOldSessionsAsync(maxAge);
        }
    }
}
