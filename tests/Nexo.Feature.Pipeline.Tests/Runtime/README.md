# Cross-Runtime Testing Infrastructure

This directory contains a comprehensive cross-runtime testing infrastructure for the Nexo Pipeline feature, designed to test functionality across different runtime environments including .NET, Unity, Mono, and CoreCLR.

## Overview

The cross-runtime testing infrastructure provides:

- **Runtime Detection** - Automatically detects the current runtime environment
- **Runtime-Specific Test Attributes** - Mark tests for specific runtimes or features
- **Unity Integration** - Specialized testing for Unity Engine environments
- **Performance Monitoring** - Runtime-specific performance expectations
- **Feature Compatibility** - Test feature availability across runtimes

## Architecture

### Core Components

#### 1. Runtime Detection (`RuntimeDetection.cs`)
```csharp
// Detect current runtime
var runtime = RuntimeDetection.CurrentRuntime; // DotNet, Unity, Mono, CoreCLR, Unknown

// Get runtime information
var info = RuntimeDetection.GetRuntimeInfo();
// Output: "Runtime: DotNet, Version: 8.0.7, Framework: .NET 8.0.7, OS: Darwin 24.5.0, Architecture: Arm64"

// Check feature support
bool supportsAsync = RuntimeDetection.SupportsFeature("async");
bool supportsThreading = RuntimeDetection.SupportsFeature("threading");
```

#### 2. Cross-Runtime Test Base (`CrossRuntimeTestBase.cs`)
Base class providing common functionality for cross-runtime tests:

```csharp
public class MyCrossRuntimeTest : CrossRuntimeTestBase
{
    public MyCrossRuntimeTest(ITestOutputHelper testOutput) : base(testOutput)
    {
    }

    [RuntimeTimeout(5000)]
    public void MyTest()
    {
        RunWithRuntimeTimeout(() =>
        {
            // Test logic here
            AssertRuntimeCondition(condition, "Message");
        });
    }
}
```

#### 3. Runtime-Specific Test Attributes

##### `[RuntimeFact]` - Run tests only on specific runtimes
```csharp
[RuntimeFact(RuntimeDetection.RuntimeType.DotNet, RuntimeDetection.RuntimeType.CoreCLR)]
public void DotNetOnlyTest() { }

[RuntimeFact(RuntimeDetection.RuntimeType.Unity)]
public void UnityOnlyTest() { }
```

##### `[RuntimeTheory]` - Theory tests for specific runtimes
```csharp
[RuntimeTheory(RuntimeDetection.RuntimeType.DotNet)]
[InlineData("test1")]
[InlineData("test2")]
public void DotNetTheoryTest(string data) { }
```

##### `[RequiresFeature]` - Skip tests if feature not supported
```csharp
[RequiresFeature("threading")]
public void ThreadingTest() { }

[RequiresFeature("async")]
public void AsyncTest() { }
```

##### `[SkipOnRuntime]` - Skip tests on specific runtimes
```csharp
[SkipOnRuntime(RuntimeDetection.RuntimeType.Unity)]
public void NonUnityTest() { }
```

##### `[RuntimeTimeout]` - Runtime-adjusted timeouts
```csharp
[RuntimeTimeout(5000)] // 5s base, adjusted per runtime
public void TimeoutTest() { }
```

#### 4. Unity Test Adapter (`UnityTestAdapter.cs`)
Specialized testing for Unity environments:

```csharp
// Run Unity-specific tests
UnityTestAdapter.RunUnityTest(() =>
{
    // Unity-specific test logic
}, logger);

// Run Unity async tests
UnityTestAdapter.RunUnityTestAsync(async () =>
{
    await Task.Delay(100);
    // Async test logic
}, logger);

// Run Unity coroutine tests
UnityTestAdapter.RunUnityCoroutineTest(() =>
{
    // Coroutine logic
    yield return null;
}, logger);
```

## Usage Examples

