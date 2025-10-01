using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Unit tests to verify deterministic behavior of the Director Studio pipeline.
    /// Same brief + seed should always produce identical results.
    /// </summary>
    [TestFixture]
    public class Unit_Determinism
    {
        [Test]
        public void SameBriefAndSeed_ShouldProduceIdenticalGamePlanHash()
        {
            // Arrange
            var command = new PlanGameSliceCommand();
            var brief = new DesignBrief(
                Description: "A challenging platformer with jumping mechanics",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 4,
                Seed: 12345
            );
            
            // Act - Run the command multiple times with the same input
            var result1 = command.ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None).Result;
            var result2 = command.ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None).Result;
            
            // Assert - Results should be identical
            Assert.AreEqual(result1.Hash, result2.Hash, "Game plan hash should be identical for same input");
            Assert.AreEqual(result1.Genre, result2.Genre, "Genre should be identical");
            Assert.AreEqual(result1.CoreMechanics.Count, result2.CoreMechanics.Count, "Mechanics count should be identical");
            Assert.AreEqual(result1.EstimatedDurationMinutes, result2.EstimatedDurationMinutes, "Duration should be identical");
        }
        
        [Test]
        public void DifferentSeeds_ShouldProduceDifferentResults()
        {
            // Arrange
            var command = new PlanGameSliceCommand();
            var brief1 = new DesignBrief(
                Description: "A challenging platformer with jumping mechanics",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 4,
                Seed: 12345
            );
            var brief2 = brief1 with { Seed = 54321 };
            
            // Act
            var result1 = command.ExecuteAsync(new IPlanGameSliceCommand.Input(brief1), CancellationToken.None).Result;
            var result2 = command.ExecuteAsync(new IPlanGameSliceCommand.Input(brief2), CancellationToken.None).Result;
            
            // Assert - Results should be different
            Assert.AreNotEqual(result1.Hash, result2.Hash, "Different seeds should produce different hashes");
        }
        
        [Test]
        public void SameSeedDifferentDescription_ShouldProduceDifferentResults()
        {
            // Arrange
            var command = new PlanGameSliceCommand();
            var brief1 = new DesignBrief(
                Description: "A challenging platformer with jumping mechanics",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 4,
                Seed: 12345
            );
            var brief2 = new DesignBrief(
                Description: "A relaxing puzzle game with blocks",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 4,
                Seed: 12345
            );
            
            // Act
            var result1 = command.ExecuteAsync(new IPlanGameSliceCommand.Input(brief1), CancellationToken.None).Result;
            var result2 = command.ExecuteAsync(new IPlanGameSliceCommand.Input(brief2), CancellationToken.None).Result;
            
            // Assert - Results should be different
            Assert.AreNotEqual(result1.Hash, result2.Hash, "Different descriptions should produce different hashes");
        }
        
        [Test]
        public void GamePlanHash_ShouldBeStableAcrossRuns()
        {
            // Arrange
            var command = new PlanGameSliceCommand();
            var brief = new DesignBrief(
                Description: "A fast-paced FPS with multiple weapons",
                GenreHint: "FPS",
                TargetDurationMinutes: 3,
                DifficultyLevel: 5,
                Seed: 99999
            );
            
            // Act - Run multiple times
            var hashes = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                var result = command.ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None).Result;
                hashes.Add(result.Hash);
            }
            
            // Assert - All hashes should be identical
            var firstHash = hashes[0];
            foreach (var hash in hashes)
            {
                Assert.AreEqual(firstHash, hash, "Hash should be stable across multiple runs");
            }
        }
        
        [Test]
        public void GenreDetection_ShouldBeDeterministic()
        {
            // Arrange
            var command = new PlanGameSliceCommand();
            var brief = new DesignBrief(
                Description: "A game where you shoot enemies with guns",
                GenreHint: "FPS",
                TargetDurationMinutes: 5,
                DifficultyLevel: 3,
                Seed: 11111
            );
            
            // Act
            var result = command.ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None).Result;
            
            // Assert
            Assert.AreEqual("FPS", result.Genre, "Genre should be detected as FPS");
        }
        
        [Test]
        public void DifficultyProgression_ShouldBeDeterministic()
        {
            // Arrange
            var command = new PlanGameSliceCommand();
            var brief = new DesignBrief(
                Description: "A challenging platformer",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 4,
                Seed: 22222
            );
            
            // Act
            var result1 = command.ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None).Result;
            var result2 = command.ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None).Result;
            
            // Assert - Difficulty progression should be identical
            Assert.AreEqual(result1.DifficultyProgression.Count, result2.DifficultyProgression.Count, 
                "Difficulty progression count should be identical");
            
            for (int i = 0; i < result1.DifficultyProgression.Count; i++)
            {
                var beat1 = result1.DifficultyProgression[i];
                var beat2 = result2.DifficultyProgression[i];
                
                Assert.AreEqual(beat1.TimeOffsetSeconds, beat2.TimeOffsetSeconds, 
                    $"Time offset should be identical for beat {i}");
                Assert.AreEqual(beat1.DifficultyLevel, beat2.DifficultyLevel, 
                    $"Difficulty level should be identical for beat {i}");
            }
        }
    }
}
