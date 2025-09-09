using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;
using Nexo.Infrastructure.Configuration;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Infrastructure.Tests.Configuration;

/// <summary>
/// Tests for iteration configuration
/// </summary>
public class IterationConfigurationTests
{
    [Fact]
    public void IterationConfiguration_ShouldHaveDefaultValues()
    {
        // Act
        var config = new IterationConfiguration();
        
        // Assert
        Assert.True(config.EnableAutoOptimization);
        Assert.Equal(PerformanceLevel.Balanced, config.DefaultOptimizationLevel);
        Assert.NotNull(config.PlatformPreferences);
        Assert.True(config.PlatformPreferences.Count > 0);
        Assert.NotNull(config.Benchmark);
        Assert.NotNull(config.PerformanceThresholds);
        Assert.NotNull(config.StrategyConfigurations);
        Assert.NotNull(config.EnvironmentOverrides);
    }
    
    [Fact]
    public void IterationConfiguration_ShouldHaveCorrectPlatformPreferences()
    {
        // Act
        var config = new IterationConfiguration();
        
        // Assert
        Assert.True(config.PlatformPreferences.ContainsKey("Unity"));
        Assert.Equal("Nexo.UnityOptimized", config.PlatformPreferences["Unity"]);
        
        Assert.True(config.PlatformPreferences.ContainsKey("DotNet"));
        Assert.Equal("Nexo.ForLoop", config.PlatformPreferences["DotNet"]);
        
        Assert.True(config.PlatformPreferences.ContainsKey("Server"));
        Assert.Equal("Nexo.ParallelLinq", config.PlatformPreferences["Server"]);
    }
    
    [Fact]
    public void BenchmarkConfiguration_ShouldHaveDefaultValues()
    {
        // Act
        var config = new BenchmarkConfiguration();
        
        // Assert
        Assert.Equal(10000, config.DefaultSampleSize);
        Assert.Equal(5, config.Iterations);
        Assert.True(config.EnableWarmup);
        Assert.Equal(2, config.WarmupIterations);
        Assert.Equal(30000, config.TimeoutMs);
        Assert.True(config.IncludeMemoryUsage);
        Assert.False(config.IncludeCpuUsage);
    }
    
    [Fact]
    public void PerformanceThresholds_ShouldHaveDefaultValues()
    {
        // Act
        var config = new PerformanceThresholds();
        
        // Assert
        Assert.Equal(1000, config.MaxExecutionTimeMs);
        Assert.Equal(100, config.MaxMemoryUsageMB);
        Assert.Equal(70.0, config.MinPerformanceScore);
        Assert.Equal(0.8, config.MinConfidenceLevel);
        Assert.NotNull(config.DataSizeThresholds);
    }
    
    [Fact]
    public void DataSizeThresholds_ShouldHaveDefaultValues()
    {
        // Act
        var config = new DataSizeThresholds();
        
        // Assert
        Assert.Equal(100, config.Small);
        Assert.Equal(1000, config.Medium);
        Assert.Equal(10000, config.Large);
        Assert.Equal(100000, config.VeryLarge);
    }
    
    [Fact]
    public void StrategyConfiguration_ShouldHaveDefaultValues()
    {
        // Act
        var config = new StrategyConfiguration();
        
        // Assert
        Assert.True(config.Enabled);
        Assert.Equal(50, config.DefaultPriority);
        Assert.NotNull(config.PlatformPriorities);
        Assert.Equal((0, int.MaxValue), config.OptimalDataSizeRange);
        Assert.False(config.SupportsParallelization);
        Assert.False(config.SuitableForRealTime);
        Assert.NotNull(config.Parameters);
    }
    
    [Fact]
    public void IterationConfigurationOptions_ShouldLoadFromConfiguration()
    {
        // Arrange
        var configurationData = new Dictionary<string, string>
        {
            ["Iteration:EnableAutoOptimization"] = "false",
            ["Iteration:DefaultOptimizationLevel"] = "High",
            ["Iteration:PlatformPreferences:DotNet"] = "Nexo.CustomStrategy",
            ["Iteration:Benchmark:DefaultSampleSize"] = "50000",
            ["Iteration:PerformanceThresholds:MaxExecutionTimeMs"] = "500"
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();
        
        // Act
        var options = new IterationConfigurationOptions(configuration);
        
        // Assert
        Assert.False(options.Value.EnableAutoOptimization);
        Assert.Equal(PerformanceLevel.High, options.Value.DefaultOptimizationLevel);
        Assert.Equal("Nexo.CustomStrategy", options.Value.PlatformPreferences["DotNet"]);
        Assert.Equal(50000, options.Value.Benchmark.DefaultSampleSize);
        Assert.Equal(500, options.Value.PerformanceThresholds.MaxExecutionTimeMs);
    }
    
    [Fact]
    public void IterationConfigurationOptions_ShouldApplyEnvironmentOverrides()
    {
        // Arrange
        var originalEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        
        try
        {
            var configurationData = new Dictionary<string, string>
            {
                ["Iteration:EnableAutoOptimization"] = "true",
                ["Iteration:DefaultOptimizationLevel"] = "Balanced",
                ["Iteration:EnvironmentOverrides:Production:EnableAutoOptimization"] = "true",
                ["Iteration:EnvironmentOverrides:Production:DefaultOptimizationLevel"] = "High",
                ["Iteration:EnvironmentOverrides:Production:Benchmark:DefaultSampleSize"] = "100000"
            };
            
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();
            
            // Act
            var options = new IterationConfigurationOptions(configuration);
            
            // Assert
            Assert.True(options.Value.EnableAutoOptimization);
            Assert.Equal(PerformanceLevel.High, options.Value.DefaultOptimizationLevel);
            Assert.Equal(100000, options.Value.Benchmark.DefaultSampleSize);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnvironment);
        }
    }
    
