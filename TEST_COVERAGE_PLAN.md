# 100% Test Coverage Plan for Nexo - Cursor Agent Edition

## Overview

This document provides a structured, AI-agent-friendly plan to achieve 100% test coverage for the Nexo CLI application. Each task is designed to be executed by Cursor's coding agent with clear instructions, code patterns, and acceptance criteria.

## Current Test Coverage Status

### ✅ Already Tested
- **Domain Layer:**
  - `DomainValueObjectsTests` - RiskLevel enum
  - `DomainExceptionsTests` - Exception classes

- **Application Layer:**
  - `AnalysisHandlerTests` - AnalyzeCodeHandler (basic)
  - `ValidationHandlerTests` - RunValidationHandler (basic)

- **Infrastructure Layer:**
  - `AnalysisServiceAdapterTests` - Basic smoke test

- **CLI Layer:**
  - `CLICommandTests` - Basic smoke test

### ❌ Not Yet Tested (Priority Order)

---

## Sprint 0: Test Infrastructure Setup

**Duration:** 1-2 days  
**Goal:** Establish foundation for all testing work

### Task 0.1: Verify Test Projects Build
**File:** `src/Nexo.Tests.Domain/Nexo.Tests.Domain.csproj` and others

**Instructions:**
1. Run `dotnet build` on all test projects
2. Fix any compilation errors
3. Ensure all test projects reference correct dependencies

**Command:**
```bash
dotnet build src/Nexo.Tests.Domain/Nexo.Tests.Domain.csproj
dotnet build src/Nexo.Tests.Application/Nexo.Tests.Application.csproj
dotnet build src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj
dotnet build src/Nexo.Tests.CLI/Nexo.Tests.CLI.csproj
```

**Acceptance Criteria:**
- All test projects build successfully
- No compilation errors
- All dependencies resolved

### Task 0.2: Verify Test Discovery
**File:** `src/Nexo.Infrastructure/Testing/TestRunnerAdapter.cs`

**Instructions:**
1. Run `nexo test` command
2. Verify existing tests are discovered
3. Verify tests execute successfully

**Command:**
```bash
dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj -- test
```

**Acceptance Criteria:**
- Test discovery finds all existing tests
- Tests execute without errors
- Test output is readable

### Task 0.3: Create Test Helpers
**File:** `src/Nexo.Tests.Application/Helpers/TestHelpers.cs` (new file)

**Instructions:**
Create a test helpers file with common utilities:

```csharp
namespace Nexo.Tests.Application.Helpers;

public static class TestHelpers
{
    public static DirectoryInfo CreateTempDirectory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        return Directory.CreateDirectory(tempPath);
    }

    public static void CleanupTempDirectory(DirectoryInfo dir)
    {
        if (dir.Exists)
        {
            Directory.Delete(dir.FullName, true);
        }
    }

    public static FileInfo CreateTempAssemblyFile(DirectoryInfo dir, string name = "test.dll")
    {
        var filePath = Path.Combine(dir.FullName, name);
        File.WriteAllText(filePath, "dummy assembly content");
        return new FileInfo(filePath);
    }
}
```

**Acceptance Criteria:**
- Test helpers file created
- Common utilities available
- Helpers are reusable

---

## Sprint 1: Domain Layer Foundation

**Duration:** 5 days  
**Goal:** Achieve 100% test coverage for Domain layer  
**Target:** ~15-20 test methods

### Task 1.1: Complete Value Object Tests
**File:** `src/Nexo.Tests.Domain/Tests/ValueObjectsTests.cs` (new file)

**Instructions:**
1. Read all value objects from `src/Nexo.Core.Domain/Values/`
2. Create comprehensive tests for each value object
3. Test enum values, parsing, comparison, validation

