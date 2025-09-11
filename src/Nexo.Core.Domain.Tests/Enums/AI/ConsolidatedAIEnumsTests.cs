using System;
using FluentAssertions;
using Nexo.Core.Domain.Enums.AI;
using Xunit;

namespace Nexo.Core.Domain.Tests.Enums.AI
{
    /// <summary>
    /// Tests for consolidated AI enums following hexagonal architecture
    /// </summary>
    public class ConsolidatedAIEnumsTests
    {
        [Fact]
        public void AIEngineType_ShouldHaveCorrectValues()
        {
            // Assert
            Enum.GetValues<AIEngineType>().Should().Contain(AIEngineType.Llama);
            Enum.GetValues<AIEngineType>().Should().Contain(AIEngineType.OpenAI);
            Enum.GetValues<AIEngineType>().Should().Contain(AIEngineType.Anthropic);
            Enum.GetValues<AIEngineType>().Should().Contain(AIEngineType.Google);
            Enum.GetValues<AIEngineType>().Should().Contain(AIEngineType.Azure);
            Enum.GetValues<AIEngineType>().Should().Contain(AIEngineType.Custom);
        }

        [Fact]
        public void AIProviderType_ShouldHaveCorrectValues()
        {
            // Assert
            Enum.GetValues<AIProviderType>().Should().Contain(AIProviderType.Local);
            Enum.GetValues<AIProviderType>().Should().Contain(AIProviderType.Cloud);
            Enum.GetValues<AIProviderType>().Should().Contain(AIProviderType.Hybrid);
        }

        [Fact]
        public void AIOperationType_ShouldHaveCorrectValues()
        {
            // Assert
            Enum.GetValues<AIOperationType>().Should().Contain(AIOperationType.CodeGeneration);
            Enum.GetValues<AIOperationType>().Should().Contain(AIOperationType.CodeReview);
            Enum.GetValues<AIOperationType>().Should().Contain(AIOperationType.CodeOptimization);
            Enum.GetValues<AIOperationType>().Should().Contain(AIOperationType.Documentation);
            Enum.GetValues<AIOperationType>().Should().Contain(AIOperationType.Testing);
            Enum.GetValues<AIOperationType>().Should().Contain(AIOperationType.Analysis);
        }

        [Fact]
        public void AIOperationStatus_ShouldHaveCorrectValues()
        {
            // Assert
            Enum.GetValues<AIOperationStatus>().Should().Contain(AIOperationStatus.Pending);
            Enum.GetValues<AIOperationStatus>().Should().Contain(AIOperationStatus.InProgress);
            Enum.GetValues<AIOperationStatus>().Should().Contain(AIOperationStatus.Completed);
            Enum.GetValues<AIOperationStatus>().Should().Contain(AIOperationStatus.Failed);
            Enum.GetValues<AIOperationStatus>().Should().Contain(AIOperationStatus.Cancelled);
            Enum.GetValues<AIOperationStatus>().Should().Contain(AIOperationStatus.Error);
        }

        [Fact]
        public void AIConfidenceLevel_ShouldHaveCorrectValues()
        {
            // Assert
            Enum.GetValues<AIConfidenceLevel>().Should().Contain(AIConfidenceLevel.Low);
            Enum.GetValues<AIConfidenceLevel>().Should().Contain(AIConfidenceLevel.Medium);
            Enum.GetValues<AIConfidenceLevel>().Should().Contain(AIConfidenceLevel.High);
        }

        [Fact]
        public void ModelStatus_ShouldHaveCorrectValues()
        {
            // Assert
            Enum.GetValues<ModelStatus>().Should().Contain(ModelStatus.Available);
            Enum.GetValues<ModelStatus>().Should().Contain(ModelStatus.Downloading);
            Enum.GetValues<ModelStatus>().Should().Contain(ModelStatus.Downloaded);
            Enum.GetValues<ModelStatus>().Should().Contain(ModelStatus.Installing);
            Enum.GetValues<ModelStatus>().Should().Contain(ModelStatus.Installed);
            Enum.GetValues<ModelStatus>().Should().Contain(ModelStatus.Error);
            Enum.GetValues<ModelStatus>().Should().Contain(ModelStatus.Unavailable);
        }

