using System;
using System.Collections.Generic;
using FluentAssertions;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Xunit;

namespace Nexo.Core.Domain.Tests.Entities.AI
{
    /// <summary>
    /// Tests for consolidated AI entities following hexagonal architecture
    /// </summary>
    public class ConsolidatedAIEntitiesTests
    {
        [Fact]
        public void AIEngineInfo_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var engineInfo = new AIEngineInfo
            {
                Id = "test-engine",
                Name = "Test AI Engine",
                EngineType = AIEngineType.Llama,
                Version = "1.0.0",
                IsAvailable = true,
                IsInitialized = true
            };

            // Assert
            engineInfo.Id.Should().Be("test-engine");
            engineInfo.Name.Should().Be("Test AI Engine");
            engineInfo.EngineType.Should().Be(AIEngineType.Llama);
            engineInfo.Version.Should().Be("1.0.0");
            engineInfo.IsAvailable.Should().BeTrue();
            engineInfo.IsInitialized.Should().BeTrue();
            engineInfo.Capabilities.Should().NotBeNull();
            engineInfo.SupportedLanguages.Should().NotBeNull();
            engineInfo.Requirements.Should().NotBeNull();
            engineInfo.Performance.Should().NotBeNull();
            engineInfo.Environment.Should().NotBeNull();
        }

        [Fact]
        public void AIEngineInfo_ShouldInheritFromBaseEntity()
        {
            // Arrange
            var engineInfoType = typeof(AIEngineInfo);

            // Assert
            engineInfoType.BaseType.Should().Be(typeof(BaseEntity));
        }

        [Fact]
        public void AIEngineInfo_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var engineInfo = new AIEngineInfo();
            var testId = "test-engine-id";
            var testName = "Test AI Engine";
            var testEngineType = AIEngineType.OpenAI;
            var testVersion = "2.0.0";
            var testIsAvailable = true;
            var testIsInitialized = true;
            var testCapabilities = new Dictionary<string, object> { ["key"] = "value" };
            var testSupportedLanguages = new List<string> { "C#", "Python" };
            var testRequirements = new AIRequirements();
            var testPerformance = new PerformanceEstimate();
            var testEnvironment = new EnvironmentProfile();

            // Act
            engineInfo.Id = testId;
            engineInfo.Name = testName;
            engineInfo.EngineType = testEngineType;
            engineInfo.Version = testVersion;
            engineInfo.IsAvailable = testIsAvailable;
            engineInfo.IsInitialized = testIsInitialized;
            engineInfo.Capabilities = testCapabilities;
            engineInfo.SupportedLanguages = testSupportedLanguages;
            engineInfo.Requirements = testRequirements;
            engineInfo.Performance = testPerformance;
            engineInfo.Environment = testEnvironment;