**Pattern:**
```csharp
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Values;

namespace Nexo.Tests.Domain.Tests;

public class ValueObjectsTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test RiskLevel enum
            AssertTrue(Enum.IsDefined(typeof(RiskLevel), RiskLevel.Low));
            AssertTrue(Enum.IsDefined(typeof(RiskLevel), RiskLevel.Medium));
            AssertTrue(Enum.IsDefined(typeof(RiskLevel), RiskLevel.High));
            AssertTrue(Enum.IsDefined(typeof(RiskLevel), RiskLevel.Critical));
            
            // Test enum comparison
            AssertTrue(RiskLevel.Critical > RiskLevel.High);
            AssertTrue(RiskLevel.High > RiskLevel.Medium);
            AssertTrue(RiskLevel.Medium > RiskLevel.Low);
            
            // Add tests for other value objects discovered in Values/ directory
            
            return Task.FromResult(new TestResult
            {
                TestName = nameof(ValueObjectsTests),
                Category = "Domain",
                Passed = true,
                Message = "All value object tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                TestName = nameof(ValueObjectsTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}
```

**Steps:**
1. List all files in `src/Nexo.Core.Domain/Values/`
2. For each value object, create test cases:
   - Initialization
   - Validation
   - Equality/comparison
   - Immutability
3. Run tests: `nexo test --filter "ValueObjects"`

**Acceptance Criteria:**
- All value objects have tests
- All enum values tested
- Comparison logic tested
- Coverage shows 100% for Values/ directory

### Task 1.2: Complete Exception Tests
**File:** `src/Nexo.Tests.Domain/Tests/DomainExceptionsComprehensiveTests.cs` (new file)

**Instructions:**
1. Read all exception classes from `src/Nexo.Core.Domain/Exceptions/`
2. Test all constructors for each exception
3. Test error code assignment
4. Test suggestion property

**Pattern:**
```csharp
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Tests.Domain.Tests;

public class DomainExceptionsComprehensiveTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test AnalysisException - all constructors
            var ex1 = new AnalysisException("Message");
            AssertEqual("Message", ex1.Message);
            
            var ex2 = new AnalysisException("Message", ErrorCodes.AnalysisUnauthorizedAccess);
            AssertEqual("Message", ex2.Message);
            AssertEqual(ErrorCodes.AnalysisUnauthorizedAccess, ex2.ErrorCode);
            
            var ex3 = new AnalysisException("Message", new Exception("Inner"));
            AssertEqual("Message", ex3.Message);
            AssertNotNull(ex3.InnerException);
            
            var ex4 = new AnalysisException("Message", ErrorCodes.AnalysisUnauthorizedAccess, new Exception("Inner"), "Suggestion");
            AssertEqual("Message", ex4.Message);
            AssertEqual(ErrorCodes.AnalysisUnauthorizedAccess, ex4.ErrorCode);
            AssertEqual("Suggestion", ex4.Suggestion);
            
            // Repeat for ValidationException, AgentExecutionException, ConfigurationException
            
            return Task.FromResult(new TestResult
            {
                TestName = nameof(DomainExceptionsComprehensiveTests),
                Category = "Domain",
                Passed = true,
                Message = "All exception tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                TestName = nameof(DomainExceptionsComprehensiveTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}
```

**Steps:**
1. List all exception classes
2. For each exception, test all constructors
3. Verify error code assignment
4. Verify suggestion property
5. Run tests: `nexo test --filter "Exceptions"`

**Acceptance Criteria:**
- All exceptions have tests
- All constructors tested
- Error codes verified
- Suggestions verified
- Coverage shows 100% for Exceptions/ directory

### Task 1.3: Complete Error Codes Tests
**File:** `src/Nexo.Tests.Domain/Tests/ErrorCodesTests.cs` (new file)

**Instructions:**
1. Read `src/Nexo.Core.Domain/Exceptions/ErrorCodes.cs`
2. Verify all constants are defined
3. Test format consistency
4. Test uniqueness

