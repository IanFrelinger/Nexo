# E2E Smoke Tests for NexoDirector

Comprehensive end-to-end smoke tests for the NexoDirector pipeline, ensuring the entire system works correctly from natural language input to validated game components.

## 🎯 Overview

The E2E smoke tests provide comprehensive validation of the NexoDirector pipeline across multiple dimensions:

- **End-to-End Pipeline Tests**: Complete flow from brief to validated content
- **Integration Tests**: Component interaction and service resolution
- **Performance Tests**: Load testing and performance benchmarks
- **CI/CD Tests**: Automated validation for build pipelines
- **Component Validation Tests**: Unity test runner-based component validation

## 🧪 Test Categories

### 1. E2E Smoke Tests (`E2ESmokeTests.cs`)

Tests the complete pipeline from natural language input to validated game components:

- **FPS Pipeline**: Doom-style FPS with weapons and enemies
- **Platformer Pipeline**: Mario-style platformer with jumps and collectibles
- **RPG Pipeline**: Fantasy RPG with NPCs and quests
- **Retry Logic**: Tests automatic regeneration on validation failures
- **Performance**: Pipeline completion within time limits
- **Accessibility**: Accessibility compliance validation
- **Integration**: Complex system interactions
- **Error Handling**: Graceful failure handling
- **Concurrent Execution**: Multiple pipelines running simultaneously
- **Memory Usage**: Memory leak detection
- **Deterministic Generation**: Reproducible results with same seed

### 2. Integration Tests (`IntegrationTests.cs`)

Tests component interaction and service resolution:

- **Service Container**: All services can be resolved
- **Command Chain**: Commands execute in correct order
- **Genre Profiles**: Genre-specific functionality works
- **Validators**: Validation system integration
- **Adapters**: External adapter health checks
- **Auto-Fix Workflow**: End-to-end auto-fix process
- **Data Flow**: Data consistency across pipeline
- **Error Propagation**: Graceful error handling
- **Concurrent Execution**: No interference between pipelines
- **Resource Cleanup**: Memory leak prevention

### 3. Performance Tests (`PerformanceTests.cs`)

Load testing and performance benchmarks:

- **Single Pipeline**: Individual pipeline performance
- **Concurrent Pipelines**: Scalability testing
- **Memory Usage**: Memory consumption monitoring
- **Validation Speed**: Component validation performance
- **Large Content Bundles**: Handling of complex content
- **Stress Testing**: System stability under load
- **Command Latency**: Consistent performance
- **Resource Cleanup**: Memory leak prevention
- **Validation Throughput**: Concurrent validation performance

### 4. CI/CD Tests (`CICDTests.cs`)

Automated validation for build pipelines:

- **Smoke Test**: Basic functionality verification
- **All Genres**: FPS, Platformer, RPG validation
- **Service Resolution**: Dependency injection verification
- **Data Consistency**: Data flow validation
- **Concurrent Execution**: Parallel processing
- **Error Handling**: Graceful failure handling
- **Validation Integration**: Component validation
- **Genre Profiles**: Profile availability
- **Adapter Health**: External service health
- **Stress Testing**: System stability
- **Memory Usage**: Resource management
- **Deterministic Generation**: Reproducible results

### 5. Component Validation Tests (`ComponentValidationTests.cs`)

Unity test runner-based component validation:

- **FPS Components**: Weapon and enemy validation
- **Platformer Components**: Platform and collectible validation
- **RPG Components**: NPC and quest validation
- **Retry Logic**: Validation with regeneration
- **Performance Requirements**: Performance validation
- **Accessibility Requirements**: Accessibility validation
- **Integration Requirements**: Component interaction validation
- **Genre-Specific Requirements**: Genre-specific validation

## 🚀 Usage

### Unity Menu Access

Access tests through Unity menu:
- **Nexo → Director Studio → Run E2E Smoke Tests**
- **Nexo → Director Studio → Run Integration Tests**
- **Nexo → Director Studio → Run Performance Tests**
- **Nexo → Director Studio → Run CI/CD Tests**
- **Nexo → Director Studio → Run All Tests**
- **Nexo → Director Studio → Run Production Tests**

### Test Runner Window

Run tests through Unity's Test Runner:
- **Window → General → Test Runner**
- Select "EditMode" tests
- Run specific test categories or all tests

### Programmatic Execution

```csharp
// Run specific test category
var result = await TestRunnerConfiguration.RunTestCategoryAsync("E2E", "Development");

// Run all tests
var result = await TestRunnerConfiguration.RunAllTestsAsync("Production");

// Export test report
await TestRunnerConfiguration.ExportTestReportAsync(result, "test_report.txt");
```

## 📊 Test Configurations

### Development Environment
- **Timeout**: 120 seconds
- **Max Retries**: 3
- **Performance Tests**: Enabled
- **Stress Tests**: Enabled
- **Memory Tests**: Enabled
- **Min Validation Score**: 70%

