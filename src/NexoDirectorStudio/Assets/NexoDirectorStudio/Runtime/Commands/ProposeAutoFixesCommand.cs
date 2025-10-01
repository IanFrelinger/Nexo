using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Profiles;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Implementation of IProposeAutoFixesCommand.
    /// Analyzes validation results and proposes specific fixes that can be automatically applied.
    /// </summary>
    public sealed class ProposeAutoFixesCommand : IProposeAutoFixesCommand
    {
        private readonly GenreProfileService _profileService;
        private readonly List<IAutoFixGenerator> _fixGenerators;
        
        public ProposeAutoFixesCommand(GenreProfileService profileService)
        {
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _fixGenerators = new List<IAutoFixGenerator>
            {
                new CriticalIssueFixGenerator(),
                new ErrorIssueFixGenerator(),
                new WarningIssueFixGenerator(),
                new InfoIssueFixGenerator(),
                new HighPrioritySuggestionFixGenerator(),
                new MediumHighPrioritySuggestionFixGenerator(),
                new MediumPrioritySuggestionFixGenerator(),
                new LowMediumPrioritySuggestionFixGenerator(),
                new LowPrioritySuggestionFixGenerator()
            };
        }
        
        public async Task<AutoFixProposal> ExecuteAsync(ValidationReport validationReport, CancellationToken cancellationToken = default)
        {
            if (validationReport == null)
                throw new ArgumentNullException(nameof(validationReport));
            
            var proposedFixes = new List<AutoFix>();
            var canApplyAllFixes = true;
            var totalConfidence = 0;
            var fixCount = 0;
            
            // Analyze each issue and propose fixes
            foreach (var issue in validationReport.AllIssues)
            {
                var fixes = await ProposeFixesForIssue(issue, cancellationToken);
                proposedFixes.AddRange(fixes);
                
                if (fixes.Any(f => !f.CanApplyAutomatically))
                {
                    canApplyAllFixes = false;
                }
                
                totalConfidence += fixes.Sum(f => f.ConfidenceScore);
                fixCount += fixes.Count;
            }
            
            // Analyze suggestions and propose fixes
            foreach (var suggestion in validationReport.AllSuggestions)
            {
                var fixes = await ProposeFixesForSuggestion(suggestion, cancellationToken);
                proposedFixes.AddRange(fixes);
                
                if (fixes.Any(f => !f.CanApplyAutomatically))
                {
                    canApplyAllFixes = false;
                }
                
                totalConfidence += fixes.Sum(f => f.ConfidenceScore);
                fixCount += fixes.Count;
            }
            
            // Create the proposal
            var proposal = new AutoFixProposal
            {
                Suggestions = proposedFixes,
                CanApplyAllFixes = canApplyAllFixes,
                AverageConfidence = fixCount > 0 ? (float)totalConfidence / fixCount : 0f,
                Summary = AutoFixUtilities.GenerateSummary(proposedFixes),
                Description = AutoFixUtilities.GenerateDescription(proposedFixes),
                EstimatedEffort = AutoFixUtilities.CalculateEstimatedEffort(proposedFixes),
                TotalFixes = proposedFixes.Count,
                AutomaticFixes = proposedFixes.Count(f => f.CanApplyAutomatically),
                ManualFixes = proposedFixes.Count(f => !f.CanApplyAutomatically),
                FixesByCategory = proposedFixes.GroupBy(f => f.Category)
                    .ToDictionary(g => g.Key, g => g.Count()),
                FixesBySeverity = proposedFixes.GroupBy(f => f.Severity)
                    .ToDictionary(g => g.Key, g => g.Count()),
                RecommendedOrder = GenerateRecommendedOrder(proposedFixes),
                Warnings = GenerateWarnings(proposedFixes),
                Notes = GenerateNotes(proposedFixes)
            };
            
            return proposal;
        }
        
        private async Task<IReadOnlyList<AutoFix>> ProposeFixesForIssue(ValidationIssue issue, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();
            
            foreach (var generator in _fixGenerators)
            {
                if (generator.CanHandle(issue))
                {
                    var generatorFixes = await generator.GenerateFixesAsync(issue, cancellationToken);
                    fixes.AddRange(generatorFixes);
                }
            }
            
            return fixes;
        }
        
        private async Task<IReadOnlyList<AutoFix>> ProposeFixesForSuggestion(ValidationSuggestion suggestion, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();
            
            foreach (var generator in _fixGenerators)
            {
                if (generator.CanHandle(suggestion))
                {
                    var generatorFixes = await generator.GenerateFixesAsync(suggestion, cancellationToken);
                    fixes.AddRange(generatorFixes);
                }
            }
            
            return fixes;
        }
        
        private string[] GenerateRecommendedOrder(IReadOnlyList<AutoFix> fixes)
        {
            // Order fixes by severity and confidence
            return fixes
                .OrderByDescending(f => f.Severity)
                .ThenByDescending(f => f.ConfidenceScore)
                .Select(f => f.Id)
                .ToArray();
        }

        private string[] GenerateWarnings(IReadOnlyList<AutoFix> fixes)
        {
            var warnings = new List<string>();
            
            var highRiskFixes = fixes.Where(f => f.RiskLevel == "High").ToList();
            if (highRiskFixes.Any())
            {
                warnings.Add($"{highRiskFixes.Count} high-risk fixes require careful review");
            }
            
            var conflictingFixes = fixes.Where(f => f.Conflicts.Any()).ToList();
            if (conflictingFixes.Any())
            {
                warnings.Add($"{conflictingFixes.Count} fixes have potential conflicts");
            }
            
            var dependentFixes = fixes.Where(f => f.Dependencies.Any()).ToList();
            if (dependentFixes.Any())
            {
                warnings.Add($"{dependentFixes.Count} fixes have dependencies");
            }
            
            return warnings.ToArray();
        }

        private string[] GenerateNotes(IReadOnlyList<AutoFix> fixes)
        {
            var notes = new List<string>();
            
            var automaticFixes = fixes.Where(f => f.CanApplyAutomatically).ToList();
            if (automaticFixes.Any())
            {
                notes.Add($"{automaticFixes.Count} fixes can be applied automatically");
            }
            
            var manualFixes = fixes.Where(f => !f.CanApplyAutomatically).ToList();
            if (manualFixes.Any())
            {
                notes.Add($"{manualFixes.Count} fixes require manual intervention");
            }
            
            var highConfidenceFixes = fixes.Where(f => f.ConfidenceScore >= 80).ToList();
            if (highConfidenceFixes.Any())
            {
                notes.Add($"{highConfidenceFixes.Count} fixes have high confidence scores");
            }
            
            return notes.ToArray();
        }
    }
}