        [Fact]
        public void ModelQuantization_ShouldHaveCorrectValues()
        {
            // Assert
            Enum.GetValues<ModelQuantization>().Should().Contain(ModelQuantization.None);
            Enum.GetValues<ModelQuantization>().Should().Contain(ModelQuantization.Q4_0);
            Enum.GetValues<ModelQuantization>().Should().Contain(ModelQuantization.Q4_1);
            Enum.GetValues<ModelQuantization>().Should().Contain(ModelQuantization.Q5_0);
            Enum.GetValues<ModelQuantization>().Should().Contain(ModelQuantization.Q5_1);
            Enum.GetValues<ModelQuantization>().Should().Contain(ModelQuantization.Q8_0);
            Enum.GetValues<ModelQuantization>().Should().Contain(ModelQuantization.Q8_1);
        }

        [Fact]
        public void CodeIssueType_ShouldHaveCorrectValues()
        {
            // Assert
            Enum.GetValues<CodeIssueType>().Should().Contain(CodeIssueType.Error);
            Enum.GetValues<CodeIssueType>().Should().Contain(CodeIssueType.Warning);
            Enum.GetValues<CodeIssueType>().Should().Contain(CodeIssueType.Info);
            Enum.GetValues<CodeIssueType>().Should().Contain(CodeIssueType.Suggestion);
        }

        [Fact]
        public void CommandPriority_ShouldHaveCorrectValues()
        {
            // Assert
            Enum.GetValues<CommandPriority>().Should().Contain(CommandPriority.Low);
            Enum.GetValues<CommandPriority>().Should().Contain(CommandPriority.Normal);
            Enum.GetValues<CommandPriority>().Should().Contain(CommandPriority.High);
            Enum.GetValues<CommandPriority>().Should().Contain(CommandPriority.Critical);
        }

        [Fact]
        public void OptimizationType_ShouldHaveCorrectValues()
        {
            // Assert
            Enum.GetValues<OptimizationType>().Should().Contain(OptimizationType.Performance);
            Enum.GetValues<OptimizationType>().Should().Contain(OptimizationType.Memory);
            Enum.GetValues<OptimizationType>().Should().Contain(OptimizationType.Readability);
            Enum.GetValues<OptimizationType>().Should().Contain(OptimizationType.Security);
            Enum.GetValues<OptimizationType>().Should().Contain(OptimizationType.Maintainability);
        }

        [Fact]
        public void TestType_ShouldHaveCorrectValues()
        {
            // Assert
            Enum.GetValues<TestType>().Should().Contain(TestType.Unit);
            Enum.GetValues<TestType>().Should().Contain(TestType.Integration);
            Enum.GetValues<TestType>().Should().Contain(TestType.System);
            Enum.GetValues<TestType>().Should().Contain(TestType.Performance);
            Enum.GetValues<TestType>().Should().Contain(TestType.Security);
        }

        [Fact]
        public void AIEngineType_ToString_ShouldReturnCorrectString()
        {
            // Assert
            AIEngineType.Llama.ToString().Should().Be("Llama");
            AIEngineType.OpenAI.ToString().Should().Be("OpenAI");
            AIEngineType.Anthropic.ToString().Should().Be("Anthropic");
            AIEngineType.Google.ToString().Should().Be("Google");
            AIEngineType.Azure.ToString().Should().Be("Azure");
            AIEngineType.Custom.ToString().Should().Be("Custom");
        }

        [Fact]
        public void AIProviderType_ToString_ShouldReturnCorrectString()
        {
            // Assert
            AIProviderType.Local.ToString().Should().Be("Local");
            AIProviderType.Cloud.ToString().Should().Be("Cloud");
            AIProviderType.Hybrid.ToString().Should().Be("Hybrid");
        }

        [Fact]
        public void AIOperationType_ToString_ShouldReturnCorrectString()
        {
            // Assert
            AIOperationType.CodeGeneration.ToString().Should().Be("CodeGeneration");
            AIOperationType.CodeReview.ToString().Should().Be("CodeReview");
            AIOperationType.CodeOptimization.ToString().Should().Be("CodeOptimization");
            AIOperationType.Documentation.ToString().Should().Be("Documentation");
            AIOperationType.Testing.ToString().Should().Be("Testing");
            AIOperationType.Analysis.ToString().Should().Be("Analysis");
        }

