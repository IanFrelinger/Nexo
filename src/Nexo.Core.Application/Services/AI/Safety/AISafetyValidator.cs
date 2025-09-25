using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums.Safety;
using Nexo.Core.Domain.Entities.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.AI.Safety.Components;

namespace Nexo.Core.Application.Services.AI.Safety
{
    /// <summary>
    /// Orchestrator for AI safety validation that delegates to specialized safety validation components.
    /// </summary>
    public class AISafetyValidator
    {
        private readonly ILogger<AISafetyValidator> _logger;
        private readonly List<SafetyRule> _safetyRules;
        private readonly List<ContentFilter> _contentFilters;
        private readonly ContentSafetyValidator _contentValidator;
        private readonly BiasDetectionValidator _biasValidator;
        private readonly ToxicityDetectionValidator _toxicityValidator;
        private readonly ComplianceValidator _complianceValidator;
        private readonly SafetyReportGenerator _reportGenerator;

        public AISafetyValidator(ILogger<AISafetyValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _safetyRules = InitializeSafetyRules();
            _contentFilters = InitializeContentFilters();
            
            _contentValidator = new ContentSafetyValidator(_logger, _safetyRules, _contentFilters);
            _biasValidator = new BiasDetectionValidator(_logger);
            _toxicityValidator = new ToxicityDetectionValidator(_logger);
            _complianceValidator = new ComplianceValidator(_logger);
            _reportGenerator = new SafetyReportGenerator(_logger);
        }

        /// <summary>
        /// Validates AI-generated content for safety and compliance
        /// </summary>
        public async Task<SafetyValidationResult> ValidateContentAsync(string content, SafetyLevel safetyLevel, string context = "")
        {
            return await _contentValidator.ValidateContentAsync(content, safetyLevel, context);
        }

        /// <summary>
        /// Detects bias in AI-generated content
        /// </summary>
        public async Task<BiasDetectionResult> DetectBiasAsync(string content, string context = "")
        {
            return await _biasValidator.DetectBiasAsync(content, context);
        }

        /// <summary>
        /// Detects toxicity in AI-generated content
        /// </summary>
        public async Task<ToxicityDetectionResult> DetectToxicityAsync(string content, string context = "")
        {
            return await _toxicityValidator.DetectToxicityAsync(content, context);
        }

        /// <summary>
        /// Validates compliance with safety regulations
        /// </summary>
        public async Task<ComplianceValidationResult> ValidateComplianceAsync(string content, string context = "")
        {
            return await _complianceValidator.ValidateComplianceAsync(content, context);
        }

        /// <summary>
        /// Generates comprehensive safety report
        /// </summary>
        public async Task<SafetyReport> GenerateSafetyReportAsync(string content, SafetyLevel safetyLevel, string context = "")
        {
            return await _reportGenerator.GenerateReportAsync(content, safetyLevel, context);
        }

        /// <summary>
        /// Validates content against multiple safety criteria
        /// </summary>
        public async Task<ComprehensiveSafetyResult> ValidateComprehensiveAsync(string content, SafetyLevel safetyLevel, string context = "")
        {
            try
            {
                _logger.LogInformation("Starting comprehensive safety validation for content");

                var contentResult = await ValidateContentAsync(content, safetyLevel, context);
                var biasResult = await DetectBiasAsync(content, context);
                var toxicityResult = await DetectToxicityAsync(content, context);
                var complianceResult = await ValidateComplianceAsync(content, context);

                var comprehensiveResult = new ComprehensiveSafetyResult
                {
                    ContentValidation = contentResult,
                    BiasDetection = biasResult,
                    ToxicityDetection = toxicityResult,
                    ComplianceValidation = complianceResult,
                    OverallSafetyScore = CalculateOverallSafetyScore(contentResult, biasResult, toxicityResult, complianceResult),
                    ValidationTime = DateTime.UtcNow
                };

                _logger.LogInformation("Comprehensive safety validation completed with score: {Score}", comprehensiveResult.OverallSafetyScore);
                return comprehensiveResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Comprehensive safety validation failed");
                throw;
            }
        }

        private double CalculateOverallSafetyScore(
            SafetyValidationResult contentResult,
            BiasDetectionResult biasResult,
            ToxicityDetectionResult toxicityResult,
            ComplianceValidationResult complianceResult)
        {
            var scores = new List<double>
            {
                contentResult.IsValid ? 100 : 0,
                biasResult.BiasScore,
                toxicityResult.ToxicityScore,
                complianceResult.ComplianceScore
            };

            return scores.Average();
        }

        private List<SafetyRule> InitializeSafetyRules()
        {
            return new List<SafetyRule>
            {
                new SafetyRule { Name = "No Harmful Content", Description = "Content must not contain harmful or dangerous information" },
                new SafetyRule { Name = "No Hate Speech", Description = "Content must not contain hate speech or discriminatory language" },
                new SafetyRule { Name = "No Violence", Description = "Content must not promote or describe violence" },
                new SafetyRule { Name = "No Illegal Activities", Description = "Content must not describe or promote illegal activities" }
            };
        }

        private List<ContentFilter> InitializeContentFilters()
        {
            return new List<ContentFilter>
            {
                new ContentFilter { Name = "Profanity Filter", Pattern = @"\b(profanity|swear)\b", Action = "Replace" },
                new ContentFilter { Name = "Personal Information Filter", Pattern = @"\b\d{3}-\d{2}-\d{4}\b", Action = "Remove" },
                new ContentFilter { Name = "Spam Filter", Pattern = @"\b(spam|scam|phishing)\b", Action = "Block" }
            };
        }
    }
}
