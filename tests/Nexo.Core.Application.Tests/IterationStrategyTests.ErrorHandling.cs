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
/// Error handling test cases for IterationStrategyTests.
/// </summary>
public partial class IterationStrategyTests
{
    [Fact]
    public void ForLoopStrategy_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ForLoopStrategy<int>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.Execute(null, x => { }));
    }
    
    [Fact]
    public void ForLoopStrategy_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ForLoopStrategy<int>();
        var data = new[] { 1, 2, 3 };
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.Execute(data, null));
    }
    
    [Fact]
    public void ForeachStrategy_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ForeachStrategy<int>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.Execute(null, x => { }));
    }
    
    [Fact]
    public void ForeachStrategy_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ForeachStrategy<int>();
        var data = new[] { 1, 2, 3 };
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.Execute(data, null));
    }
    
    [Fact]
    public void LinqStrategy_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new LinqStrategy<int>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.Execute(null, x => x));
    }
    
    [Fact]
    public void LinqStrategy_WithNullTransform_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new LinqStrategy<int>();
        var data = new[] { 1, 2, 3 };
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.Execute(data, null));
    }
    
    [Fact]
    public void ParallelLinqStrategy_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ParallelLinqStrategy<int>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.Execute(null, x => x));
    }
    
    [Fact]
    public void ParallelLinqStrategy_WithNullTransform_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ParallelLinqStrategy<int>();
        var data = new[] { 1, 2, 3 };
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.Execute(data, null));
    }
    
    [Fact]
    public void UnityOptimizedStrategy_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new UnityOptimizedStrategy<int>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.Execute(null, x => { }));
    }
    
    [Fact]
    public void UnityOptimizedStrategy_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new UnityOptimizedStrategy<int>();
        var data = new[] { 1, 2, 3 };
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.Execute(data, null));
    }
    
    [Fact]
    public void WasmOptimizedStrategy_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new WasmOptimizedStrategy<int>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.Execute(null, x => x));
    }
    
    [Fact]
    public void WasmOptimizedStrategy_WithNullTransform_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new WasmOptimizedStrategy<int>();
        var data = new[] { 1, 2, 3 };
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.Execute(data, null));
    }
    
    [Fact]
    public void StrategySelector_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var selector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => selector.SelectStrategy<object>(null));
    }
    
    [Fact]
    public void StrategySelector_WithNullStrategy_ShouldThrowArgumentNullException()
    {
        // Arrange
        var selector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => selector.RegisterStrategy(null));
    }
    
    [Fact]
    public void ForLoopStrategy_ExecuteWhere_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ForLoopStrategy<int>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.ExecuteWhere(null, x => true, x => x));
    }
    
    [Fact]
    public void ForLoopStrategy_ExecuteWhere_WithNullPredicate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ForLoopStrategy<int>();
        var data = new[] { 1, 2, 3 };
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.ExecuteWhere(data, null, x => x));
    }
    
    [Fact]
    public void ForLoopStrategy_ExecuteWhere_WithNullTransform_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ForLoopStrategy<int>();
        var data = new[] { 1, 2, 3 };
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.ExecuteWhere(data, x => true, null));
    }
    
    [Fact]
    public void CodeGenerationContext_WithNullPlatformTarget_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ForLoopStrategy<int>();
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
    public void CodeGenerationContext_WithNullCollectionName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ForLoopStrategy<int>();
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
    public void CodeGenerationContext_WithNullIterationBodyTemplate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new ForLoopStrategy<int>();
        var context = new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.CSharp,
            CollectionName = "items",
            IterationBodyTemplate = null
        };
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.GenerateCode(context));
    }
}
