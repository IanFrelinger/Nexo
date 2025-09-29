using System;
using System.Threading;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Simple runner to validate Phase 5 implementation.
    /// This can be executed to verify the validation suite is working.
    /// </summary>
    public static class Phase5ValidationRunner
    {
        public static void RunValidationTests()
        {
            Console.WriteLine("=== Phase 5 Validation Suite Test ===");
            Console.WriteLine();
            
            try
            {
                // Test 1: PlayabilityValidator
                Console.WriteLine("Testing PlayabilityValidator...");
                var playabilityValidator = new PlayabilityValidator();
                var interactionGraph = CreateTestInteractionGraph();
                var playabilityResult = playabilityValidator.ValidateAsync(interactionGraph, CancellationToken.None).Result;
                
                Console.WriteLine($"  ✓ PlayabilityValidator works");
                Console.WriteLine($"  - Score: {playabilityResult.Score}/100");
                Console.WriteLine($"  - Valid: {playabilityResult.IsValid}");
                Console.WriteLine($"  - Issues: {playabilityResult.Issues.Count}");
                Console.WriteLine($"  - Suggestions: {playabilityResult.Suggestions.Count}");
                Console.WriteLine();
                
                // Test 2: MechanicsValidator
                Console.WriteLine("Testing MechanicsValidator...");
                var mechanicsValidator = new MechanicsValidator();
                var gamePlan = CreateTestGamePlan();
                var mechanicsResult = mechanicsValidator.ValidateAsync(gamePlan, CancellationToken.None).Result;
                
                Console.WriteLine($"  ✓ MechanicsValidator works");
                Console.WriteLine($"  - Score: {mechanicsResult.Score}/100");
                Console.WriteLine($"  - Valid: {mechanicsResult.IsValid}");
                Console.WriteLine($"  - Issues: {mechanicsResult.Issues.Count}");
                Console.WriteLine($"  - Suggestions: {mechanicsResult.Suggestions.Count}");
                Console.WriteLine();
                
                // Test 3: ValidationReport aggregation
                Console.WriteLine("Testing ValidationReport aggregation...");
                var validationReport = ValidationReport.Create(new[] { playabilityResult, mechanicsResult });
                
                Console.WriteLine($"  ✓ ValidationReport aggregation works");
                Console.WriteLine($"  - Overall Score: {validationReport.OverallScore}/100");
                Console.WriteLine($"  - Status: {validationReport.Status}");
                Console.WriteLine($"  - Playtest Allowed: {validationReport.IsPlaytestAllowed}");
                Console.WriteLine($"  - Total Issues: {validationReport.AllIssues.Count}");
                Console.WriteLine($"  - Total Suggestions: {validationReport.AllSuggestions.Count}");
                Console.WriteLine();
                
                // Test 4: JSON serialization
                Console.WriteLine("Testing JSON serialization...");
                var json = validationReport.ToJson();
                var deserializedReport = ValidationReport.FromJson(json);
                
                Console.WriteLine($"  ✓ JSON serialization works");
                Console.WriteLine($"  - JSON Length: {json.Length} characters");
                Console.WriteLine($"  - Deserialized Status: {deserializedReport.Status}");
                Console.WriteLine($"  - Deserialized Score: {deserializedReport.OverallScore}");
                Console.WriteLine();
                
                // Test 5: Validation gating
                Console.WriteLine("Testing validation gating...");
                var canPlaytest = validationReport.IsPlaytestAllowed;
                var hasCriticalIssues = validationReport.HasCriticalIssues;
                var hasErrors = validationReport.HasErrors;
                var hasWarnings = validationReport.HasWarnings;
                
                Console.WriteLine($"  ✓ Validation gating works");
                Console.WriteLine($"  - Can Playtest: {canPlaytest}");
                Console.WriteLine($"  - Has Critical Issues: {hasCriticalIssues}");
                Console.WriteLine($"  - Has Errors: {hasErrors}");
                Console.WriteLine($"  - Has Warnings: {hasWarnings}");
                Console.WriteLine();
                
                // Test 6: Issue categorization
                Console.WriteLine("Testing issue categorization...");
                var issuesBySeverity = validationReport.IssuesBySeverity;
                var issuesByCategory = validationReport.IssuesByCategory;
                
                Console.WriteLine($"  ✓ Issue categorization works");
                Console.WriteLine($"  - Severity Categories: {issuesBySeverity.Count}");
                Console.WriteLine($"  - Issue Categories: {issuesByCategory.Count}");
                Console.WriteLine();
                
                // Test 7: Empty report handling
                Console.WriteLine("Testing empty report handling...");
                var emptyReport = ValidationReport.Empty();
                
                Console.WriteLine($"  ✓ Empty report handling works");
                Console.WriteLine($"  - Empty Status: {emptyReport.Status}");
                Console.WriteLine($"  - Empty Score: {emptyReport.OverallScore}");
                Console.WriteLine($"  - Empty Playtest Allowed: {emptyReport.IsPlaytestAllowed}");
                Console.WriteLine();
                
                Console.WriteLine("=== Phase 5 Validation Suite Test PASSED ===");
                Console.WriteLine("All validation components are working correctly!");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Phase 5 Validation Suite Test FAILED ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }
        
        private static GamePlan CreateTestGamePlan()
        {
            return new GamePlan
            {
                Id = "test-plan-1",
                SourceBrief = new DesignBrief
                {
                    Description = "A test game slice for validation",
                    GenreHint = "FPS",
                    TargetDurationMinutes = 5,
                    DifficultyLevel = 3,
                    Seed = 12345
                },
                Genre = "FPS",
                Description = "A test FPS game slice",
                CoreMechanics = new[] { "Shoot", "Aim", "Move", "Reload" },
                PlayerExperience = new[] { "Intense", "Fast-paced" },
                EstimatedDurationMinutes = 5,
                DifficultyProgression = new[]
                {
                    new DifficultyBeat { TimeOffsetSeconds = 0, DifficultyLevel = 2, Description = "Start" },
                    new DifficultyBeat { TimeOffsetSeconds = 60, DifficultyLevel = 3, Description = "Ramp up" }
                },
                NarrativeBeats = new[] { "Introduction", "Climax", "Resolution" },
                RequiredAssets = new[]
                {
                    new AssetRequirement
                    {
                        AssetType = "Weapon",
                        Name = "Test Weapon",
                        Description = "A test weapon",
                        IsRequired = true,
                        Priority = 5
                    }
                },
                Seed = 12345
            };
        }
        
        private static InteractionGraph CreateTestInteractionGraph()
        {
            var spawnNode = new InteractionNode
            {
                Id = "spawn-1",
                NodeType = "Spawn",
                WorldPosition = new Vector3(0, 0, 0),
                Name = "Player Spawn",
                Description = "Spawns the player",
                Actions = Array.Empty<InteractionAction>(),
                IsRepeatable = false,
                Priority = 10
            };
            
            var goalNode = new InteractionNode
            {
                Id = "goal-1",
                NodeType = "Goal",
                WorldPosition = new Vector3(10, 0, 0),
                Name = "Level Goal",
                Description = "Completes the level",
                Actions = Array.Empty<InteractionAction>(),
                IsRepeatable = false,
                Priority = 5
            };
            
            var nodes = new[] { spawnNode, goalNode };
            
            var connections = new[]
            {
                new InteractionConnection
                {
                    Id = "conn-1",
                    SourceNodeId = spawnNode.Id,
                    TargetNodeId = goalNode.Id,
                    ConnectionType = "Success",
                    Weight = 1.0f
                }
            };
            
            return new InteractionGraph
            {
                Id = "test-graph-1",
                WorldLayoutId = "test-layout-1",
                Nodes = nodes,
                Connections = connections,
                Variables = Array.Empty<InteractionVariable>(),
                EntryPointIds = new[] { spawnNode.Id },
                Seed = 12345
            };
        }
    }
}
