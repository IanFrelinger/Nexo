using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Tests for DTO validation
    /// </summary>
    public class DTOValidationTests
    {
        [Test]
        public async Task DTOs_ShouldBeValid()
        {
            // Test DesignBrief
            var designBrief = new DesignBrief(
                Description: "Test brief",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 0.5f,
                Seed: 123
            );
            
            Assert.IsNotNull(designBrief.Id, "DesignBrief should have ID");
            Assert.IsNotNull(designBrief.Description, "DesignBrief should have description");
            Assert.IsTrue(designBrief.TargetDurationMinutes > 0, "DesignBrief should have positive duration");
            
            // Test GamePlan
            var gamePlan = new GamePlan
            {
                Id = "test-plan",
                Genre = "Platformer",
                Description = "Test plan",
                CoreMechanics = new[] { "Jump", "Move" },
                EstimatedDurationMinutes = 5
            };
            
            Assert.IsNotNull(gamePlan.Id, "GamePlan should have ID");
            Assert.IsNotNull(gamePlan.Genre, "GamePlan should have genre");
            Assert.IsTrue(gamePlan.CoreMechanics.Length > 0, "GamePlan should have mechanics");
            
            // Test WorldLayout
            var worldLayout = new WorldLayout
            {
                Id = "test-layout",
                Name = "Test Layout",
                GridSize = new Vector2Int(10, 10)
            };
            
            Assert.IsNotNull(worldLayout.Id, "WorldLayout should have ID");
            Assert.IsTrue(worldLayout.GridSize.X > 0, "WorldLayout should have positive grid size");
            
            // Test InteractionGraph
            var interactionGraph = new InteractionGraph
            {
                Id = "test-graph",
                Name = "Test Graph"
            };
            
            Assert.IsNotNull(interactionGraph.Id, "InteractionGraph should have ID");
            
            // Test ContentBundle
            var contentBundle = new ContentBundle
            {
                Id = "test-bundle",
                Name = "Test Bundle"
            };
            
            Assert.IsNotNull(contentBundle.Id, "ContentBundle should have ID");
        }
    }
}