        [Fact]
        public void AIOperationStatus_ToString_ShouldReturnCorrectString()
        {
            // Assert
            AIOperationStatus.Pending.ToString().Should().Be("Pending");
            AIOperationStatus.InProgress.ToString().Should().Be("InProgress");
            AIOperationStatus.Completed.ToString().Should().Be("Completed");
            AIOperationStatus.Failed.ToString().Should().Be("Failed");
            AIOperationStatus.Cancelled.ToString().Should().Be("Cancelled");
            AIOperationStatus.Error.ToString().Should().Be("Error");
        }

        [Fact]
        public void AIConfidenceLevel_ToString_ShouldReturnCorrectString()
        {
            // Assert
            AIConfidenceLevel.Low.ToString().Should().Be("Low");
            AIConfidenceLevel.Medium.ToString().Should().Be("Medium");
            AIConfidenceLevel.High.ToString().Should().Be("High");
        }

        [Fact]
        public void ModelStatus_ToString_ShouldReturnCorrectString()
        {
            // Assert
            ModelStatus.Available.ToString().Should().Be("Available");
            ModelStatus.Downloading.ToString().Should().Be("Downloading");
            ModelStatus.Downloaded.ToString().Should().Be("Downloaded");
            ModelStatus.Installing.ToString().Should().Be("Installing");
            ModelStatus.Installed.ToString().Should().Be("Installed");
            ModelStatus.Error.ToString().Should().Be("Error");
            ModelStatus.Unavailable.ToString().Should().Be("Unavailable");
        }

        [Fact]
        public void ModelQuantization_ToString_ShouldReturnCorrectString()
        {
            // Assert
            ModelQuantization.None.ToString().Should().Be("None");
            ModelQuantization.Q4_0.ToString().Should().Be("Q4_0");
            ModelQuantization.Q4_1.ToString().Should().Be("Q4_1");
            ModelQuantization.Q5_0.ToString().Should().Be("Q5_0");
            ModelQuantization.Q5_1.ToString().Should().Be("Q5_1");
            ModelQuantization.Q8_0.ToString().Should().Be("Q8_0");
            ModelQuantization.Q8_1.ToString().Should().Be("Q8_1");
        }

        [Fact]
        public void CodeIssueType_ToString_ShouldReturnCorrectString()
        {
            // Assert
            CodeIssueType.Error.ToString().Should().Be("Error");
            CodeIssueType.Warning.ToString().Should().Be("Warning");
            CodeIssueType.Info.ToString().Should().Be("Info");
            CodeIssueType.Suggestion.ToString().Should().Be("Suggestion");
        }

        [Fact]
        public void CommandPriority_ToString_ShouldReturnCorrectString()
        {
            // Assert
            CommandPriority.Low.ToString().Should().Be("Low");
            CommandPriority.Normal.ToString().Should().Be("Normal");
            CommandPriority.High.ToString().Should().Be("High");
            CommandPriority.Critical.ToString().Should().Be("Critical");
        }

        [Fact]
        public void OptimizationType_ToString_ShouldReturnCorrectString()
        {
            // Assert
            OptimizationType.Performance.ToString().Should().Be("Performance");
            OptimizationType.Memory.ToString().Should().Be("Memory");
            OptimizationType.Readability.ToString().Should().Be("Readability");
            OptimizationType.Security.ToString().Should().Be("Security");
            OptimizationType.Maintainability.ToString().Should().Be("Maintainability");
        }

        [Fact]
        public void TestType_ToString_ShouldReturnCorrectString()
        {
            // Assert
            TestType.Unit.ToString().Should().Be("Unit");
            TestType.Integration.ToString().Should().Be("Integration");
            TestType.System.ToString().Should().Be("System");
            TestType.Performance.ToString().Should().Be("Performance");
            TestType.Security.ToString().Should().Be("Security");
        }

        [Fact]
        public void AIEngineType_ShouldHaveCorrectNumericValues()
        {
            // Assert
            ((int)AIEngineType.Llama).Should().Be(0);
            ((int)AIEngineType.OpenAI).Should().Be(1);
            ((int)AIEngineType.Anthropic).Should().Be(2);
            ((int)AIEngineType.Google).Should().Be(3);
            ((int)AIEngineType.Azure).Should().Be(4);
            ((int)AIEngineType.Custom).Should().Be(5);
        }