### Basic Cross-Runtime Test
```csharp
public class PipelineCrossRuntimeTests : CrossRuntimeTestBase
{
    public PipelineCrossRuntimeTests(ITestOutputHelper testOutput) : base(testOutput)
    {
    }

    [RuntimeTimeout(5000)]
    public void Pipeline_Works_Across_Runtimes()
    {
        Logger.LogInformation($"Testing on runtime: {CurrentRuntime}");
        
        RunWithRuntimeTimeout(() =>
        {
            var config = new PipelineConfiguration();
            var context = new PipelineContext(Logger, config);
            
            AssertRuntimeCondition(!string.IsNullOrEmpty(context.ExecutionId), 
                "Pipeline should work across runtimes");
        });
    }
}
```

### Runtime-Specific Test
```csharp
[RuntimeFact(RuntimeDetection.RuntimeType.DotNet)]
public void DotNet_Specific_Feature()
{
    // This test only runs on .NET runtime
    AssertRuntimeCondition(CurrentRuntime == RuntimeDetection.RuntimeType.DotNet);
}

[RuntimeFact(RuntimeDetection.RuntimeType.Unity)]
public void Unity_Specific_Feature()
{
    // This test only runs on Unity runtime
    UnityTestAdapter.RunUnityTest(() =>
    {
        // Unity-specific logic
    }, Logger);
}
```

### Feature-Dependent Test
```csharp
[RequiresFeature("threading")]
public void Concurrent_Operations_Test()
{
    // This test is skipped if threading is not supported
    SkipIfFeatureNotSupported("threading", "Threading required for this test");
    
    // Test concurrent operations
}
```

### Performance Test
```csharp
[RuntimeTimeout(5000)]
public void Performance_Test()
{
    var startTime = DateTime.UtcNow;
    
    // Perform operations
    var config = new PipelineConfiguration();
    var context = new PipelineContext(Logger, config);
    
    var elapsed = DateTime.UtcNow - startTime;
    var maxExpectedTime = GetRuntimeAdjustedTimeout(100); // 100ms base
    
    AssertRuntimeCondition(elapsed.TotalMilliseconds < maxExpectedTime, 
        $"Operations should complete within {maxExpectedTime}ms on {CurrentRuntime}");
}
```

## Runtime-Specific Considerations

### .NET Runtime
- **Performance**: Fast execution, low memory overhead
- **Features**: Full .NET feature support
- **Timeout Multiplier**: 1.0x (base timeout)

### Unity Runtime
- **Performance**: Slower execution, higher memory usage
- **Features**: Limited threading, coroutine support
- **Timeout Multiplier**: 2.0x (double base timeout)
- **Memory Limit**: 512MB max for tests

### Mono Runtime
- **Performance**: Significantly slower
- **Features**: Limited feature support
- **Timeout Multiplier**: 3.0x (triple base timeout)

### CoreCLR Runtime
- **Performance**: Fast execution
- **Features**: Full .NET Core feature support
- **Timeout Multiplier**: 1.0x (base timeout)

## Feature Support Matrix

| Feature | .NET | Unity | Mono | CoreCLR |
|---------|------|-------|------|---------|
| async/await | ✅ | ✅ | ✅ | ✅ |
| reflection | ✅ | ✅ | ✅ | ✅ |
| LINQ | ✅ | ✅ | ✅ | ✅ |
| JSON | ✅ | ⚠️ | ✅ | ✅ |
| threading | ✅ | ❌ | ✅ | ✅ |
| dynamic | ✅ | ❌ | ✅ | ✅ |
| serialization | ✅ | ⚠️ | ✅ | ✅ |

## Running Tests

### All Cross-Runtime Tests
```bash
dotnet test tests/Nexo.Feature.Pipeline.Tests/Nexo.Feature.Pipeline.Tests.csproj
```

### Specific Runtime Tests
```bash
# .NET only tests
dotnet test --filter "RuntimeFactAttribute"

# Unity only tests  
dotnet test --filter "RuntimeFactAttribute&SupportedRuntimes=Unity"

# Feature-dependent tests
dotnet test --filter "RequiresFeatureAttribute"
```

### With Detailed Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Test Results Interpretation

