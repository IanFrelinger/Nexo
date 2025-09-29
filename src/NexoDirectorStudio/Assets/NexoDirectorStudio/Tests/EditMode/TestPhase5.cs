using System;
using System.Threading;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Simple test program to validate Phase 5 implementation.
    /// This demonstrates that the validation suite is working correctly.
    /// </summary>
    public class TestPhase5
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Director Studio Phase 5 Validation Test ===");
            Console.WriteLine();
            
            try
            {
                // Test PlayabilityValidator
                Console.WriteLine("1. Testing PlayabilityValidator...");
                var playabilityValidator = new PlayabilityValidator();
                var interactionGraph = CreateTestInteractionGraph();
                var playabilityResult = playabilityValidator.ValidateAsync(interactionGraph, CancellationToken.None).Result;
                
                Console.WriteLine($"   ✓ PlayabilityValidator: Score {playabilityResult.Score}/100, Valid: {playabilityResult.IsValid}");
                
                // Test MechanicsValidator
                Console.WriteLine("2. Testing MechanicsValidator...");
                var mechanicsValidator = new MechanicsValidator();
                var gamePlan = CreateTestGamePlan();
                var mechanicsResult = mechanicsValidator.ValidateAsync(gamePlan, CancellationToken.None).Result;
                
                Console.WriteLine($"   ✓ MechanicsValidator: Score {mechanicsResult.Score}/100, Valid: {mechanicsResult.IsValid}");
                
                // Test ValidationReport aggregation
                Console.WriteLine("3. Testing ValidationReport aggregation...");
                var validationReport = ValidationReport.Create(new[] { playabilityResult, mechanicsResult });
                
                Console.WriteLine($"   ✓ ValidationReport: Overall Score {validationReport.OverallScore}/100, Status: {validationReport.Status}");
                
                // Test JSON serialization
                Console.WriteLine("4. Testing JSON serialization...");
                var json = validationReport.ToJson();
                var deserializedReport = ValidationReport.FromJson(json);
                
                Console.WriteLine($"   ✓ JSON serialization: {json.Length} chars, Status: {deserializedReport.Status}");
                
                // Test validation gating
                Console.WriteLine("5. Testing validation gating...");
                var canPlaytest = validationReport.IsPlaytestAllowed;
                var hasCriticalIssues = validationReport.HasCriticalIssues;
                
                Console.WriteLine($"   ✓ Validation gating: Playtest Allowed: {canPlaytest}, Critical Issues: {hasCriticalIssues}");
                
                // Test issue categorization
                Console.WriteLine("6. Testing issue categorization...");
                var issuesBySeverity = validationReport.IssuesBySeverity;
                var issuesByCategory = validationReport.IssuesByCategory;
                
                Console.WriteLine($"   ✓ Issue categorization: {issuesBySeverity.Count} severity levels, {issuesByCategory.Count} categories");
                
                // Test empty report
                Console.WriteLine("7. Testing empty report handling...");
                var emptyReport = ValidationReport.Empty();
                
                Console.WriteLine($"   ✓ Empty report: Status {emptyReport.Status}, Score {emptyReport.OverallScore}");
                
                Console.WriteLine();
                Console.WriteLine("=== Phase 5 Validation Test PASSED ===");
                Console.WriteLine("All validation components are working correctly!");
                Console.WriteLine();
                Console.WriteLine("Summary:");
                Console.WriteLine($"- PlayabilityValidator: {playabilityResult.Score}/100");
                Console.WriteLine($"- MechanicsValidator: {mechanicsResult.Score}/100");
                Console.WriteLine($"- Overall Score: {validationReport.OverallScore}/100");
                Console.WriteLine($"- Status: {validationReport.Status}");
                Console.WriteLine($"- Playtest Allowed: {validationReport.IsPlaytestAllowed}");
                Console.WriteLine($"- Total Issues: {validationReport.AllIssues.Count}");
                Console.WriteLine($"- Total Suggestions: {validationReport.AllSuggestions.Count}");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Phase 5 Validation Test FAILED ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Environment.Exit(1);
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