**Pattern:**
```csharp
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Tests.Domain.Tests;

public class ErrorCodesTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify all error codes are defined
            AssertNotNull(ErrorCodes.AnalysisPathNotFound);
            AssertNotNull(ErrorCodes.AnalysisUnauthorizedAccess);
            // ... test all error codes
            
            // Verify format consistency (e.g., all start with category prefix)
            AssertTrue(ErrorCodes.AnalysisPathNotFound.StartsWith("ANALYSIS_"));
            AssertTrue(ErrorCodes.ValidationNoTestProjects.StartsWith("VALIDATION_"));
            
            // Verify uniqueness
            var allCodes = new[]
            {
                ErrorCodes.AnalysisPathNotFound,
                ErrorCodes.AnalysisUnauthorizedAccess,
                // ... all error codes
            };
            AssertEqual(allCodes.Length, allCodes.Distinct().Count());
            
            return Task.FromResult(new TestResult
            {
                TestName = nameof(ErrorCodesTests),
                Category = "Domain",
                Passed = true,
                Message = "All error code tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                TestName = nameof(ErrorCodesTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}
```

**Acceptance Criteria:**
- All error codes verified
- Format consistency verified
- Uniqueness verified
- Coverage shows 100% for ErrorCodes class

---

## Sprint 2: Application Layer - Handlers & Validators

**Duration:** 5 days  
**Goal:** Achieve 100% test coverage for Application layer handlers and validators  
**Target:** ~40-50 test methods

### Task 2.1: Comprehensive AnalyzeCodeHandler Tests
**File:** `src/Nexo.Tests.Application/Tests/Handlers/AnalyzeCodeHandlerComprehensiveTests.cs` (new file)

**Instructions:**
1. Read `src/Nexo.Core.Application/Analysis/UseCases/AnalyzeCode/AnalyzeCodeHandler.cs`
2. Create test class with Moq mocks
3. Test all scenarios: success, failures, edge cases, cancellation

**Pattern:**
```csharp
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Tests.Application.Tests.Handlers;

public class AnalyzeCodeHandlerComprehensiveTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test 1: Successful analysis with no violations
            var mockService = new Mock<IAnalysisService>();
            var mockLogger = new Mock<ILogger<AnalyzeCodeHandler>>();
            var handler = new AnalyzeCodeHandler(mockService.Object, mockLogger.Object);
            
            var expectedResult = new AnalysisResult
            {
                HasViolations = false,
                Violations = Array.Empty<Violation>(),
                TotalViolations = 0
            };
            
            mockService
                .Setup(s => s.AnalyzeAsync(It.IsAny<DirectoryInfo>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);
            
            var command = new AnalyzeCodeCommand(new DirectoryInfo(Path.GetTempPath()));
            var result = await handler.Handle(command, cancellationToken);
            
            AssertNotNull(result);
            AssertFalse(result.HasViolations);
            AssertEqual(0, result.TotalViolations);
            
            // Test 2: Analysis with violations
            // Test 3: UnauthorizedAccessException handling
            // Test 4: Cancellation token propagation
            // Test 5: Progress reporting
            // Test 6: Metrics collection
            
            return new TestResult
            {
                TestName = nameof(AnalyzeCodeHandlerComprehensiveTests),
                Category = "Application",
                Passed = true,
                Message = "All AnalyzeCodeHandler tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(AnalyzeCodeHandlerComprehensiveTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }
}
```

**Steps:**
1. Create test file with pattern above
2. Add test cases for:
   - Success scenarios (with/without violations)
   - Exception handling (UnauthorizedAccessException, AnalysisException)
   - Cancellation token
   - Progress reporting
   - Metrics collection
3. Run tests: `nexo test --filter "AnalyzeCodeHandler"`

**Acceptance Criteria:**
- All handler scenarios tested
- Mocks properly configured
- Exception handling verified
- Coverage shows 100% for AnalyzeCodeHandler

### Task 2.2: Comprehensive RunValidationHandler Tests
**File:** `src/Nexo.Tests.Application/Tests/Handlers/RunValidationHandlerComprehensiveTests.cs` (new file)

**Instructions:**
Similar to Task 2.1, but for RunValidationHandler. Test:
- With filter
- Without filter
- No test projects
- Failed tests
- Passed tests
- Cancellation
- Metrics
- Progress reporting

**Acceptance Criteria:**
- All scenarios tested
- Coverage shows 100% for RunValidationHandler