### Passed Tests
- ✅ **Cross-Runtime Tests**: Working across all supported runtimes
- ✅ **Runtime-Specific Tests**: Working on intended runtime
- ✅ **Feature Tests**: Feature available and working

### Skipped Tests
- ⏭️ **Runtime Mismatch**: Test designed for different runtime
- ⏭️ **Feature Unavailable**: Required feature not supported

### Failed Tests
- ❌ **Runtime Issues**: Problems specific to runtime environment
- ❌ **Performance Issues**: Exceeded runtime-adjusted timeouts
- ❌ **Feature Issues**: Expected features not working

## Best Practices

### 1. Use Runtime-Specific Attributes
```csharp
// Good: Runtime-specific test
[RuntimeFact(RuntimeDetection.RuntimeType.DotNet)]
public void DotNetSpecificTest() { }

// Avoid: Generic test that might fail on other runtimes
[Fact]
public void GenericTest() { }
```

### 2. Check Feature Support
```csharp
// Good: Check feature availability
[RequiresFeature("threading")]
public void ThreadingTest()
{
    SkipIfFeatureNotSupported("threading");
    // Test logic
}

// Avoid: Assume feature availability
[Fact]
public void ThreadingTest()
{
    // Might fail on Unity
}
```

### 3. Use Runtime-Adjusted Timeouts
```csharp
// Good: Runtime-adjusted timeout
[RuntimeTimeout(5000)]
public void TimeoutTest() { }

// Avoid: Fixed timeout
[Fact(Timeout = 5000)]
public void TimeoutTest() { }
```

### 4. Log Runtime Information
```csharp
public void MyTest()
{
    Logger.LogInformation($"Testing on {CurrentRuntime}");
    Logger.LogInformation($"Runtime info: {RuntimeDetection.GetRuntimeInfo()}");
    
    // Test logic
}
```

### 5. Handle Runtime Differences
```csharp
public void CrossRuntimeTest()
{
    if (CurrentRuntime == RuntimeDetection.RuntimeType.Unity)
    {
        // Unity-specific logic
        UnityTestAdapter.RunUnityTest(() => { /* logic */ }, Logger);
    }
    else
    {
        // Standard .NET logic
        RunWithRuntimeTimeout(() => { /* logic */ });
    }
}
```

## Extending the Infrastructure

### Adding New Runtime Types
1. Add to `RuntimeDetection.RuntimeType` enum
2. Update detection logic in `RuntimeDetection.CurrentRuntime`
3. Add timeout multipliers in `GetRuntimeAdjustedTimeout`

### Adding New Features
1. Add feature to `RuntimeDetection.SupportsFeature`
2. Update feature support matrix
3. Add tests using `[RequiresFeature]` attribute

### Adding Unity-Specific Functionality
1. Extend `UnityTestAdapter` with new methods
2. Add Unity-specific test utilities
3. Update Unity configuration constants

## Troubleshooting

### Common Issues

#### Tests Skipped Unexpectedly
- Check runtime detection logic
- Verify feature support detection
- Review test attributes

#### Performance Test Failures
- Adjust timeout multipliers
- Check runtime-specific performance expectations
- Review memory usage limits

#### Unity Tests Not Running
- Ensure Unity runtime detection works
- Check Unity-specific assemblies
- Verify Unity feature detection

### Debug Information
Enable detailed logging to see runtime information:
```bash
dotnet test --logger "console;verbosity=detailed"
```

This will show:
- Current runtime detection
- Feature support status
- Performance metrics
- Test execution details

## Future Enhancements

### Planned Features
- **Runtime Performance Baselines**: Store and compare performance across runs
- **Automated Runtime Testing**: CI/CD integration for multiple runtimes
- **Unity Editor Integration**: Direct Unity Editor testing support
- **Cross-Platform Testing**: Support for different OS platforms
- **Memory Profiling**: Detailed memory usage analysis

### Integration Opportunities
- **Unity Test Framework**: Integration with Unity's test framework
- **Performance Monitoring**: Integration with performance monitoring tools
- **CI/CD Pipelines**: Automated testing across multiple runtime environments
- **Reporting**: Detailed test reports with runtime-specific metrics 