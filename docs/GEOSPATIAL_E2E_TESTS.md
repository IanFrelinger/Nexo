# Geospatial E2E Smoke Tests

## Overview

Comprehensive end-to-end smoke tests for the geospatial application covering CLI commands, API services, validation features, job persistence, and core functionality.

## Test Project

**Location:** `src/Nexo.Tests.GeospatialE2E/`

**Framework:** xUnit with FluentAssertions

## Test Coverage

### ✅ CLI Tests

1. **CLI_GeoTerrain_BoundsToObj_WithValidation_ShouldSucceed**
   - Tests `geoterrain bounds-to-obj` command
   - Validates `--validate-integrity` flag integration
   - Validates `--mesh-quality-report` flag integration
   - Verifies OBJ file generation
   - Uses echo provider for deterministic testing

2. **CLI_GeoVector_BuildingsToObj_ShouldSucceed**
   - Tests `geovector buildings-to-obj` command
   - Verifies building mesh generation
   - Uses echo provider

3. **CLI_Validation_ShouldDetectCorruption**
   - Tests validation flags work correctly
   - Verifies integrity checking
   - Verifies mesh quality reporting

### ✅ API Service Tests

4. **API_GeoTerrain_GenerateTerrain_ShouldCreateJob**
   - Tests terrain generation service
   - Verifies job creation
   - Verifies job persistence in repository

5. **API_GeoTerrain_GetJobStatus_ShouldReturnJob**
   - Tests job status retrieval
   - Verifies job state tracking

6. **API_GeoVector_ExtractFeatures_ShouldSupportMultipleFeatureKinds**
   - Tests building extraction
   - Tests road extraction
   - Tests water extraction
   - Verifies all feature kinds work via API

7. **API_World_GenerateWorld_ShouldCreateJob**
   - Tests world bundle generation
   - Verifies job creation

8. **API_World_ValidateWorld_ShouldReturnValidationResult**
   - Tests world bundle validation endpoint

### ✅ Infrastructure Tests

9. **API_JobPersistence_ShouldSurviveServiceRestart**
   - Tests job persistence in SQLite repository
   - Simulates service restart scenario
   - Verifies jobs survive across restarts

10. **JobCleanup_ShouldDeleteOldJobs**
    - Tests job cleanup service
    - Verifies old jobs are deleted after retention period

## Running Tests

```bash
# Run all E2E tests
dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj

# Run with verbose output
dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj --verbosity detailed

# Run specific test
dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj --filter "FullyQualifiedName~CLI_GeoTerrain"

# Run with code coverage
dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj --collect:"XPlat Code Coverage"
```

## Test Structure

### Test Isolation

Each test:
- Creates its own temporary output directory
- Uses isolated SQLite job repository database
- Uses echo providers for deterministic results (no network calls)
- Cleans up resources in `Dispose()` method

### Service Setup

Tests use a custom `ServiceProvider` that:
- Registers all required services (commands, repositories, HTTP clients)
- Uses in-memory SQLite for job storage
- Provides isolated test environment

### Assertions

Tests use FluentAssertions for readable assertions:
```csharp
exitCode.Should().Be(0, "Command should succeed");
File.Exists(outputFile).Should().BeTrue("Output file should be created");
job.Should().NotBeNull("Job should persist in repository");
```

## Test Execution Flow

1. **Setup**: Create test directory, initialize service provider
2. **Execute**: Run CLI command or API service method
3. **Verify**: Check exit codes, file existence, job persistence
4. **Cleanup**: Remove temporary files and directories

## Dependencies

- **xUnit** - Test framework
- **FluentAssertions** - Assertion library
- **Microsoft.NET.Test.Sdk** - Test SDK
- **Echo Providers** - Deterministic test data (no network)

## Adding New Tests

When adding new E2E tests:

1. Follow naming convention: `{Component}_{Feature}_{ExpectedBehavior}`
2. Use echo providers for deterministic testing
3. Create isolated test directories
4. Clean up in `Dispose()` method
5. Use FluentAssertions for assertions
6. Test both success and failure scenarios

## Example Test

```csharp
[Fact]
public async Task CLI_GeoTerrain_BoundsToObj_WithValidation_ShouldSucceed()
{
    // Arrange
    var outputFile = Path.Combine(_testOutputDir, "test-terrain.obj");
    var command = CreateGeoTerrainCommand();

    // Act
    var exitCode = await command.BoundsToObjAsync(
        bounds: "37.0,-122.0,37.1,-121.9",
        output: new FileInfo(outputFile),
        provider: "echo",
        validateIntegrity: true,
        meshQualityReport: true,
        // ... other parameters
        CancellationToken.None);

    // Assert
    exitCode.Should().Be(0, "Command should succeed");
    File.Exists(outputFile).Should().BeTrue("Output file should be created");
}
```

## Integration with CI/CD

These tests should be run:
- On every pull request
- Before merging to main
- As part of release validation
- In nightly builds

## Test Metrics

- **Total Tests**: 10
- **Test Categories**: CLI, API, Infrastructure
- **Coverage**: CLI commands, API services, job persistence, validation
- **Execution Time**: ~30-60 seconds (depends on system)

## Known Limitations

- Tests use echo providers (no real data validation)
- Progress streaming not fully tested (requires web host)
- Some validation endpoints return placeholders

## Future Enhancements

- Add tests for real provider integration (with mocks)
- Add tests for progress streaming (requires WebApplicationFactory)
- Add performance benchmarks
- Add stress tests for large datasets
- Add tests for error recovery scenarios