### Task 2.3: RunAgentHandler Tests
**File:** `src/Nexo.Tests.Application/Tests/Handlers/RunAgentHandlerTests.cs` (new file)

**Instructions:**
Test RunAgentHandler with:
- Valid agent name
- Invalid agent name
- With input file
- Without input file
- Timeout exception
- Agent execution exception
- Duration tracking
- Metrics collection

**Acceptance Criteria:**
- All scenarios tested
- Coverage shows 100% for RunAgentHandler

### Task 2.4: Validator Tests
**Files:**
- `src/Nexo.Tests.Application/Tests/Validators/AnalyzeCodeValidatorTests.cs`
- `src/Nexo.Tests.Application/Tests/Validators/RunValidationValidatorTests.cs`
- `src/Nexo.Tests.Application/Tests/Validators/RunAgentValidatorTests.cs`

**Instructions:**
For each validator:
1. Test all validation rules
2. Test error messages
3. Test edge cases (null, empty, whitespace)

**Pattern:**
```csharp
using FluentValidation.TestHelper;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;

namespace Nexo.Tests.Application.Tests.Validators;

public class AnalyzeCodeValidatorTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var validator = new AnalyzeCodeValidator();
            
            // Test null path
            var command1 = new AnalyzeCodeCommand(null!);
            var result1 = validator.TestValidate(command1);
            result1.ShouldHaveValidationErrorFor(x => x.Path);
            
            // Test non-existent path
            var command2 = new AnalyzeCodeCommand(new DirectoryInfo("nonexistent"));
            var result2 = validator.TestValidate(command2);
            result2.ShouldHaveValidationErrorFor(x => x.Path);
            
            // Test valid path
            var command3 = new AnalyzeCodeCommand(new DirectoryInfo(Path.GetTempPath()));
            var result3 = validator.TestValidate(command3);
            result3.ShouldNotHaveValidationErrorFor(x => x.Path);
            
            return Task.FromResult(new TestResult
            {
                TestName = nameof(AnalyzeCodeValidatorTests),
                Category = "Application",
                Passed = true,
                Message = "All validator tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                TestName = nameof(AnalyzeCodeValidatorTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}
```

**Acceptance Criteria:**
- All validators tested
- All validation rules verified
- Error messages verified
- Coverage shows 100% for validators

---

## Sprint 3: Application Layer - Models

**Duration:** 3 days  
**Goal:** Complete Application layer test coverage  
**Target:** ~15-20 test methods

### Task 3.1: Model Tests
**Files:**
- `src/Nexo.Tests.Application/Tests/Models/AnalysisResultTests.cs`
- `src/Nexo.Tests.Application/Tests/Models/ValidationResultTests.cs`
- `src/Nexo.Tests.Application/Tests/Models/AgentExecutionResultTests.cs`
- `src/Nexo.Tests.Application/Tests/Models/ProgressReportTests.cs`
- `src/Nexo.Tests.Application/Tests/Models/TestResultTests.cs`

**Instructions:**
For each model (record type):
1. Test record equality
2. Test record immutability
3. Test initialization
4. Test with/without optional properties

**Pattern:**
```csharp
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Models;

public class AnalysisResultTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test record equality
            var result1 = new AnalysisResult
            {
                HasViolations = false,
                Violations = Array.Empty<Violation>(),
                TotalViolations = 0
            };
            
            var result2 = new AnalysisResult
            {
                HasViolations = false,
                Violations = Array.Empty<Violation>(),
                TotalViolations = 0
            };
            
            AssertEqual(result1, result2);
            
            // Test immutability (records are immutable by default)
            // Test initialization with violations
            // Test initialization without violations
            
            return Task.FromResult(new TestResult
            {
                TestName = nameof(AnalysisResultTests),
                Category = "Application",
                Passed = true,
                Message = "All model tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                TestName = nameof(AnalysisResultTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}
```

**Acceptance Criteria:**
- All models tested
- Record equality verified
- Immutability verified
- Coverage shows 100% for models

---

## Sprint 4: Infrastructure Layer - Analysis & Validation