        [Fact]
        public void AIProviderType_ShouldHaveCorrectNumericValues()
        {
            // Assert
            ((int)AIProviderType.Local).Should().Be(0);
            ((int)AIProviderType.Cloud).Should().Be(1);
            ((int)AIProviderType.Hybrid).Should().Be(2);
        }

        [Fact]
        public void AIOperationType_ShouldHaveCorrectNumericValues()
        {
            // Assert
            ((int)AIOperationType.CodeGeneration).Should().Be(0);
            ((int)AIOperationType.CodeReview).Should().Be(1);
            ((int)AIOperationType.CodeOptimization).Should().Be(2);
            ((int)AIOperationType.Documentation).Should().Be(3);
            ((int)AIOperationType.Testing).Should().Be(4);
            ((int)AIOperationType.Analysis).Should().Be(5);
        }

        [Fact]
        public void AIOperationStatus_ShouldHaveCorrectNumericValues()
        {
            // Assert
            ((int)AIOperationStatus.Pending).Should().Be(0);
            ((int)AIOperationStatus.InProgress).Should().Be(1);
            ((int)AIOperationStatus.Completed).Should().Be(2);
            ((int)AIOperationStatus.Failed).Should().Be(3);
            ((int)AIOperationStatus.Cancelled).Should().Be(4);
            ((int)AIOperationStatus.Error).Should().Be(5);
        }

        [Fact]
        public void AIConfidenceLevel_ShouldHaveCorrectNumericValues()
        {
            // Assert
            ((int)AIConfidenceLevel.Low).Should().Be(0);
            ((int)AIConfidenceLevel.Medium).Should().Be(1);
            ((int)AIConfidenceLevel.High).Should().Be(2);
        }

        [Fact]
        public void ModelStatus_ShouldHaveCorrectNumericValues()
        {
            // Assert
            ((int)ModelStatus.Available).Should().Be(0);
            ((int)ModelStatus.Downloading).Should().Be(1);
            ((int)ModelStatus.Downloaded).Should().Be(2);
            ((int)ModelStatus.Installing).Should().Be(3);
            ((int)ModelStatus.Installed).Should().Be(4);
            ((int)ModelStatus.Error).Should().Be(5);
            ((int)ModelStatus.Unavailable).Should().Be(6);
        }

        [Fact]
        public void ModelQuantization_ShouldHaveCorrectNumericValues()
        {
            // Assert
            ((int)ModelQuantization.None).Should().Be(0);
            ((int)ModelQuantization.Q4_0).Should().Be(1);
            ((int)ModelQuantization.Q4_1).Should().Be(2);
            ((int)ModelQuantization.Q5_0).Should().Be(3);
            ((int)ModelQuantization.Q5_1).Should().Be(4);
            ((int)ModelQuantization.Q8_0).Should().Be(5);
            ((int)ModelQuantization.Q8_1).Should().Be(6);
        }

        [Fact]
        public void CodeIssueType_ShouldHaveCorrectNumericValues()
        {
            // Assert
            ((int)CodeIssueType.Error).Should().Be(0);
            ((int)CodeIssueType.Warning).Should().Be(1);
            ((int)CodeIssueType.Info).Should().Be(2);
            ((int)CodeIssueType.Suggestion).Should().Be(3);
        }

        [Fact]
        public void CommandPriority_ShouldHaveCorrectNumericValues()
        {
            // Assert
            ((int)CommandPriority.Low).Should().Be(0);
            ((int)CommandPriority.Normal).Should().Be(1);
            ((int)CommandPriority.High).Should().Be(2);
            ((int)CommandPriority.Critical).Should().Be(3);
        }

        [Fact]
        public void OptimizationType_ShouldHaveCorrectNumericValues()
        {
            // Assert
            ((int)OptimizationType.Performance).Should().Be(0);
            ((int)OptimizationType.Memory).Should().Be(1);
            ((int)OptimizationType.Readability).Should().Be(2);
            ((int)OptimizationType.Security).Should().Be(3);
            ((int)OptimizationType.Maintainability).Should().Be(4);
        }

        [Fact]
        public void TestType_ShouldHaveCorrectNumericValues()
        {
            // Assert
            ((int)TestType.Unit).Should().Be(0);
            ((int)TestType.Integration).Should().Be(1);
            ((int)TestType.System).Should().Be(2);
            ((int)TestType.Performance).Should().Be(3);
            ((int)TestType.Security).Should().Be(4);
        }
    }
}