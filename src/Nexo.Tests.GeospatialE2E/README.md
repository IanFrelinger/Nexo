# Geospatial E2E Smoke Tests

End-to-end smoke tests for the geospatial application covering CLI commands, API services, validation features, job persistence, and core functionality.

## Test Coverage

### CLI Tests
- ✅ `geoterrain bounds-to-obj` with validation flags
- ✅ `geovector buildings-to-obj` command
- ✅ Validation integration (data integrity, mesh quality)

### API Service Tests
- ✅ Terrain generation service
- ✅ Vector extraction service (buildings, roads, water)
- ✅ World generation service
- ✅ Job status retrieval
- ✅ Job persistence across service restarts

### Validation Tests
- ✅ Data integrity validation
- ✅ Mesh quality reporting
- ✅ World bundle validation

### Infrastructure Tests
- ✅ Job repository persistence
- ✅ Job cleanup service

## Running Tests

```bash
# Run all E2E tests
dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj

# Run with verbose output
dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj --verbosity detailed

# Run specific test
dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj --filter "FullyQualifiedName~CLI_GeoTerrain"
```

## Test Structure

Tests use:
- **xUnit** for test framework
- **FluentAssertions** for assertions
- **In-memory SQLite** for job repository (isolated per test)
- **Echo providers** for deterministic testing (no network calls)

## Test Isolation

Each test:
- Creates its own temporary output directory
- Uses isolated job repository database
- Cleans up after execution
- Uses echo providers for deterministic results

## Adding New Tests

When adding new E2E tests:

1. Follow the naming convention: `{Component}_{Feature}_{ExpectedBehavior}`
2. Use echo providers for deterministic testing
3. Clean up resources in `Dispose()` method
4. Use FluentAssertions for readable assertions
5. Test both success and failure scenarios

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