### CI Environment
- **Timeout**: 60 seconds
- **Max Retries**: 1
- **Performance Tests**: Disabled
- **Stress Tests**: Disabled
- **Memory Tests**: Enabled
- **Min Validation Score**: 60%

### Production Environment
- **Timeout**: 180 seconds
- **Max Retries**: 5
- **Performance Tests**: Enabled
- **Stress Tests**: Enabled
- **Memory Tests**: Enabled
- **Min Validation Score**: 85%

## 📈 Performance Benchmarks

### Expected Performance
- **Single Pipeline**: < 30 seconds
- **Concurrent Pipelines**: < 60 seconds for 5 pipelines
- **Memory Usage**: < 100MB increase
- **Validation Speed**: < 10 seconds
- **Large Content Bundles**: < 45 seconds
- **Stress Testing**: > 90% success rate

### Performance Metrics
- **Pipeline Duration**: Total time for complete pipeline
- **Memory Usage**: Peak memory consumption
- **Validation Score**: Component validation quality
- **Success Rate**: Percentage of successful tests
- **Concurrency**: Parallel execution performance

## 🔧 Test Configuration

### Test Suite Configuration
```csharp
var config = new TestSuiteConfig
{
    TimeoutSeconds = 60,
    MaxRetries = 3,
    EnablePerformanceTests = true,
    EnableStressTests = true,
    EnableMemoryTests = true,
    MinValidationScore = 70f
};
```

### Environment-Specific Settings
- **Development**: Full test suite with relaxed timeouts
- **CI**: Fast tests with strict timeouts
- **Production**: Comprehensive tests with high quality thresholds

## 📄 Test Reporting

### Test Execution Results
- **Category**: Test category executed
- **Environment**: Execution environment
- **Duration**: Total execution time
- **Success**: Overall pass/fail status
- **Tests Run**: Total number of tests
- **Tests Passed**: Number of successful tests
- **Tests Failed**: Number of failed tests
- **Performance Metrics**: Performance data
- **Error Messages**: Detailed error information

### Report Export
- **Text Reports**: Human-readable test reports
- **Performance Metrics**: Detailed performance data
- **Error Analysis**: Comprehensive error reporting
- **Category Breakdown**: Results by test category

## 🚨 Error Handling

### Common Issues
- **Timeout Errors**: Tests exceeding time limits
- **Memory Leaks**: Excessive memory usage
- **Validation Failures**: Component validation issues
- **Service Resolution**: Dependency injection problems
- **Concurrent Issues**: Race conditions in parallel execution

### Error Recovery
- **Retry Logic**: Automatic retry on failures
- **Graceful Degradation**: Fallback to simpler tests
- **Error Reporting**: Detailed error information
- **Resource Cleanup**: Automatic cleanup on failures

## 🔮 Future Enhancements

### Planned Features
- **Visual Test Results**: Unity Editor integration
- **Real-Time Monitoring**: Live test execution monitoring
- **Automated Fixes**: Automatic issue resolution
- **Performance Profiling**: Detailed performance analysis
- **Cross-Platform Testing**: Multi-platform validation

### Advanced Testing
- **Load Testing**: High-load scenario testing
- **Stress Testing**: System breaking point testing
- **Security Testing**: Security vulnerability testing
- **Compatibility Testing**: Cross-version compatibility
- **Regression Testing**: Automated regression detection

## 📚 Examples

### Basic E2E Test
```csharp
[Test]
public async Task E2E_FPS_Pipeline_ShouldCompleteSuccessfully()
{
    var brief = new DesignBrief(
        Description: "Doom-style FPS with intense combat",
        GenreHint: "FPS",
        TargetDurationMinutes: 5,
        DifficultyLevel: 4,
        Seed: 12345
    );

    var result = await RunCompletePipelineAsync(brief);
    
    Assert.IsTrue(result.Success);
    Assert.IsNotNull(result.GamePlan);
    Assert.IsNotNull(result.WorldLayout);
    Assert.IsNotNull(result.InteractionGraph);
    Assert.IsNotNull(result.ContentBundle);
    Assert.IsTrue(result.ValidationResult.OverallPassed);
}
```

### Performance Test
```csharp
[Test]
public async Task Performance_SinglePipeline_ShouldCompleteWithinTimeLimit()
{
    var brief = new DesignBrief(/* ... */);
    var stopwatch = Stopwatch.StartNew();
    
    var result = await RunCompletePipelineAsync(brief);
    stopwatch.Stop();
    
    Assert.IsTrue(stopwatch.ElapsedMilliseconds < 30000);
    Assert.IsTrue(result.Success);
}
```

### CI/CD Test
```csharp
[Test]
[Timeout(30000)]
public async Task CICD_SmokeTest_ShouldPass()
{
    var brief = new DesignBrief(/* ... */);
    var result = await RunMinimalPipelineAsync(brief);
    
    Assert.IsTrue(result.Success);
    Assert.IsNotNull(result.GamePlan);
    Assert.IsNotNull(result.WorldLayout);
}
```

This comprehensive test suite ensures that the NexoDirector pipeline is robust, performant, and reliable across all scenarios and environments.
