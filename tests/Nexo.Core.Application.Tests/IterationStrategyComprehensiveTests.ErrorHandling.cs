using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Application.Services.Iteration.Strategies;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Xunit;

namespace Nexo.Core.Application.Tests;

/// <summary>
/// Error handling test cases for IterationStrategyComprehensiveTests.
/// </summary>
public partial class IterationStrategyComprehensiveTests
{
    [Fact]
    public void IterationStrategySelector_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var selector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => selector.SelectStrategy<int>(null));
    }
    
    [Fact]
    public void IterationStrategySelector_WithNullStrategy_ShouldThrowArgumentNullException()
    {
        // Arrange
        var selector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => selector.RegisterStrategy(null));
    }
    
    [Fact]
    public void IterationStrategySelector_WithNullEnvironmentProfile_ShouldThrowArgumentNullException()
    {
        // Arrange
        var selector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => selector.SetEnvironmentProfile(null));
    }
    
    [Fact]
    public void AllStrategies_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategies = new IIterationStrategy<int>[]
        {
            new ForLoopStrategy<int>(),
            new ForeachStrategy<int>(),
            new LinqStrategy<int>(),
            new ParallelLinqStrategy<int>(),
            new UnityOptimizedStrategy<int>(),
            new WasmOptimizedStrategy<int>()
        };
        
        foreach (var strategy in strategies)
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => strategy.Execute(null, x => { }));
            Assert.Throws<ArgumentNullException>(() => strategy.Execute(null, x => x));
            Assert.Throws<ArgumentNullException>(() => strategy.ExecuteWhere(null, x => true, x => x));
        }
    }
    
    [Fact]
    public void AllStrategies_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategies = new IIterationStrategy<int>[]
        {
            new ForLoopStrategy<int>(),
            new ForeachStrategy<int>(),
            new UnityOptimizedStrategy<int>()
        };
        
        var data = new[] { 1, 2, 3 };
        
        foreach (var strategy in strategies)
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => strategy.Execute(data, null));
        }
    }
    
    [Fact]
    public void AllStrategies_WithNullTransform_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategies = new IIterationStrategy<int>[]
        {
            new LinqStrategy<int>(),
            new ParallelLinqStrategy<int>(),
            new WasmOptimizedStrategy<int>()
        };
        
        var data = new[] { 1, 2, 3 };
        
        foreach (var strategy in strategies)
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => strategy.Execute(data, null));
        }
    }
    
    [Fact]
    public void AllStrategies_WithNullPredicate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategies = new IIterationStrategy<int>[]
        {
            new ForLoopStrategy<int>(),
            new ForeachStrategy<int>(),
            new LinqStrategy<int>(),
            new ParallelLinqStrategy<int>(),
            new UnityOptimizedStrategy<int>(),
            new WasmOptimizedStrategy<int>()
        };
        
        var data = new[] { 1, 2, 3 };
        
        foreach (var strategy in strategies)
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => strategy.ExecuteWhere(data, null, x => x));
        }
    }
    
    [Fact]
    public void AllStrategies_WithNullTransformInExecuteWhere_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategies = new IIterationStrategy<int>[]
        {
            new ForLoopStrategy<int>(),
            new ForeachStrategy<int>(),
            new LinqStrategy<int>(),
            new ParallelLinqStrategy<int>(),
            new UnityOptimizedStrategy<int>(),
            new WasmOptimizedStrategy<int>()
        };
        
        var data = new[] { 1, 2, 3 };
        
        foreach (var strategy in strategies)
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => strategy.ExecuteWhere(data, x => true, null));
        }
    }
    
    [Fact]
    public void CodeGeneration_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategies = new IIterationStrategy<object>[]
        {
            new ForLoopStrategy<object>(),
            new ForeachStrategy<object>(),
            new LinqStrategy<object>(),
            new ParallelLinqStrategy<object>(),
            new UnityOptimizedStrategy<object>(),
            new WasmOptimizedStrategy<object>()
        };
        
        foreach (var strategy in strategies)
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => strategy.GenerateCode(null));
        }
    }
    
    [Fact]
    public void CodeGeneration_WithNullPlatformTarget_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ForLoopStrategy<object>();
        var context = new CodeGenerationContext
        {
            PlatformTarget = null,
            CollectionName = "items",
            IterationBodyTemplate = "ProcessItem({item});"
        };
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.GenerateCode(context));
    }
    
    [Fact]
    public void CodeGeneration_WithNullCollectionName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ForLoopStrategy<object>();
        var context = new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.CSharp,
            CollectionName = null,
            IterationBodyTemplate = "ProcessItem({item});"
        };
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.GenerateCode(context));
    }
    
    [Fact]
    public void CodeGeneration_WithNullIterationBodyTemplate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ForLoopStrategy<object>();
        var context = new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.CSharp,
            CollectionName = "items",
            IterationBodyTemplate = null
        };
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.GenerateCode(context));
    }
    
    [Fact]
    public void CodeGeneration_WithNullItemName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ForeachStrategy<object>();
        var context = new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.CSharp,
            CollectionName = "items",
            IterationBodyTemplate = "ProcessItem({item});",
            ItemName = null
        };
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.GenerateCode(context));
    }
    
    [Fact]
    public void CodeGeneration_WithNullActionTemplate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new LinqStrategy<object>();
        var context = new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.CSharp,
            CollectionName = "items",
            ActionTemplate = null
        };
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.GenerateCode(context));
    }
    
    [Fact]
    public void IterationContext_WithNegativeDataSize_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new IterationContext
        {
            DataSize = -1
        });
    }
    
    [Fact]
    public void IterationRequirements_WithNegativeMaxDegreeOfParallelism_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new IterationRequirements
        {
            MaxDegreeOfParallelism = -1
        });
    }
    
    [Fact]
    public void IterationRequirements_WithNegativeTimeout_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new IterationRequirements
        {
            Timeout = TimeSpan.FromSeconds(-1)
        });
    }
    
    [Fact]
    public void RuntimeEnvironmentProfile_WithNegativeCpuCores_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RuntimeEnvironmentProfile
        {
            CpuCores = -1
        });
    }
    
    [Fact]
    public void RuntimeEnvironmentProfile_WithNegativeAvailableMemory_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RuntimeEnvironmentProfile
        {
            AvailableMemoryMB = -1
        });
    }
    
    [Fact]
    public void RuntimeEnvironmentProfile_WithNullFrameworkVersion_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RuntimeEnvironmentProfile
        {
            FrameworkVersion = null
        });
    }
    
    [Fact]
    public void RuntimeEnvironmentProfile_WithNullOptimizationLevel_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RuntimeEnvironmentProfile
        {
            OptimizationLevel = null
        });
    }
    
    [Fact]
    public void CodeGenerationContext_WithNullPlatformTarget_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CodeGenerationContext
        {
            PlatformTarget = null
        });
    }
    
    [Fact]
    public void CodeGenerationContext_WithNullCollectionName_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.CSharp,
            CollectionName = null
        });
    }
    
    [Fact]
    public void CodeGenerationContext_WithNullIterationBodyTemplate_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.CSharp,
            CollectionName = "items",
            IterationBodyTemplate = null
        });
    }
    
    [Fact]
    public void CodeGenerationContext_WithNullItemName_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.CSharp,
            CollectionName = "items",
            IterationBodyTemplate = "ProcessItem({item});",
            ItemName = null
        });
    }
    
    [Fact]
    public void CodeGenerationContext_WithNullActionTemplate_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.CSharp,
            CollectionName = "items",
            ActionTemplate = null
        });
    }
}