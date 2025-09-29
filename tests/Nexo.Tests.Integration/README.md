# Nexo Integration Tests

This directory contains comprehensive integration tests for the Nexo framework, designed to test end-to-end workflows, performance, error handling, and system resilience.

## 🏗️ Test Architecture

The integration test suite follows a comprehensive testing strategy:

- **Test Fixtures**: Reusable test data and setup (`IntegrationTestFixture`)
- **Test Scenarios**: End-to-end workflow testing (`EndToEndWorkflowTests`)
- **Performance Tests**: Load and performance testing (`PerformanceIntegrationTests`)
- **Error Handling**: Resilience and error recovery testing (`ErrorHandlingIntegrationTests`)
- **Comprehensive Tests**: Full framework capabilities testing (`ComprehensiveIntegrationTests`)
- **Test Runner**: Orchestrated test execution (`IntegrationTestRunner`)
- **Utilities**: Helper functions and utilities (`IntegrationTestHelper`)

## 📁 Test Structure

```
tests/Nexo.Tests.Integration/
├── Commands/                          # Test command implementations
│   ├── TestIntegrationCommand.cs     # Integration test command
│   └── TestMasterOrchestrator.cs     # Master test orchestrator
├── Configuration/                     # Test configuration
│   └── IntegrationTestConfiguration.cs
├── Scenarios/                         # Test scenarios
│   ├── EndToEndWorkflowTests.cs      # End-to-end workflow tests
│   ├── PerformanceIntegrationTests.cs # Performance and load tests
│   └── ErrorHandlingIntegrationTests.cs # Error handling tests
├── TestFixtures/                      # Test fixtures and setup
│   └── IntegrationTestFixture.cs     # Main test fixture
├── Utilities/                         # Test utilities
│   └── IntegrationTestHelper.cs     # Helper functions
├── IntegrationTestRunner.cs         # Main test runner
├── ComprehensiveIntegrationTests.cs # Comprehensive test suite
├── IntegrationTests.cs              # Basic integration tests
├── MasterOrchestratorTests.cs       # Master orchestrator tests
└── README.md                         # This file
```

## 🧪 Test Categories

### 1. End-to-End Workflow Tests
- **Complete Workflow**: Full project creation to agent execution
- **Project Creation**: Project setup and configuration
- **Agent Creation**: Agent initialization and capabilities
- **Assembly Analysis**: Assembly analysis and security scanning
- **Integration Commands**: Different test scenarios and configurations

### 2. Performance Integration Tests
- **Execution Time**: Performance thresholds and timing
- **Concurrent Operations**: Multi-threaded execution
- **Load Testing**: Multiple iterations and stress testing
- **Memory Usage**: Memory consumption monitoring
- **Resource Cleanup**: Resource leak detection

### 3. Error Handling Tests
- **Invalid Input**: Graceful handling of invalid data
- **File System Errors**: Missing files and directories
- **Concurrent Conflicts**: Resource contention handling
- **Timeout Handling**: Long-running operation management
- **Resource Cleanup**: Error recovery and cleanup

### 4. Comprehensive Tests
- **Test Runner**: Full test suite execution
- **Configuration**: Custom configuration testing
- **System Requirements**: Environment validation
- **Utilities**: Helper function testing
- **Reporting**: Test report generation

## 🚀 Running Tests

### Run All Integration Tests
```bash
dotnet test tests/Nexo.Tests.Integration/Nexo.Tests.Integration/Nexo.Tests.Integration.csproj
```

### Run Specific Test Categories
```bash
# End-to-end workflow tests
dotnet test --filter "EndToEndWorkflowTests"

# Performance tests
dotnet test --filter "PerformanceIntegrationTests"

# Error handling tests
dotnet test --filter "ErrorHandlingIntegrationTests"

# Comprehensive tests
dotnet test --filter "ComprehensiveIntegrationTests"
```

### Run with Verbose Output
```bash
dotnet test tests/Nexo.Tests.Integration/Nexo.Tests.Integration/Nexo.Tests.Integration.csproj --verbosity normal
```

### Run with Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

## ⚙️ Configuration

### Environment Variables
- `NEXO_TEST_ENV`: Test environment (default: "Development")
- `NEXO_TEST_DATA_PATH`: Test data directory (default: "/tmp/nexo-integration-tests")
- `NEXO_CLEANUP_AFTER_TESTS`: Cleanup after tests (default: "true")
- `NEXO_GENERATE_COVERAGE`: Generate coverage report (default: "true")
- `NEXO_PERFORMANCE_MONITORING`: Enable performance monitoring (default: "true")
- `NEXO_RESOURCE_MONITORING`: Enable resource monitoring (default: "true")
- `NEXO_MAX_CONCURRENT`: Max concurrent operations (default: "5")
- `NEXO_DEFAULT_TIMEOUT`: Default timeout in seconds (default: "30")
- `NEXO_PERFORMANCE_THRESHOLD`: Performance threshold in ms (default: "5000")
- `NEXO_MEMORY_THRESHOLD`: Memory threshold in MB (default: "100")

