using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using NUnit.Framework;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the PlayabilityValidator.
    /// </summary>
    [TestFixture]
    public class Unit_Playability
    {
        private PlayabilityValidator _validator;
        
        [SetUp]
        public void SetUp()
        {
            _validator = new PlayabilityValidator();
        }
        
        [Test]
        public void ValidateAsync_WithValidGraph_ShouldPass()
        {
            // Arrange
            var graph = CreateValidInteractionGraph();
            
            // Act
            var result = _validator.ValidateAsync(graph, CancellationToken.None).Result;
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid graph should pass validation");
            Assert.IsTrue(result.Score > 80, "Valid graph should have high score");
            Assert.AreEqual(0, result.Issues.Count, "Valid graph should have no issues");
        }
        
        [Test]
        public void ValidateAsync_WithNoEntryPoints_ShouldFail()
        {
            // Arrange
            var graph = CreateValidInteractionGraph();
            graph = graph with { EntryPointIds = Array.Empty<string>() };
            
            // Act
            var result = _validator.ValidateAsync(graph, CancellationToken.None).Result;
            
            // Assert
            Assert.IsFalse(result.IsValid, "Graph with no entry points should fail validation");
            Assert.IsTrue(result.Score < 50, "Graph with no entry points should have low score");
            Assert.IsTrue(result.Issues.Any(i => i.Title == "No Entry Points"), "Should have no entry points issue");
        }
        
        [Test]
        public void ValidateAsync_WithNoGoalNodes_ShouldFail()
        {
            // Arrange
            var graph = CreateValidInteractionGraph();
            var nodesWithoutGoal = graph.Nodes.Where(n => n.NodeType != "Goal").ToList();
            graph = graph with { Nodes = nodesWithoutGoal };
            
            // Act
            var result = _validator.ValidateAsync(graph, CancellationToken.None).Result;
            
            // Assert
            Assert.IsFalse(result.IsValid, "Graph with no goal nodes should fail validation");
            Assert.IsTrue(result.Score < 60, "Graph with no goal nodes should have low score");
            Assert.IsTrue(result.Issues.Any(i => i.Title == "No Goal Nodes"), "Should have no goal nodes issue");
        }
        
        [Test]
        public void ValidateAsync_WithUnreachableGoal_ShouldFail()
        {
            // Arrange
            var graph = CreateValidInteractionGraph();
            var unreachableGoal = new InteractionNode
            {
                Id = "unreachable-goal",
                NodeType = "Goal",
                WorldPosition = new Vector3(100, 100, 100),
                Name = "Unreachable Goal",
                Description = "A goal that cannot be reached",
                Actions = Array.Empty<InteractionAction>(),
                IsRepeatable = false,
                Priority = 5
            };
            
            var nodesWithUnreachableGoal = graph.Nodes.Concat(new[] { unreachableGoal }).ToList();
            graph = graph with { Nodes = nodesWithUnreachableGoal };
            
            // Act
            var result = _validator.ValidateAsync(graph, CancellationToken.None).Result;
            
            // Assert
            Assert.IsFalse(result.IsValid, "Graph with unreachable goal should fail validation");
            Assert.IsTrue(result.Issues.Any(i => i.Title == "Unreachable Goal"), "Should have unreachable goal issue");
        }
        
        [Test]
        public void ValidateAsync_WithIsolatedNode_ShouldHaveWarning()
        {
            // Arrange
            var graph = CreateValidInteractionGraph();
            var isolatedNode = new InteractionNode
            {
                Id = "isolated-node",
                NodeType = "Collectible",
                WorldPosition = new Vector3(50, 50, 50),
                Name = "Isolated Collectible",
                Description = "A collectible with no connections",
                Actions = Array.Empty<InteractionAction>(),
                IsRepeatable = false,
                Priority = 3
            };
            
            var nodesWithIsolated = graph.Nodes.Concat(new[] { isolatedNode }).ToList();
            graph = graph with { Nodes = nodesWithIsolated };
            
            // Act
            var result = _validator.ValidateAsync(graph, CancellationToken.None).Result;
            
            // Assert
            Assert.IsTrue(result.IsValid, "Graph with isolated node should still be valid");
            Assert.IsTrue(result.Issues.Any(i => i.Title == "Isolated Node"), "Should have isolated node warning");
        }
        
        [Test]
        public void ValidateAsync_WithSoftLock_ShouldHaveError()
        {
            // Arrange
            var graph = CreateValidInteractionGraph();
            var nonRepeatableNode = new InteractionNode
            {
                Id = "non-repeatable",
                NodeType = "Collectible",
                WorldPosition = new Vector3(10, 10, 10),
                Name = "Non-Repeatable Collectible",
                Description = "A collectible that can only be collected once",
                Actions = Array.Empty<InteractionAction>(),
                IsRepeatable = false,
                Priority = 3
            };
            
            var nodesWithNonRepeatable = graph.Nodes.Concat(new[] { nonRepeatableNode }).ToList();
            graph = graph with { Nodes = nodesWithNonRepeatable };
            
            // Act
            var result = _validator.ValidateAsync(graph, CancellationToken.None).Result;
            
            // Assert
            Assert.IsTrue(result.IsValid, "Graph with soft lock should still be valid");
            Assert.IsTrue(result.Issues.Any(i => i.Title == "Potential Soft Lock"), "Should have soft lock warning");
        }
        
        [Test]
        public void ValidateAsync_WithInvalidInput_ShouldFail()
        {
            // Arrange
            var invalidInput = "not an InteractionGraph";
            
            // Act
            var result = _validator.ValidateAsync(invalidInput, CancellationToken.None).Result;
            
            // Assert
            Assert.IsFalse(result.IsValid, "Invalid input should fail validation");
            Assert.AreEqual(0, result.Score, "Invalid input should have zero score");
            Assert.IsTrue(result.Message.Contains("Invalid input type"), "Should have invalid input message");
        }
        
        [Test]
        public void ValidateAsync_ShouldGenerateSuggestions()
        {
            // Arrange
            var graph = CreateValidInteractionGraph();
            
            // Act
            var result = _validator.ValidateAsync(graph, CancellationToken.None).Result;
            
            // Assert
            Assert.IsTrue(result.Suggestions.Count > 0, "Should generate suggestions");
            Assert.IsTrue(result.Suggestions.Any(s => s.Category == "Playability"), "Should have playability suggestions");
        }
        
        [Test]
        public void ValidateAsync_ShouldHaveValidTimestamp()
        {
            // Arrange
            var graph = CreateValidInteractionGraph();
            var beforeValidation = DateTimeOffset.UtcNow;
            
            // Act
            var result = _validator.ValidateAsync(graph, CancellationToken.None).Result;
            
            // Assert
            Assert.IsTrue(result.ValidatedAt >= beforeValidation, "Validation timestamp should be after start");
            Assert.IsTrue(result.ValidatedAt <= DateTimeOffset.UtcNow, "Validation timestamp should be before now");
        }
        
        [Test]
        public void ValidateAsync_ShouldHaveValidDetails()
        {
            // Arrange
            var graph = CreateValidInteractionGraph();
            
            // Act
            var result = _validator.ValidateAsync(graph, CancellationToken.None).Result;
            
            // Assert
            Assert.IsNotNull(result.Details, "Details should not be null");
            Assert.IsTrue(result.Details.Contains("Total Nodes"), "Details should contain node count");
            Assert.IsTrue(result.Details.Contains("Total Connections"), "Details should contain connection count");
        }
        
        private static InteractionGraph CreateValidInteractionGraph()
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
            
            var collectibleNode = new InteractionNode
            {
                Id = "collectible-1",
                NodeType = "Collectible",
                WorldPosition = new Vector3(5, 0, 0),
                Name = "Collectible 1",
                Description = "A collectible item",
                Actions = Array.Empty<InteractionAction>(),
                IsRepeatable = false,
                Priority = 3
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
            
            var nodes = new[] { spawnNode, collectibleNode, goalNode };
            
            var connections = new[]
            {
                new InteractionConnection
                {
                    Id = "conn-1",
                    SourceNodeId = spawnNode.Id,
                    TargetNodeId = collectibleNode.Id,
                    ConnectionType = "Success",
                    Weight = 1.0f
                },
                new InteractionConnection
                {
                    Id = "conn-2",
                    SourceNodeId = collectibleNode.Id,
                    TargetNodeId = goalNode.Id,
                    ConnectionType = "Success",
                    Weight = 1.0f
                }
            };
            
            return new InteractionGraph
            {
                Id = "test-graph",
                WorldLayoutId = "test-layout",
                Nodes = nodes,
                Connections = connections,
                Variables = Array.Empty<InteractionVariable>(),
                EntryPointIds = new[] { spawnNode.Id },
                Seed = 12345
            };
        }
    }
}
