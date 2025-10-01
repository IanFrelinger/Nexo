# Component Validation System

A comprehensive Unity test runner-based validation system for generated game components. This system ensures that all generated components are working correctly before they are promoted to the main pipeline.

## 🎯 Overview

The Component Validation System leverages Unity's test runner to validate generated game components across multiple dimensions:

- **Functionality**: Basic component instantiation, required components, interactions
- **Performance**: Component count limits, memory usage, rendering performance
- **Integration**: Component interactions, scene hierarchy, physics setup
- **Accessibility**: Color contrast, text readability, audio accessibility
- **Genre-Specific**: Genre-specific requirements and mechanics

## 🏗️ Architecture

### Core Components

- **`ComponentValidator`**: Main validation orchestrator
- **`TestRunnerIntegration`**: Unity test runner integration
- **`IComponentTest`**: Interface for test suites
- **Test Suites**: Specialized validation test suites

### Test Suites

1. **`FunctionalityTestSuite`**: Basic functionality validation
2. **`PerformanceTestSuite`**: Performance and resource validation
3. **`IntegrationTestSuite`**: Component integration validation
4. **`AccessibilityTestSuite`**: Accessibility compliance validation
5. **`GenreSpecificTestSuite`**: Genre-specific requirements validation

## 🚀 Usage

### Basic Validation

```csharp
var validator = new ComponentValidator();
var result = await validator.ValidateAsync(contentBundle, gamePlan);
```

### Validation with Retry Logic

```csharp
var validator = new ComponentValidator(maxRetries: 3, timeoutSeconds: 30f);
var result = await validator.ValidateWithRetryAsync(
    contentBundle, 
    gamePlan, 
    async (failedResult) => {
        // Regenerate content based on validation failures
        return await regenerateContentAsync(failedResult);
    });
```

### Test Runner Integration

```csharp
var testRunner = new TestRunnerIntegration();
var result = await testRunner.RunValidationTestsAsync(contentBundle, gamePlan);
```

## 🧪 Test Categories

### Functionality Tests
- Component instantiation
- Required components presence
- Basic interactions
- Null reference checks

### Performance Tests
- Component count limits
- Memory usage validation
- Rendering performance checks
- Triangle count validation

### Integration Tests
- Component interactions
- Scene hierarchy validation
- Physics setup verification
- Circular reference detection

### Accessibility Tests
- Color contrast compliance
- Text readability validation
- Audio accessibility checks
- UI element accessibility

### Genre-Specific Tests
- FPS: Weapons, enemies, spawn points
- Platformer: Platforms, collectibles, hazards
- RPG: NPCs, quest objects, dialogue systems

## 📊 Validation Results

### ComponentValidationResult
- Overall pass/fail status
- Overall score percentage
- Test suite results
- Individual test results
- Validation duration
- Error messages and recommendations

### TestSuiteResult
- Test suite pass/fail status
- Individual test results
- Duration and performance metrics
- Error messages

### TestResult
- Individual test pass/fail status
- Error messages
- Test duration

## 🔄 Retry Logic

The system includes intelligent retry logic that:

1. Runs validation tests
2. If validation fails, analyzes the failure reasons
3. Regenerates content with improvements
4. Re-runs validation tests
5. Repeats until success or max retries reached

## 🎮 Unity Integration

### Menu Items

Access validation through Unity menu:
- **Nexo → Director Studio → Run Component Validation Tests**
- **Nexo → Director Studio → Run FPS Validation Tests**
- **Nexo → Director Studio → Run Platformer Validation Tests**
- **Nexo → Director Studio → Run RPG Validation Tests**
- **Nexo → Director Studio → Run Performance Validation Tests**
- **Nexo → Director Studio → Run Accessibility Validation Tests**

### Test Runner Window

Tests can also be run through Unity's Test Runner window:
- **Window → General → Test Runner**
- Select "EditMode" tests
- Run "ComponentValidationTests"

## 📈 Quality Thresholds

### Genre-Specific Thresholds
- **FPS**: 85% minimum score
- **Platformer**: 80% minimum score  
- **RPG**: 90% minimum score

### Test Suite Requirements
- **FPS**: Functionality + Performance + Genre-Specific
- **Platformer**: Functionality + Integration + Genre-Specific
- **RPG**: Functionality + Integration + Accessibility + Genre-Specific

## 📄 Reporting

### Validation Reports
- Detailed test results
- Performance metrics
- Error analysis
- Recommendations for improvement

### Export Options
- Text-based reports
- JSON export (future)
- HTML reports (future)

## 🔧 Configuration

### AgentDirector Settings
```csharp
[Header("Validation Settings")]
public bool enableComponentValidation = true;
public float validationTimeoutSeconds = 30f;
public int maxValidationRetries = 3;
public float minValidationScore = 80f;
```

### TestRunnerConfiguration
```csharp
var config = new GenreTestConfig
{
    MinScore = 85f,
    RequiredTestSuites = new[] { "FunctionalityTestSuite", "PerformanceTestSuite" },
    MaxRetries = 3,
    TimeoutSeconds = 30f
};
```

## 🚨 Error Handling

### Common Issues
- Component instantiation failures
- Missing required components
- Performance threshold violations
- Integration problems
- Accessibility compliance issues

### Recommendations
The system provides specific recommendations for each type of failure:
- Regeneration suggestions
- Component improvement guidance
- Performance optimization tips
- Accessibility compliance fixes

## 🔮 Future Enhancements

### Planned Features
- Visual test results in Unity
- Automated fix suggestions
- Machine learning-based validation
- Real-time validation during generation
- Integration with CI/CD pipelines

### Advanced Testing
- Stress testing
- Load testing
- Cross-platform validation
- Multi-user testing
- Performance profiling integration

## 📚 Examples

### Basic Validation Example
```csharp
// Create validator
var validator = new ComponentValidator();

// Run validation
var result = await validator.ValidateAsync(contentBundle, gamePlan);

// Check results
if (result.OverallPassed)
{
    Debug.Log($"✅ Validation passed: {result.OverallScore:F1}%");
}
else
{
    Debug.LogError($"❌ Validation failed: {result.ValidationError}");
}
```

### Retry Logic Example
```csharp
// Create validator with retry logic
var validator = new ComponentValidator(maxRetries: 3);

// Run validation with retry
var result = await validator.ValidateWithRetryAsync(
    contentBundle, 
    gamePlan, 
    async (failedResult) => {
        // Analyze failures and regenerate
        var improvements = AnalyzeFailures(failedResult);
        return await RegenerateWithImprovements(contentBundle, improvements);
    });
```

### Test Runner Integration Example
```csharp
// Create test runner
var testRunner = new TestRunnerIntegration();

// Run specific test categories
var result = await testRunner.RunTargetedValidationAsync(
    contentBundle, 
    gamePlan, 
    new[] { "PerformanceTestSuite", "AccessibilityTestSuite" });
```

This validation system ensures that all generated game components meet quality standards and work correctly before being integrated into the final game slice.
