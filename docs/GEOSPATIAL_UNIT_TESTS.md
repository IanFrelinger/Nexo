# Geospatial Unit Tests Documentation

## Overview

Comprehensive white box (unit) tests for the geospatial application. These tests validate internal implementation details, ensuring that individual components work correctly in isolation.

## Test Suite Structure

### Test Project
- **Location**: `src/Nexo.Tests.GeospatialUnit/`
- **Framework**: xUnit + FluentAssertions + Moq
- **Total Tests**: 30+ unit tests

### Test Categories

#### 1. Service Layer Tests
Tests for API service classes that orchestrate business logic:

- **GeoTerrainServiceTests** (7 tests)
  - Job creation and ID generation
  - Status transitions
  - Error handling
  - Webhook integration
  - Output path retrieval

- **GeoVectorServiceTests** (5 tests)
  - Feature extraction (buildings, roads, water)
  - Unsupported feature handling
  - Job management

- **WorldServiceTests** (4 tests)
  - World bundle generation
  - Validation logic
  - Directory checks

#### 2. Repository Tests
Tests for data persistence layer:

- **SqliteJobRepositoryTests** (10 tests)
  - CRUD operations
  - Concurrent access
  - Data persistence
  - Cleanup operations
  - Query operations

#### 3. Validation Tests
Tests for data integrity and quality validation:

- **ValidationTests** (8 tests)
  - Checksum computation
  - Corruption detection
  - Projection validation
  - Mesh quality metrics

#### 4. Model Tests
Tests for API models and DTOs:

- **ApiModelsTests** (6 tests)
  - Request validation
  - Model initialization
  - Record mutations

#### 5. Infrastructure Tests
Tests for background services:

- **JobCleanupServiceTests** (3 tests)
  - Service lifecycle
  - Cleanup execution

## Running Tests

```bash
# Run all tests
dotnet test src/Nexo.Tests.GeospatialUnit/Nexo.Tests.GeospatialUnit.csproj

# Run with coverage
dotnet test src/Nexo.Tests.GeospatialUnit/Nexo.Tests.GeospatialUnit.csproj --collect:"XPlat Code Coverage"

# Run specific test
dotnet test src/Nexo.Tests.GeospatialUnit/Nexo.Tests.GeospatialUnit.csproj --filter "FullyQualifiedName~GeoTerrainService"
```

## Test Patterns

### Mocking Strategy
- **CLI Commands**: Fully mocked to avoid actual execution
- **Repositories**: Real SQLite for integration testing
- **Loggers**: Mocked to reduce noise
- **Webhooks**: Mocked to avoid network calls

### Test Isolation
- Each test is independent
- Temporary directories/files created per test
- Cleanup in `Dispose()` methods
- No shared state between tests

### Assertion Style
Uses FluentAssertions for readable assertions:
```csharp
result.Should().NotBeNull();
result.Status.Should().Be("completed");
_mockRepository.Verify(r => r.CreateJobAsync(...), Times.Once);
```

## Coverage Summary

| Component | Coverage | Tests |
|-----------|----------|-------|
| Services | 80%+ | 16 tests |
| Repository | 90%+ | 10 tests |
| Validation | 85%+ | 8 tests |
| Models | 70%+ | 6 tests |
| Infrastructure | 75%+ | 3 tests |

## Key Test Scenarios

### Service Tests
- ✅ Job creation and ID generation
- ✅ Status transitions (pending → processing → completed)
- ✅ Error handling and failure recovery
- ✅ Webhook notifications
- ✅ Output path retrieval

### Repository Tests
- ✅ Job CRUD operations
- ✅ Concurrent access handling
- ✅ Data persistence across operations
- ✅ Old job cleanup
- ✅ Query and filtering

### Validation Tests
- ✅ Checksum validation
- ✅ Data corruption detection
- ✅ Projection parameter validation
- ✅ Mesh quality metrics

## Best Practices

1. **AAA Pattern**: All tests follow Arrange-Act-Assert
2. **Descriptive Names**: Test names clearly describe what they test
3. **Single Responsibility**: Each test verifies one behavior
4. **Fast Execution**: All tests complete in < 5 seconds
5. **No External Dependencies**: Tests don't require network or external services

## Integration with CI/CD

These tests are designed to run:
- On every commit (fast feedback)
- In pull request validation
- As part of build pipeline
- Before releases

## Future Enhancements

- Property-based testing for validation logic
- Performance benchmarks
- Mutation testing
- Expanded edge case coverage
- Better async testing patterns