            // Assert
            engineInfo.Id.Should().Be(testId);
            engineInfo.Name.Should().Be(testName);
            engineInfo.EngineType.Should().Be(testEngineType);
            engineInfo.Version.Should().Be(testVersion);
            engineInfo.IsAvailable.Should().Be(testIsAvailable);
            engineInfo.IsInitialized.Should().Be(testIsInitialized);
            engineInfo.Capabilities.Should().Be(testCapabilities);
            engineInfo.SupportedLanguages.Should().Be(testSupportedLanguages);
            engineInfo.Requirements.Should().Be(testRequirements);
            engineInfo.Performance.Should().Be(testPerformance);
            engineInfo.Environment.Should().Be(testEnvironment);
        }

        [Fact]
        public void ModelInfo_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var modelInfo = new ModelInfo
            {
                Id = "test-model",
                Name = "Test Model",
                ModelId = "llama-2-7b",
                Version = "2.0.0",
                Size = 7000000000,
                Format = "GGML",
                Quantization = ModelQuantization.Q4_0,
                Status = ModelStatus.Available
            };

            // Assert
            modelInfo.Id.Should().Be("test-model");
            modelInfo.Name.Should().Be("Test Model");
            modelInfo.ModelId.Should().Be("llama-2-7b");
            modelInfo.Version.Should().Be("2.0.0");
            modelInfo.Size.Should().Be(7000000000);
            modelInfo.Format.Should().Be("GGML");
            modelInfo.Quantization.Should().Be(ModelQuantization.Q4_0);
            modelInfo.Status.Should().Be(ModelStatus.Available);
            modelInfo.Parameters.Should().NotBeNull();
            modelInfo.SupportedLanguages.Should().NotBeNull();
            modelInfo.Requirements.Should().NotBeNull();
        }

        [Fact]
        public void ModelInfo_ShouldInheritFromBaseEntity()
        {
            // Arrange
            var modelInfoType = typeof(ModelInfo);

            // Assert
            modelInfoType.BaseType.Should().Be(typeof(BaseEntity));
        }

        [Fact]
        public void ModelInfo_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var modelInfo = new ModelInfo();
            var testId = "test-model-id";
            var testName = "Test Model";
            var testModelId = "gpt-4";
            var testVersion = "3.0.0";
            var testSize = 10000000000L;
            var testFormat = "ONNX";
            var testQuantization = ModelQuantization.Q8_0;
            var testStatus = ModelStatus.Installed;
            var testLocalPath = "/models/gpt-4";
            var testDownloadUrl = "https://example.com/gpt-4";
            var testPlatform = "Linux";
            var testLastUpdated = DateTime.UtcNow.AddDays(-1);
            var testParameters = new Dictionary<string, object> { ["key"] = "value" };
            var testSupportedLanguages = new List<string> { "C#", "Python", "JavaScript" };
            var testRequirements = new AIRequirements();

            // Act
            modelInfo.Id = testId;
            modelInfo.Name = testName;
            modelInfo.ModelId = testModelId;
            modelInfo.Version = testVersion;
            modelInfo.Size = testSize;
            modelInfo.Format = testFormat;
            modelInfo.Quantization = testQuantization;
            modelInfo.Status = testStatus;
            modelInfo.LocalPath = testLocalPath;
            modelInfo.DownloadUrl = testDownloadUrl;
            modelInfo.Platform = testPlatform;
            modelInfo.LastUpdated = testLastUpdated;
            modelInfo.Parameters = testParameters;
            modelInfo.SupportedLanguages = testSupportedLanguages;
            modelInfo.Requirements = testRequirements;

            // Assert
            modelInfo.Id.Should().Be(testId);
            modelInfo.Name.Should().Be(testName);
            modelInfo.ModelId.Should().Be(testModelId);
            modelInfo.Version.Should().Be(testVersion);
            modelInfo.Size.Should().Be(testSize);
            modelInfo.Format.Should().Be(testFormat);
            modelInfo.Quantization.Should().Be(testQuantization);
            modelInfo.Status.Should().Be(testStatus);
            modelInfo.LocalPath.Should().Be(testLocalPath);
            modelInfo.DownloadUrl.Should().Be(testDownloadUrl);
            modelInfo.Platform.Should().Be(testPlatform);
            modelInfo.LastUpdated.Should().Be(testLastUpdated);
            modelInfo.Parameters.Should().Be(testParameters);
            modelInfo.SupportedLanguages.Should().Be(testSupportedLanguages);
            modelInfo.Requirements.Should().Be(testRequirements);
        }

        [Fact]
        public void AIRequirements_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var requirements = new AIRequirements
            {
                Id = "test-requirements",
                Name = "Test Requirements",
                Language = "C#",
                Framework = ".NET",
                Platform = "Local",
                SafetyLevel = SafetyLevel.Standard
            };

            // Assert
            requirements.Id.Should().Be("test-requirements");
            requirements.Name.Should().Be("Test Requirements");
            requirements.Language.Should().Be("C#");
            requirements.Framework.Should().Be(".NET");
            requirements.Platform.Should().Be("Local");
            requirements.SafetyLevel.Should().Be(SafetyLevel.Standard);
            requirements.Constraints.Should().NotBeNull();
            requirements.Parameters.Should().NotBeNull();
            requirements.Performance.Should().NotBeNull();
            requirements.Environment.Should().NotBeNull();
        }

        [Fact]
        public void AIRequirements_ShouldInheritFromBaseEntity()
        {
            // Arrange
            var requirementsType = typeof(AIRequirements);

            // Assert
            requirementsType.BaseType.Should().Be(typeof(BaseEntity));
        }

        [Fact]
        public void AIRequirements_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var requirements = new AIRequirements();
            var testId = "test-requirements-id";
            var testName = "Test Requirements";
            var testLanguage = "Python";
            var testFramework = "Django";
            var testPlatform = "Cloud";
            var testSafetyLevel = SafetyLevel.High;
            var testConstraints = new List<string> { "No external dependencies", "Must be async" };
            var testParameters = new Dictionary<string, object> { ["key"] = "value" };
            var testPerformance = new PerformanceEstimate();
            var testEnvironment = new EnvironmentProfile();

            // Act
            requirements.Id = testId;
            requirements.Name = testName;
            requirements.Language = testLanguage;
            requirements.Framework = testFramework;
            requirements.Platform = testPlatform;
            requirements.SafetyLevel = testSafetyLevel;
            requirements.Constraints = testConstraints;
            requirements.Parameters = testParameters;
            requirements.Performance = testPerformance;
            requirements.Environment = testEnvironment;

            // Assert
            requirements.Id.Should().Be(testId);
            requirements.Name.Should().Be(testName);
            requirements.Language.Should().Be(testLanguage);
            requirements.Framework.Should().Be(testFramework);
            requirements.Platform.Should().Be(testPlatform);
            requirements.SafetyLevel.Should().Be(testSafetyLevel);
            requirements.Constraints.Should().Be(testConstraints);
            requirements.Parameters.Should().Be(testParameters);
            requirements.Performance.Should().Be(testPerformance);
            requirements.Environment.Should().Be(testEnvironment);
        }

        [Fact]
        public void PerformanceEstimate_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var performance = new PerformanceEstimate
            {
                Id = "test-performance",
                Name = "Test Performance",
                EstimatedTime = TimeSpan.FromSeconds(5),
                CpuUtilization = 0.8,
                MemoryUsage = 1024,
                Confidence = 0.9
            };

            // Assert
            performance.Id.Should().Be("test-performance");
            performance.Name.Should().Be("Test Performance");
            performance.EstimatedTime.Should().Be(TimeSpan.FromSeconds(5));
            performance.CpuUtilization.Should().Be(0.8);
            performance.MemoryUsage.Should().Be(1024);
            performance.Confidence.Should().Be(0.9);
            performance.Metrics.Should().NotBeNull();
            performance.Environment.Should().NotBeNull();
        }

        [Fact]
        public void PerformanceEstimate_ShouldInheritFromBaseEntity()
        {
            // Arrange
            var performanceType = typeof(PerformanceEstimate);

            // Assert
            performanceType.BaseType.Should().Be(typeof(BaseEntity));
        }

        [Fact]
        public void PerformanceEstimate_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var performance = new PerformanceEstimate();
            var testId = "test-performance-id";
            var testName = "Test Performance";
            var testEstimatedTime = TimeSpan.FromMinutes(2);
            var testCpuUtilization = 0.9;
            var testMemoryUsage = 2048;
            var testConfidence = 0.95;
            var testMetrics = new Dictionary<string, object> { ["key"] = "value" };
            var testEnvironment = new EnvironmentProfile();

            // Act
            performance.Id = testId;
            performance.Name = testName;
            performance.EstimatedTime = testEstimatedTime;
            performance.CpuUtilization = testCpuUtilization;
            performance.MemoryUsage = testMemoryUsage;
            performance.Confidence = testConfidence;
            performance.Metrics = testMetrics;
            performance.Environment = testEnvironment;

            // Assert
            performance.Id.Should().Be(testId);
            performance.Name.Should().Be(testName);
            performance.EstimatedTime.Should().Be(testEstimatedTime);
            performance.CpuUtilization.Should().Be(testCpuUtilization);
            performance.MemoryUsage.Should().Be(testMemoryUsage);
            performance.Confidence.Should().Be(testConfidence);
            performance.Metrics.Should().Be(testMetrics);
            performance.Environment.Should().Be(testEnvironment);
        }

        [Fact]
        public void EnvironmentProfile_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var environment = new EnvironmentProfile
            {
                Id = "test-environment",
                Name = "Test Environment",
                CurrentPlatform = "Windows",
                AvailableMemory = 8192,
                PlatformType = PlatformType.Windows
            };

            // Assert
            environment.Id.Should().Be("test-environment");
            environment.Name.Should().Be("Test Environment");
            environment.CurrentPlatform.Should().Be("Windows");
            environment.AvailableMemory.Should().Be(8192);
            environment.PlatformType.Should().Be(PlatformType.Windows);
            environment.Resources.Should().NotBeNull();
            environment.OSVersion.Should().NotBeNull();
        }

        [Fact]
        public void EnvironmentProfile_ShouldInheritFromBaseEntity()
        {
            // Arrange
            var environmentType = typeof(EnvironmentProfile);

            // Assert
            environmentType.BaseType.Should().Be(typeof(BaseEntity));
        }

        [Fact]
        public void EnvironmentProfile_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var environment = new EnvironmentProfile();
            var testId = "test-environment-id";
            var testName = "Test Environment";
            var testCurrentPlatform = "Linux";
            var testAvailableMemory = 16384L;
            var testPlatformType = PlatformType.Linux;
            var testResources = new Dictionary<string, object> { ["key"] = "value" };
            var testOSVersion = new OSVersion();

            // Act
            environment.Id = testId;
            environment.Name = testName;
            environment.CurrentPlatform = testCurrentPlatform;
            environment.AvailableMemory = testAvailableMemory;
            environment.PlatformType = testPlatformType;
            environment.Resources = testResources;
            environment.OSVersion = testOSVersion;

            // Assert
            environment.Id.Should().Be(testId);
            environment.Name.Should().Be(testName);
            environment.CurrentPlatform.Should().Be(testCurrentPlatform);
            environment.AvailableMemory.Should().Be(testAvailableMemory);
            environment.PlatformType.Should().Be(testPlatformType);
            environment.Resources.Should().Be(testResources);
            environment.OSVersion.Should().Be(testOSVersion);
        }

        [Fact]
        public void OSVersion_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var osVersion = new OSVersion
            {
                Name = "Windows",
                Version = "10",
                BuildNumber = "19045",
                ServicePack = "None",
                Is64Bit = true
            };

            // Assert
            osVersion.Name.Should().Be("Windows");
            osVersion.Version.Should().Be("10");
            osVersion.BuildNumber.Should().Be("19045");
            osVersion.ServicePack.Should().Be("None");
            osVersion.Is64Bit.Should().BeTrue();
            osVersion.Metadata.Should().NotBeNull();
        }

        [Fact]
        public void OSVersion_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var osVersion = new OSVersion();
            var testName = "Ubuntu";
            var testVersion = "22.04";
            var testBuildNumber = "22.04.1";
            var testServicePack = "LTS";
            var testIs64Bit = true;
            var testMetadata = new Dictionary<string, object> { ["key"] = "value" };

            // Act
            osVersion.Name = testName;
            osVersion.Version = testVersion;
            osVersion.BuildNumber = testBuildNumber;
            osVersion.ServicePack = testServicePack;
            osVersion.Is64Bit = testIs64Bit;
            osVersion.Metadata = testMetadata;

            // Assert
            osVersion.Name.Should().Be(testName);
            osVersion.Version.Should().Be(testVersion);
            osVersion.BuildNumber.Should().Be(testBuildNumber);
            osVersion.ServicePack.Should().Be(testServicePack);
            osVersion.Is64Bit.Should().Be(testIs64Bit);
            osVersion.Metadata.Should().Be(testMetadata);
        }

        [Fact]
        public void PlatformType_ShouldHaveCorrectValues()
        {
            // Assert
            Enum.GetValues<PlatformType>().Should().Contain(PlatformType.Windows);
            Enum.GetValues<PlatformType>().Should().Contain(PlatformType.Linux);
            Enum.GetValues<PlatformType>().Should().Contain(PlatformType.macOS);
            Enum.GetValues<PlatformType>().Should().Contain(PlatformType.Docker);
            Enum.GetValues<PlatformType>().Should().Contain(PlatformType.Cloud);
            Enum.GetValues<PlatformType>().Should().Contain(PlatformType.Unknown);
        }

        [Fact]
        public void PlatformType_ToString_ShouldReturnCorrectString()
        {
            // Assert
            PlatformType.Windows.ToString().Should().Be("Windows");
            PlatformType.Linux.ToString().Should().Be("Linux");
            PlatformType.macOS.ToString().Should().Be("macOS");
            PlatformType.Docker.ToString().Should().Be("Docker");
            PlatformType.Cloud.ToString().Should().Be("Cloud");
            PlatformType.Unknown.ToString().Should().Be("Unknown");
        }
    }
}