using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Data;
using FeatureFactoryDemo.Services;
using FeatureFactoryDemo.Models;
using Nexo.Feature.Analysis.Interfaces;

namespace FeatureFactoryDemo.Validation
{
    /// <summary>
    /// Comprehensive validation service for testing all Feature Factory features.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class FeatureValidationService
    {
        private readonly FeatureFactoryDbContext _context;
        private readonly CommandHistoryService _commandHistoryService;
        private readonly CodebaseAnalysisService _codebaseAnalysisService;
        private readonly ICodingStandardAnalyzer _codeAnalyzer;
        private readonly ILogger<FeatureValidationService> _logger;
        
        public FeatureValidationService(
            FeatureFactoryDbContext context,
            CommandHistoryService commandHistoryService,
            CodebaseAnalysisService codebaseAnalysisService,
            ICodingStandardAnalyzer codeAnalyzer,
            ILogger<FeatureValidationService> logger)
        {
            _context = context;
            _commandHistoryService = commandHistoryService;
            _codebaseAnalysisService = codebaseAnalysisService;
            _codeAnalyzer = codeAnalyzer;
            _logger = logger;
        }
        // This class acts as an orchestrator for various feature validation functionalities,
        // with specific categories defined in partial classes.
    }
}