**Duration:** 5 days  
**Goal:** Achieve 100% test coverage for Analysis and Validation infrastructure  
**Target:** ~50-60 test methods

### Task 4.1: AnalysisServiceAdapter Comprehensive Tests
**File:** `src/Nexo.Tests.Infrastructure/Tests/Analysis/AnalysisServiceAdapterComprehensiveTests.cs` (new file)

**Instructions:**
1. Create temporary directories with test assemblies
2. Test with real file system operations
3. Test all scenarios: empty directory, no assemblies, errors, cancellation

**Pattern:**
```csharp
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Analysis.Adapters;
using Nexo.Infrastructure.Analysis.Rules;
using Nexo.Tests.Application.Helpers;

namespace Nexo.Tests.Infrastructure.Tests.Analysis;

public class AnalysisServiceAdapterComprehensiveTests : UnitTestBase
{
    private DirectoryInfo? _tempDir;

    public override async Task SetupAsync(CancellationToken cancellationToken = default)
    {
        _tempDir = TestHelpers.CreateTempDirectory();
    }

    public override async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        if (_tempDir != null)
        {
            TestHelpers.CleanupTempDirectory(_tempDir);
        }
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var mockLogger = new Mock<ILogger<AnalysisServiceAdapter>>();
            var mockRuleEngine = new Mock<AnalysisRuleEngine>(Array.Empty<IAnalysisRule>(), mockLogger.Object);
            var adapter = new AnalysisServiceAdapter(mockLogger.Object, mockRuleEngine.Object);
            
            // Test with empty directory
            var result1 = await adapter.AnalyzeAsync(_tempDir!, null, cancellationToken);
            AssertNotNull(result1);
            
            // Test with assembly files
            TestHelpers.CreateTempAssemblyFile(_tempDir!, "test.dll");
            var result2 = await adapter.AnalyzeAsync(_tempDir!, null, cancellationToken);
            AssertNotNull(result2);
            
            // Test cancellation
            var cts = new CancellationTokenSource();
            cts.Cancel();
            await AssertThrowsAsync<OperationCanceledException>(() => 
                adapter.AnalyzeAsync(_tempDir!, null, cts.Token));
            
            return new TestResult
            {
                TestName = nameof(AnalysisServiceAdapterComprehensiveTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All AnalysisServiceAdapter tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(AnalysisServiceAdapterComprehensiveTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }
}
```

**Steps:**
1. Use TestHelpers for temp directories
2. Test all scenarios
3. Use real file system operations
4. Test error handling

**Acceptance Criteria:**
- All scenarios tested
- Real file operations work
- Error handling verified
- Coverage shows 100% for AnalysisServiceAdapter

### Task 4.2-4.7: Other Infrastructure Tests
Follow similar patterns for:
- CachedAnalysisServiceAdapter
- AnalysisRuleEngine
- SecurityAnalysisRule
- CodeQualityRule
- ValidationServiceAdapter
- CachedValidationServiceAdapter
- TrxTestResultParser

**Acceptance Criteria:**
- All adapters tested
- All rules tested
- Coverage shows 100% for infrastructure

---

## Sprint 5-10: Continue Pattern

Continue the same pattern for remaining sprints:
- Clear task definitions
- Code patterns provided
- Step-by-step instructions
- Acceptance criteria
- Test commands to run

---

## Quick Reference: Test Execution Commands

```bash
# Run all tests
nexo test

# Run tests by category
nexo test --filter "Domain"
nexo test --filter "Application"
nexo test --filter "Infrastructure"
nexo test --filter "CLI"

# Run specific test
nexo test --filter "AnalyzeCodeHandler"

# Run with verbose output
nexo test --verbose

# Run with JSON output
nexo test --format-json
```

## Agent Instructions Summary

For Cursor coding agent:
1. Read the task definition
2. Follow the code pattern provided
3. Implement all test cases listed
4. Run the test command to verify
5. Check acceptance criteria
6. Move to next task

Each task is self-contained with:
- Clear file paths
- Code patterns to follow
- Step-by-step instructions
- Acceptance criteria
- Test commands
