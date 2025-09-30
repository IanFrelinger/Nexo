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
        
        public ProposeAutoFixesCommand(GenreProfileService profileService)
        {
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
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
            
            // Calculate overall confidence
            var overallConfidence = fixCount > 0 ? totalConfidence / fixCount : 0;
            
            // Generate summary
            var summary = GenerateSummary(proposedFixes);
            var description = GenerateDescription(proposedFixes);
            var estimatedEffort = CalculateEstimatedEffort(proposedFixes);
            
            return new AutoFixProposal
            {
                OriginalReport = validationReport,
                ProposedFixes = proposedFixes,
                EstimatedEffort = estimatedEffort,
                ConfidenceScore = overallConfidence,
                CanApplyAllFixes = canApplyAllFixes,
                Summary = summary,
                Description = description
            };
        }
        
        private async Task<IReadOnlyList<AutoFix>> ProposeFixesForIssue(ValidationIssue issue, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();
            
            // Analyze issue severity and propose appropriate fixes
            switch (issue.Severity)
            {
                case ValidationSeverity.Critical:
                    fixes.AddRange(await ProposeCriticalFixes(issue, cancellationToken));
                    break;
                case ValidationSeverity.Error:
                    fixes.AddRange(await ProposeErrorFixes(issue, cancellationToken));
                    break;
                case ValidationSeverity.Warning:
                    fixes.AddRange(await ProposeWarningFixes(issue, cancellationToken));
                    break;
                case ValidationSeverity.Info:
                    fixes.AddRange(await ProposeInfoFixes(issue, cancellationToken));
                    break;
            }
            
            return fixes;
        }
        
        private async Task<IReadOnlyList<AutoFix>> ProposeFixesForSuggestion(ValidationSuggestion suggestion, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();
            
            // Analyze suggestion priority and propose appropriate fixes
            switch (suggestion.Priority)
            {
                case 5: // High priority
                    fixes.AddRange(await ProposeHighPriorityFixes(suggestion, cancellationToken));
                    break;
                case 4: // Medium-high priority
                    fixes.AddRange(await ProposeMediumHighPriorityFixes(suggestion, cancellationToken));
                    break;
                case 3: // Medium priority
                    fixes.AddRange(await ProposeMediumPriorityFixes(suggestion, cancellationToken));
                    break;
                case 2: // Low-medium priority
                    fixes.AddRange(await ProposeLowMediumPriorityFixes(suggestion, cancellationToken));
                    break;
                case 1: // Low priority
                    fixes.AddRange(await ProposeLowPriorityFixes(suggestion, cancellationToken));
                    break;
            }
            
            return fixes;
        }
        
        private async Task<IReadOnlyList<AutoFix>> ProposeCriticalFixes(ValidationIssue issue, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();
            
            // Critical issues require immediate fixes
            if (issue.Category == "Mechanics")
            {
                fixes.Add(new AutoFix
                {
                    TargetIssue = issue,
                    FixType = AutoFixType.AddMechanics,
                    Changes = new[]
                    {
                        new GamePlanChange
                        {
                            ChangeType = GamePlanChangeType.Add,
                            PropertyPath = "CoreMechanics",
                            NewValue = ExtractMechanicsFromIssue(issue),
                            Context = "Adding missing critical mechanics"
                        }
                    },
                    Priority = 5,
                    Effort = "Low",
                    ConfidenceScore = 90,
                    CanApplyAutomatically = true,
                    Title = $"Add Critical Mechanics: {issue.Title}",
                    Description = $"Add the missing critical mechanics identified in: {issue.Description}"
                });
            }
            else if (issue.Category == "Assets")
            {
                fixes.Add(new AutoFix
                {
                    TargetIssue = issue,
                    FixType = AutoFixType.AddAssets,
                    Changes = new[]
                    {
                        new GamePlanChange
                        {
                            ChangeType = GamePlanChangeType.Add,
                            PropertyPath = "RequiredAssets",
                            NewValue = CreateAssetFromIssue(issue),
                            Context = "Adding missing critical assets"
                        }
                    },
                    Priority = 5,
                    Effort = "Medium",
                    ConfidenceScore = 85,
                    CanApplyAutomatically = true,
                    Title = $"Add Critical Assets: {issue.Title}",
                    Description = $"Add the missing critical assets identified in: {issue.Description}"
                });
            }
            
            return fixes;
        }
        
        private async Task<IReadOnlyList<AutoFix>> ProposeErrorFixes(ValidationIssue issue, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();
            
            // Error issues require fixes but with lower priority
            if (issue.Category == "Mechanics")
            {
                fixes.Add(new AutoFix
                {
                    TargetIssue = issue,
                    FixType = AutoFixType.AddMechanics,
                    Changes = new[]
                    {
                        new GamePlanChange
                        {
                            ChangeType = GamePlanChangeType.Add,
                            PropertyPath = "CoreMechanics",
                            NewValue = ExtractMechanicsFromIssue(issue),
                            Context = "Adding missing mechanics"
                        }
                    },
                    Priority = 4,
                    Effort = "Low",
                    ConfidenceScore = 80,
                    CanApplyAutomatically = true,
                    Title = $"Add Mechanics: {issue.Title}",
                    Description = $"Add the missing mechanics identified in: {issue.Description}"
                });
            }
            
            return fixes;
        }
        
        private async Task<IReadOnlyList<AutoFix>> ProposeWarningFixes(ValidationIssue issue, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();
            
            // Warning issues can be addressed with lower priority
            if (issue.Category == "Difficulty")
            {
                fixes.Add(new AutoFix
                {
                    TargetIssue = issue,
                    FixType = AutoFixType.AdjustDifficulty,
                    Changes = new[]
                    {
                        new GamePlanChange
                        {
                            ChangeType = GamePlanChangeType.Update,
                            PropertyPath = "SourceBrief.DifficultyLevel",
                            NewValue = CalculateOptimalDifficulty(issue),
                            Context = "Adjusting difficulty level"
                        }
                    },
                    Priority = 3,
                    Effort = "Low",
                    ConfidenceScore = 70,
                    CanApplyAutomatically = true,
                    Title = $"Adjust Difficulty: {issue.Title}",
                    Description = $"Adjust the difficulty level as suggested in: {issue.Description}"
                });
            }
            
            return fixes;
        }
        
        private async Task<IReadOnlyList<AutoFix>> ProposeInfoFixes(ValidationIssue issue, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();
            
            // Info issues are suggestions for improvement
            if (issue.Category == "Duration")
            {
                fixes.Add(new AutoFix
                {
                    TargetIssue = issue,
                    FixType = AutoFixType.AdjustDuration,
                    Changes = new[]
                    {
                        new GamePlanChange
                        {
                            ChangeType = GamePlanChangeType.Update,
                            PropertyPath = "EstimatedDurationMinutes",
                            NewValue = CalculateOptimalDuration(issue),
                            Context = "Adjusting estimated duration"
                        }
                    },
                    Priority = 2,
                    Effort = "Low",
                    ConfidenceScore = 60,
                    CanApplyAutomatically = true,
                    Title = $"Adjust Duration: {issue.Title}",
                    Description = $"Adjust the duration as suggested in: {issue.Description}"
                });
            }
            
            return fixes;
        }
        
        private async Task<IReadOnlyList<AutoFix>> ProposeHighPriorityFixes(ValidationSuggestion suggestion, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();
            
            // High priority suggestions get immediate attention
            if (suggestion.Category == "Mechanics")
            {
                fixes.Add(new AutoFix
                {
                    TargetIssue = new ValidationIssue
                    {
                        Severity = ValidationSeverity.Warning,
                        Category = suggestion.Category,
                        Title = suggestion.Title,
                        Description = suggestion.Description
                    },
                    FixType = AutoFixType.AddMechanics,
                    Changes = new[]
                    {
                        new GamePlanChange
                        {
                            ChangeType = GamePlanChangeType.Add,
                            PropertyPath = "CoreMechanics",
                            NewValue = ExtractMechanicsFromSuggestion(suggestion),
                            Context = "Adding suggested mechanics"
                        }
                    },
                    Priority = 4,
                    Effort = suggestion.Effort,
                    ConfidenceScore = 75,
                    CanApplyAutomatically = true,
                    Title = $"Add Suggested Mechanics: {suggestion.Title}",
                    Description = suggestion.Description
                });
            }
            
            return fixes;
        }
        
        private async Task<IReadOnlyList<AutoFix>> ProposeMediumHighPriorityFixes(ValidationSuggestion suggestion, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();
            
            // Medium-high priority suggestions get attention
            if (suggestion.Category == "Assets")
            {
                fixes.Add(new AutoFix
                {
                    TargetIssue = new ValidationIssue
                    {
                        Severity = ValidationSeverity.Info,
                        Category = suggestion.Category,
                        Title = suggestion.Title,
                        Description = suggestion.Description
                    },
                    FixType = AutoFixType.AddAssets,
                    Changes = new[]
                    {
                        new GamePlanChange
                        {
                            ChangeType = GamePlanChangeType.Add,
                            PropertyPath = "RequiredAssets",
                            NewValue = CreateAssetFromSuggestion(suggestion),
                            Context = "Adding suggested assets"
                        }
                    },
                    Priority = 3,
                    Effort = suggestion.Effort,
                    ConfidenceScore = 70,
                    CanApplyAutomatically = true,
                    Title = $"Add Suggested Assets: {suggestion.Title}",
                    Description = suggestion.Description
                });
            }
            
            return fixes;
        }
        
        private async Task<IReadOnlyList<AutoFix>> ProposeMediumPriorityFixes(ValidationSuggestion suggestion, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();
            
            // Medium priority suggestions are considered
            if (suggestion.Category == "Pacing")
            {
                fixes.Add(new AutoFix
                {
                    TargetIssue = new ValidationIssue
                    {
                        Severity = ValidationSeverity.Info,
                        Category = suggestion.Category,
                        Title = suggestion.Title,
                        Description = suggestion.Description
                    },
                    FixType = AutoFixType.AddNarrativeBeats,
                    Changes = new[]
                    {
                        new GamePlanChange
                        {
                            ChangeType = GamePlanChangeType.Add,
                            PropertyPath = "NarrativeBeats",
                            NewValue = ExtractNarrativeBeatsFromSuggestion(suggestion),
                            Context = "Adding suggested narrative beats"
                        }
                    },
                    Priority = 3,
                    Effort = suggestion.Effort,
                    ConfidenceScore = 65,
                    CanApplyAutomatically = true,
                    Title = $"Add Narrative Beats: {suggestion.Title}",
                    Description = suggestion.Description
                });
            }
            
            return fixes;
        }
        
        private async Task<IReadOnlyList<AutoFix>> ProposeLowMediumPriorityFixes(ValidationSuggestion suggestion, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();
            
            // Low-medium priority suggestions are optional
            if (suggestion.Category == "Content")
            {
                fixes.Add(new AutoFix
                {
                    TargetIssue = new ValidationIssue
                    {
                        Severity = ValidationSeverity.Info,
                        Category = suggestion.Category,
                        Title = suggestion.Title,
                        Description = suggestion.Description
                    },
                    FixType = AutoFixType.ModifyPlayerExperience,
                    Changes = new[]
                    {
                        new GamePlanChange
                        {
                            ChangeType = GamePlanChangeType.Add,
                            PropertyPath = "PlayerExperience",
                            NewValue = ExtractPlayerExperienceFromSuggestion(suggestion),
                            Context = "Adding suggested player experience elements"
                        }
                    },
                    Priority = 2,
                    Effort = suggestion.Effort,
                    ConfidenceScore = 60,
                    CanApplyAutomatically = true,
                    Title = $"Add Player Experience: {suggestion.Title}",
                    Description = suggestion.Description
                });
            }
            
            return fixes;
        }
        
        private async Task<IReadOnlyList<AutoFix>> ProposeLowPriorityFixes(ValidationSuggestion suggestion, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();
            
            // Low priority suggestions are optional improvements
            if (suggestion.Category == "Accessibility")
            {
                fixes.Add(new AutoFix
                {
                    TargetIssue = new ValidationIssue
                    {
                        Severity = ValidationSeverity.Info,
                        Category = suggestion.Category,
                        Title = suggestion.Title,
                        Description = suggestion.Description
                    },
                    FixType = AutoFixType.ModifyPlayerExperience,
                    Changes = new[]
                    {
                        new GamePlanChange
                        {
                            ChangeType = GamePlanChangeType.Add,
                            PropertyPath = "PlayerExperience",
                            NewValue = ExtractAccessibilityFromSuggestion(suggestion),
                            Context = "Adding suggested accessibility elements"
                        }
                    },
                    Priority = 1,
                    Effort = suggestion.Effort,
                    ConfidenceScore = 55,
                    CanApplyAutomatically = true,
                    Title = $"Add Accessibility: {suggestion.Title}",
                    Description = suggestion.Description
                });
            }
            
            return fixes;
        }
        
        private static string[] ExtractMechanicsFromIssue(ValidationIssue issue)
        {
            // Extract mechanics from issue description
            var mechanics = new List<string>();
            
            if (issue.Description.Contains("Shoot"))
                mechanics.Add("Shoot");
            if (issue.Description.Contains("Jump"))
                mechanics.Add("Jump");
            if (issue.Description.Contains("Move"))
                mechanics.Add("Move");
            if (issue.Description.Contains("Aim"))
                mechanics.Add("Aim");
            if (issue.Description.Contains("Reload"))
                mechanics.Add("Reload");
            
            return mechanics.ToArray();
        }
        
        private static AssetRequirement CreateAssetFromIssue(ValidationIssue issue)
        {
            return new AssetRequirement
            {
                AssetType = ExtractAssetTypeFromIssue(issue),
                Name = ExtractAssetNameFromIssue(issue),
                Description = issue.Description,
                IsRequired = issue.Severity >= ValidationSeverity.Error,
                Priority = issue.Severity switch
                {
                    ValidationSeverity.Critical => 5,
                    ValidationSeverity.Error => 4,
                    ValidationSeverity.Warning => 3,
                    _ => 2
                }
            };
        }
        
        private static string ExtractAssetTypeFromIssue(ValidationIssue issue)
        {
            if (issue.Description.Contains("Weapon"))
                return "Weapon";
            if (issue.Description.Contains("Enemy"))
                return "Enemy";
            if (issue.Description.Contains("Platform"))
                return "Platform";
            if (issue.Description.Contains("Collectible"))
                return "Collectible";
            if (issue.Description.Contains("Audio"))
                return "Audio";
            
            return "Unknown";
        }
        
        private static string ExtractAssetNameFromIssue(ValidationIssue issue)
        {
            return issue.Title.Replace("Missing ", "").Replace(" Assets", "");
        }
        
        private static int CalculateOptimalDifficulty(ValidationIssue issue)
        {
            // Simple difficulty calculation based on issue description
            if (issue.Description.Contains("too easy"))
                return 4;
            if (issue.Description.Contains("too hard"))
                return 2;
            
            return 3; // Default moderate difficulty
        }
        
        private static int CalculateOptimalDuration(ValidationIssue issue)
        {
            // Simple duration calculation based on issue description
            if (issue.Description.Contains("too short"))
                return 8;
            if (issue.Description.Contains("too long"))
                return 3;
            
            return 5; // Default duration
        }
        
        private static string[] ExtractMechanicsFromSuggestion(ValidationSuggestion suggestion)
        {
            var mechanics = new List<string>();
            
            if (suggestion.Description.Contains("Shoot"))
                mechanics.Add("Shoot");
            if (suggestion.Description.Contains("Jump"))
                mechanics.Add("Jump");
            if (suggestion.Description.Contains("Move"))
                mechanics.Add("Move");
            
            return mechanics.ToArray();
        }
        
        private static AssetRequirement CreateAssetFromSuggestion(ValidationSuggestion suggestion)
        {
            return new AssetRequirement
            {
                AssetType = "Suggested",
                Name = suggestion.Title,
                Description = suggestion.Description,
                IsRequired = false,
                Priority = suggestion.Priority
            };
        }
        
        private static string[] ExtractNarrativeBeatsFromSuggestion(ValidationSuggestion suggestion)
        {
            return new[] { "Introduction", "Development", "Climax", "Resolution" };
        }
        
        private static string[] ExtractPlayerExperienceFromSuggestion(ValidationSuggestion suggestion)
        {
            var experiences = new List<string>();
            
            if (suggestion.Description.Contains("intense"))
                experiences.Add("Intense");
            if (suggestion.Description.Contains("relaxing"))
                experiences.Add("Relaxing");
            if (suggestion.Description.Contains("challenging"))
                experiences.Add("Challenging");
            
            return experiences.ToArray();
        }
        
        private static string[] ExtractAccessibilityFromSuggestion(ValidationSuggestion suggestion)
        {
            return new[] { "Accessible", "Inclusive" };
        }
        
        private static string GenerateSummary(IReadOnlyList<AutoFix> fixes)
        {
            var fixCount = fixes.Count;
            var autoFixCount = fixes.Count(f => f.CanApplyAutomatically);
            var criticalCount = fixes.Count(f => f.Priority >= 4);
            
            return $"Proposed {fixCount} fixes ({autoFixCount} automatic, {criticalCount} critical)";
        }
        
        private static string GenerateDescription(IReadOnlyList<AutoFix> fixes)
        {
            var descriptions = fixes.Select(f => $"- {f.Title} ({f.Effort})").ToArray();
            return string.Join("\n", descriptions);
        }
        
        private static string CalculateEstimatedEffort(IReadOnlyList<AutoFix> fixes)
        {
            var totalEffort = fixes.Sum(f => f.Effort switch
            {
                "Low" => 1,
                "Medium" => 2,
                "High" => 3,
                _ => 1
            });
            
            return totalEffort switch
            {
                <= 3 => "Low",
                <= 6 => "Medium",
                <= 9 => "High",
                _ => "Very High"
            };
        }
    }
}