### Test Configuration
```csharp
var config = new IntegrationTestConfiguration
{
    TestEnvironment = "Custom",
    TestDataPath = "/tmp/custom-tests",
    CleanupAfterTests = true,
    GenerateCoverageReport = true,
    EnablePerformanceMonitoring = true,
    EnableResourceMonitoring = true,
    MaxConcurrentOperations = 3,
    DefaultTimeoutSeconds = 60,
    PerformanceThresholdMs = 10000,
    MemoryThresholdMB = 200
};
```

## 📊 Test Results

### Test Summary
- **Total Test Suites**: Number of test suites executed
- **Passed Test Suites**: Successfully completed test suites
- **Failed Test Suites**: Failed test suites
- **Total Tests**: Total number of individual tests
- **Passed Tests**: Successfully completed tests
- **Failed Tests**: Failed tests
- **Execution Time**: Total execution duration
- **Success Rate**: Percentage of successful tests

### Test Reports
Integration tests generate comprehensive reports including:
- Test execution results
- Performance metrics
- Resource usage statistics
- System information
- Configuration details

## 🔧 Test Utilities

### IntegrationTestHelper
- **CreateTestDirectory()**: Create test directories
- **CleanupTestDirectory()**: Clean up test directories
- **MeasureExecutionTime()**: Measure operation timing
- **GetCurrentMemoryUsageMB()**: Monitor memory usage
- **GetCurrentHandleCount()**: Monitor handle usage
- **WaitForCondition()**: Wait for conditions with timeout
- **ExecuteConcurrently()**: Execute operations concurrently
- **RetryAsync()**: Retry operations with backoff
- **GenerateTestReport()**: Generate test reports
- **CheckSystemRequirements()**: Validate system requirements

### IntegrationTestFixture
- **CreateTestProjectAsync()**: Create test projects
- **CreateTestAgentAsync()**: Create test agents
- **AnalyzeTestAssemblyAsync()**: Analyze test assemblies
- **ExecuteCompleteWorkflowAsync()**: Execute full workflows
- **Dispose()**: Clean up resources

## 🎯 Test Scenarios

### 1. Full Integration Test
Tests the complete workflow from project creation through agent execution:
- Project creation and configuration
- Agent creation and initialization
- Assembly analysis and security scanning
- Agent task execution
- Resource cleanup

### 2. Performance Test
Validates system performance under various conditions:
- Execution time thresholds
- Concurrent operation handling
- Load testing with multiple iterations
- Memory usage monitoring
- Resource leak detection

### 3. Error Handling Test
Ensures graceful handling of error conditions:
- Invalid input validation
- File system error recovery
- Concurrent resource conflicts
- Timeout management
- Resource cleanup after errors

### 4. Configuration Test
Validates different configuration scenarios:
- Custom configuration settings
- Partial test suite execution
- Environment-specific settings
- Parameter validation

## 📈 Performance Benchmarks

### Execution Time Thresholds
- **Project Creation**: < 5 seconds
- **Agent Creation**: < 3 seconds
- **Assembly Analysis**: < 2 seconds
- **Complete Workflow**: < 10 seconds

### Resource Limits
- **Memory Usage**: < 100MB increase
- **Handle Count**: < 10 handle increase
- **Concurrent Operations**: Up to 5 simultaneous
- **Retry Attempts**: 3 with exponential backoff

### Load Testing
- **Concurrent Operations**: 3-5 simultaneous
- **Multiple Iterations**: 1-5 iterations
- **Average Execution Time**: < 8 seconds
- **Resource Cleanup**: No resource leaks

## 🛠️ Troubleshooting

### Common Issues
1. **System Requirements Not Met**: Ensure sufficient memory and processor cores
2. **File System Permissions**: Check write permissions for test directories
3. **Resource Cleanup**: Ensure proper disposal of test fixtures
4. **Timeout Issues**: Adjust timeout settings for slower systems

### Debugging
- Enable verbose logging: `--verbosity normal`
- Check system requirements: `IntegrationTestHelper.CheckSystemRequirements()`
- Monitor resource usage: Use performance monitoring utilities
- Review test reports: Generated JSON reports with detailed information

## 🔄 Continuous Integration

### CI/CD Integration
- Automated test execution on code changes
- Performance regression detection
- Resource usage monitoring
- Test report generation and storage
- Failure notification and reporting

### Test Environment
- Isolated test environments
- Clean test data setup
- Resource cleanup after tests
- Parallel test execution
- Test result aggregation

## 📚 Best Practices

### Test Design
- Use test fixtures for consistent setup
- Implement proper cleanup in teardown
- Use parameterized tests for multiple scenarios
- Include performance and resource monitoring
- Generate comprehensive test reports

### Test Execution
- Run tests in isolated environments
- Use appropriate timeouts and retries
- Monitor resource usage during tests
- Clean up test data after execution
- Validate system requirements before testing

### Test Maintenance
- Keep test data minimal and focused
- Update tests when requirements change
- Monitor test execution times
- Review and update performance thresholds
- Maintain test documentation
