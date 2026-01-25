# Geospatial Unit Tests (White Box Tests)

Comprehensive white box (unit) tests for the geospatial application. These tests focus on testing internal implementation details, individual classes, methods, and their interactions in isolation.

## Test Project

**Location:** `src/Nexo.Tests.GeospatialUnit/`

**Framework:** xUnit with FluentAssertions and Moq

## Test Coverage

### ✅ Service Layer Tests

1. **GeoTerrainServiceTests**
   - Job creation and ID generation
   - Job status transitions (pending → processing → completed)
   - Error handling and failure scenarios
   - Webhook notification integration
   - Output path retrieval

2. **GeoVectorServiceTests**
   - Building extraction
   - Road extraction
   - Water extraction
   - Unsupported feature kind handling
   - Job status retrieval

3. **WorldServiceTests**
   - World bundle generation
   - World validation
   - Directory existence checks
   - Job status management

### ✅ Repository Tests

4. **SqliteJobRepositoryTests**
   - Job creation and storage
   - Job retrieval (by ID and list)
   - Job updates and status transitions
   - Job deletion
   - Old job cleanup
   - Concurrent access handling
   - Data persistence verification

### ✅ Validation Tests

5. **ValidationTests**
   - Checksum computation and validation
   - Data corruption detection
   - Projection parameter validation
   - Mesh quality metrics calculation
   - Edge cases and error handling

### ✅ Model Tests

6. **ApiModelsTests**
   - Request validation (required fields)
   - Model initialization
   - Record mutation (with expressions)
   - Validation response structure

### ✅ Infrastructure Tests

7. **JobCleanupServiceTests**
   - Background service lifecycle
   - Service start/stop
   - Cleanup cycle execution

## Running Tests

```bash
# Run all unit tests
dotnet test src/Nexo.Tests.GeospatialUnit/Nexo.Tests.GeospatialUnit.csproj

# Run with verbose output
dotnet test src/Nexo.Tests.GeospatialUnit/Nexo.Tests.GeospatialUnit.csproj --verbosity detailed

# Run specific test class
dotnet test src/Nexo.Tests.GeospatialUnit/Nexo.Tests.GeospatialUnit.csproj --filter "FullyQualifiedName~GeoTerrainServiceTests"

# Run with code coverage
dotnet test src/Nexo.Tests.GeospatialUnit/Nexo.Tests.GeospatialUnit.csproj --collect:"XPlat Code Coverage"
```

## Test Structure

### Mocking Strategy

Tests use **Moq** for mocking dependencies:
- CLI commands are mocked to avoid actual execution
- Repositories are tested with real SQLite (in-memory)
- Loggers are mocked to avoid console noise
- Webhook services are mocked to avoid network calls

### Test Isolation

Each test:
- Uses isolated test data
- Creates temporary directories/files
- Cleans up resources in `Dispose()` methods
- Does not depend on external services

### Assertions

Tests use **FluentAssertions** for readable assertions:
```csharp
jobId.Should().NotBeNullOrEmpty();
result.Status.Should().Be("completed");
_mockRepository.Verify(r => r.CreateJobAsync(...), Times.Once);
```

## Test Categories

### Unit Tests
- Test individual methods in isolation
- Mock all dependencies
- Fast execution (< 1 second per test)
- No external dependencies

### Integration Tests (Repository)
- Test SQLite repository with real database
- Verify data persistence
- Test concurrent access patterns

## Example Test

```csharp
[Fact]
public async Task GenerateTerrainAsync_ShouldCreateJob_AndReturnJobId()
{
    // Arrange
    var request = new TerrainGenerationRequest
    {
        Bounds = "37.0,-122.0,37.1,-121.9",
        ElevationProvider = "srtm",
        Format = "obj"
    };

    _mockCommand
        .Setup(c => c.BoundsToObjAsync(...))
        .ReturnsAsync(0);

    // Act
    var jobId = await _service.GenerateTerrainAsync(request);

    // Assert
    jobId.Should().NotBeNullOrEmpty();
    _mockRepository.Verify(r => r.CreateJobAsync(...), Times.Once);
}
```

## Test Metrics

- **Total Tests**: ~30+ unit tests
- **Test Categories**: Services, Repository, Validation, Models, Infrastructure
- **Coverage**: Core business logic, data access, validation, error handling
- **Execution Time**: < 5 seconds for full suite

## Key Testing Patterns

### 1. Arrange-Act-Assert (AAA)
All tests follow the AAA pattern for clarity.

### 2. Mock Verification
Verify that mocked dependencies are called with correct parameters.

### 3. Edge Case Testing
Test null inputs, empty strings, invalid data, boundary conditions.

### 4. Error Path Testing
Test exception handling, failure scenarios, and error propagation.

### 5. State Verification
Verify that state changes correctly (job status transitions, data persistence).

## Adding New Tests

When adding new unit tests:

1. Follow naming convention: `{Method}_{Condition}_{ExpectedResult}`
2. Use Moq for all external dependencies
3. Use FluentAssertions for assertions
4. Clean up resources in `Dispose()` methods
5. Test both success and failure paths
6. Test edge cases and boundary conditions
7. Verify mock interactions when appropriate

## Integration with CI/CD

These tests should be run:
- On every commit
- Before merging pull requests
- As part of the build pipeline
- Before releases

## Coverage Goals

- **Services**: 80%+ coverage
- **Repository**: 90%+ coverage
- **Validation**: 85%+ coverage
- **Models**: 70%+ coverage

## Known Limitations

- Some tests use delays for async operations (could be improved with better async testing)
- Mock setup can be verbose for complex method signatures
- Some edge cases may not be fully covered yet

## Future Enhancements

- Add property-based tests (FsCheck or similar)
- Add performance/benchmark tests
- Add mutation testing
- Improve async testing patterns
- Add more edge case coverage