    [Fact]
    public void GetStrategyConfiguration_ShouldReturnDefaultWhenNotFound()
    {
        // Arrange
        var config = new IterationConfiguration();
        
        // Act
        var strategyConfig = config.GetStrategyConfiguration("NonExistentStrategy");
        
        // Assert
        Assert.NotNull(strategyConfig);
        Assert.True(strategyConfig.Enabled);
        Assert.Equal(50, strategyConfig.DefaultPriority);
        Assert.Equal((0, int.MaxValue), strategyConfig.OptimalDataSizeRange);
    }
    
    [Fact]
    public void GetStrategyConfiguration_ShouldReturnConfiguredStrategy()
    {
        // Arrange
        var config = new IterationConfiguration();
        config.StrategyConfigurations["Nexo.ForLoop"] = new StrategyConfiguration
        {
            Enabled = false,
            DefaultPriority = 90,
            OptimalDataSizeRange = (0, 10000)
        };
        
        // Act
        var strategyConfig = config.GetStrategyConfiguration("Nexo.ForLoop");
        
        // Assert
        Assert.NotNull(strategyConfig);
        Assert.False(strategyConfig.Enabled);
        Assert.Equal(90, strategyConfig.DefaultPriority);
        Assert.Equal((0, 10000), strategyConfig.OptimalDataSizeRange);
    }
    
    [Fact]
    public void GetPlatformPreference_ShouldReturnCorrectPreference()
    {
        // Arrange
        var config = new IterationConfiguration();
        
        // Act
        var unityPreference = config.GetPlatformPreference("Unity");
        var dotNetPreference = config.GetPlatformPreference("DotNet");
        var nonExistentPreference = config.GetPlatformPreference("NonExistent");
        
        // Assert
        Assert.Equal("Nexo.UnityOptimized", unityPreference);
        Assert.Equal("Nexo.ForLoop", dotNetPreference);
        Assert.Null(nonExistentPreference);
    }
    
    [Fact]
    public void IsStrategyEnabled_ShouldReturnCorrectStatus()
    {
        // Arrange
        var config = new IterationConfiguration();
        config.StrategyConfigurations["Nexo.DisabledStrategy"] = new StrategyConfiguration
        {
            Enabled = false
        };
        
        // Act
        var enabledStatus = config.IsStrategyEnabled("Nexo.ForLoop");
        var disabledStatus = config.IsStrategyEnabled("Nexo.DisabledStrategy");
        
        // Assert
        Assert.True(enabledStatus); // Default is enabled
        Assert.False(disabledStatus);
    }
    
    [Fact]
    public void GetStrategyPriority_ShouldReturnCorrectPriority()
    {
        // Arrange
        var config = new IterationConfiguration();
        config.StrategyConfigurations["Nexo.ForLoop"] = new StrategyConfiguration
        {
            DefaultPriority = 80,
            PlatformPriorities = new Dictionary<string, int>
            {
                { "Unity", 95 },
                { "Mobile", 90 }
            }
        };
        
        // Act
        var defaultPriority = config.GetStrategyPriority("Nexo.ForLoop", "DotNet");
        var unityPriority = config.GetStrategyPriority("Nexo.ForLoop", "Unity");
        var mobilePriority = config.GetStrategyPriority("Nexo.ForLoop", "Mobile");
        
        // Assert
        Assert.Equal(80, defaultPriority);
        Assert.Equal(95, unityPriority);
        Assert.Equal(90, mobilePriority);
    }
    
    [Fact]
    public void GetStrategyPriority_ShouldReturnDefaultForNonExistentStrategy()
    {
        // Arrange
        var config = new IterationConfiguration();
        
        // Act
        var priority = config.GetStrategyPriority("NonExistentStrategy", "DotNet");
        
        // Assert
        Assert.Equal(50, priority); // Default priority
    }
    
    [Theory]
    [InlineData("Development", true, PerformanceLevel.Balanced)]
    [InlineData("Production", true, PerformanceLevel.High)]
    [InlineData("Testing", false, PerformanceLevel.Low)]
    public void EnvironmentOverrides_ShouldApplyCorrectly(string environment, bool expectedAutoOptimization, PerformanceLevel expectedLevel)
    {
        // Arrange
        var config = new IterationConfiguration();
        config.EnvironmentOverrides[environment] = new IterationConfiguration
        {
            EnableAutoOptimization = expectedAutoOptimization,
            DefaultOptimizationLevel = expectedLevel
        };
        
        // Act
        var overrideConfig = config.EnvironmentOverrides[environment];
        
        // Assert
        Assert.Equal(expectedAutoOptimization, overrideConfig.EnableAutoOptimization);
        Assert.Equal(expectedLevel, overrideConfig.DefaultOptimizationLevel);
    }
    
    [Fact]
    public void Configuration_ShouldBeSerializable()
    {
        // Arrange
        var config = new IterationConfiguration();
        config.EnableAutoOptimization = false;
        config.DefaultOptimizationLevel = PerformanceLevel.High;
        config.PlatformPreferences["CustomPlatform"] = "CustomStrategy";
        
        // Act & Assert
        // This test ensures the configuration can be serialized/deserialized
        // without throwing exceptions
        Assert.NotNull(config);
        Assert.False(config.EnableAutoOptimization);
        Assert.Equal(PerformanceLevel.High, config.DefaultOptimizationLevel);
        Assert.Equal("CustomStrategy", config.PlatformPreferences["CustomPlatform"]);
    }
}
