using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Models.Iteration;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Feature.AI.Services;
using Xunit;

namespace Nexo.Feature.AI.Tests;

/// <summary>
/// Tests for the AI-powered iteration code generator
/// </summary>
public class IterationCodeGeneratorTests
{
    private readonly IServiceProvider _serviceProvider;
    
    public IterationCodeGeneratorTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddIterationStrategies();
        services.AddTransient<IModelOrchestrator, MockModelOrchestrator>();
        _serviceProvider = services.BuildServiceProvider();
    }
    
    [Fact]
    public async Task GenerateOptimalIterationCodeAsync_ShouldReturnValidCode()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<IIterationCodeGenerator>();
        var request = new IterationCodeRequest
        {
            Context = new IterationContext
            {
                DataSize = 1000,
                EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
            },
            CodeGeneration = new CodeGenerationContext
            {
                PlatformTarget = PlatformTarget.CSharp,
                CollectionName = "items",
                IterationBodyTemplate = "ProcessItem({item});"
            },
            UseAIEnhancement = false
        };
        
        // Act
        var code = await generator.GenerateOptimalIterationCodeAsync(request);
        
        // Assert
        Assert.NotNull(code);
        Assert.NotEmpty(code);
        Assert.Contains("items", code);
    }
    
    [Fact]
    public async Task GenerateOptimalIterationCodeAsync_WithAIEnhancement_ShouldReturnEnhancedCode()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<IIterationCodeGenerator>();
        var request = new IterationCodeRequest
        {
            Context = new IterationContext
            {
                DataSize = 1000,
                EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
            },
            CodeGeneration = new CodeGenerationContext
            {
                PlatformTarget = PlatformTarget.CSharp,
                CollectionName = "users",
                IterationBodyTemplate = "ValidateUser({item});"
            },
            UseAIEnhancement = true
        };
        
        // Act
        var code = await generator.GenerateOptimalIterationCodeAsync(request);
        
        // Assert
        Assert.NotNull(code);
        Assert.NotEmpty(code);
        Assert.Contains("users", code);
    }
    
    [Fact]
    public async Task GenerateMultiplePlatformIterationsAsync_ShouldReturnCodeForAllPlatforms()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<IIterationCodeGenerator>();
        var platforms = new[] { PlatformTarget.CSharp, PlatformTarget.JavaScript, PlatformTarget.Python, PlatformTarget.Swift };
        
        var request = new IterationCodeRequest
        {
            Context = new IterationContext
            {
                DataSize = 1000,
                EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
            },
            CodeGeneration = new CodeGenerationContext
            {
                CollectionName = "products",
                IterationBodyTemplate = "ProcessProduct({item});"
            },
            UseAIEnhancement = false,
            TargetPlatforms = platforms
        };
        
        // Act
        var codes = await generator.GenerateMultiplePlatformIterationsAsync(request);
        
        // Assert
        Assert.NotNull(codes);
        Assert.Equal(platforms.Length, codes.Count());
        
        foreach (var code in codes)
        {
            Assert.NotNull(code);
            Assert.NotEmpty(code);
            Assert.Contains("products", code);
        }
    }
    
    [Theory]
    [InlineData(PlatformTarget.CSharp)]
    [InlineData(PlatformTarget.JavaScript)]
    [InlineData(PlatformTarget.Python)]
    [InlineData(PlatformTarget.Swift)]
    [InlineData(PlatformTarget.Unity2022)]
    [InlineData(PlatformTarget.Unity2023)]
    public async Task GenerateOptimalIterationCodeAsync_ShouldWorkForAllPlatforms(PlatformTarget platform)
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<IIterationCodeGenerator>();
        var request = new IterationCodeRequest
        {
            Context = new IterationContext
            {
                DataSize = 1000,
                EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
            },
            CodeGeneration = new CodeGenerationContext
            {
                PlatformTarget = platform,
                CollectionName = "items",
                IterationBodyTemplate = "ProcessItem({item});"
            },
            UseAIEnhancement = false
        };
        
        // Act
        var code = await generator.GenerateOptimalIterationCodeAsync(request);
        
        // Assert
        Assert.NotNull(code);
        Assert.NotEmpty(code);
        Assert.Contains("items", code);
    }
    
    [Fact]
    public async Task GenerateOptimalIterationCodeAsync_ShouldHandleDifferentDataSizes()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<IIterationCodeGenerator>();
        var dataSizes = new[] { 100, 1000, 10000, 100000 };
        
        foreach (var dataSize in dataSizes)
        {
            var request = new IterationCodeRequest
            {
                Context = new IterationContext
                {
                    DataSize = dataSize,
                    EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
                },
                CodeGeneration = new CodeGenerationContext
                {
                    PlatformTarget = PlatformTarget.CSharp,
                    CollectionName = "data",
                    IterationBodyTemplate = "ProcessData({item});"
                },
                UseAIEnhancement = false
            };
            
            // Act
            var code = await generator.GenerateOptimalIterationCodeAsync(request);
            
            // Assert
            Assert.NotNull(code);
            Assert.NotEmpty(code);
            Assert.Contains("data", code);
        }
    }
    
    [Fact]
    public async Task GenerateOptimalIterationCodeAsync_ShouldHandleDifferentRequirements()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<IIterationCodeGenerator>();
        var requirements = new[]
        {
            new IterationRequirements { PrioritizeCpu = true },
            new IterationRequirements { PrioritizeMemory = true },
            new IterationRequirements { RequiresParallelization = true },
            new IterationRequirements { RequiresOrdering = false },
            new IterationRequirements { AllowSideEffects = false }
        };
        
        foreach (var requirement in requirements)
        {
            var request = new IterationCodeRequest
            {
                Context = new IterationContext
                {
                    DataSize = 1000,
                    Requirements = requirement,
                    EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
                },
                CodeGeneration = new CodeGenerationContext
                {
                    PlatformTarget = PlatformTarget.CSharp,
                    CollectionName = "items",
                    IterationBodyTemplate = "ProcessItem({item});"
                },
                UseAIEnhancement = false
            };
            
            // Act
            var code = await generator.GenerateOptimalIterationCodeAsync(request);
            
            // Assert
            Assert.NotNull(code);
            Assert.NotEmpty(code);
            Assert.Contains("items", code);
        }
    }
    
    [Fact]
    public async Task GenerateOptimalIterationCodeAsync_ShouldHandleComplexCodeGenerationContext()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<IIterationCodeGenerator>();
        var request = new IterationCodeRequest
        {
            Context = new IterationContext
            {
                DataSize = 1000,
                EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
            },
            CodeGeneration = new CodeGenerationContext
            {
                PlatformTarget = PlatformTarget.CSharp,
                CollectionName = "users",
                ItemName = "user",
                IterationBodyTemplate = "ValidateUser({item});",
                ActionTemplate = "u => ValidateUser(u)",
                PredicateTemplate = "u => u.IsActive",
                TransformTemplate = "u => u.Name",
                HasWhere = true,
                HasSelect = true,
                HasAsync = false
            },
            UseAIEnhancement = false
        };
        
        // Act
        var code = await generator.GenerateOptimalIterationCodeAsync(request);
        
        // Assert
        Assert.NotNull(code);
        Assert.NotEmpty(code);
        Assert.Contains("users", code);
    }
    
    [Fact]
    public async Task GenerateOptimalIterationCodeAsync_ShouldHandleErrorsGracefully()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<IIterationCodeGenerator>();
        var request = new IterationCodeRequest
        {
            Context = new IterationContext
            {
                DataSize = -1, // Invalid data size
                EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
            },
            CodeGeneration = new CodeGenerationContext
            {
                PlatformTarget = PlatformTarget.CSharp,
                CollectionName = "",
                IterationBodyTemplate = ""
            },
            UseAIEnhancement = false
        };
        
        // Act
        var code = await generator.GenerateOptimalIterationCodeAsync(request);
        
        // Assert
        Assert.NotNull(code);
        Assert.NotEmpty(code);
        // Should return fallback code even with invalid input
    }
}

/// <summary>
/// Mock model orchestrator for testing
/// </summary>
public class MockModelOrchestrator : IModelOrchestrator
{
    public Task<ModelResponse> ProcessAsync(string prompt)
    {
        // Mock response that enhances the base code
        var enhancedCode = $"""
        // Enhanced code with error handling
        try {{
            {prompt}
        }}
        catch (Exception ex) {{
            Console.WriteLine($"Error: {{ex.Message}}");
        }}
        """;
        
        return Task.FromResult(new ModelResponse
        {
            Response = enhancedCode,
            Success = true
        });
    }
}
