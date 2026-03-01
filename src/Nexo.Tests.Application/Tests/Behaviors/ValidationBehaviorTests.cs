using FluentValidation;
using MediatR;
using Nexo.Core.Application.Behaviors;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Behaviors;

public class ValidationBehaviorTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestHandleWithNoValidators();
            await TestHandleWithValidRequest();
            await TestHandleWithInvalidRequest();
            await TestHandleWithMultipleValidators();
            await TestHandleWithMultipleValidatorsOneFails();
            await TestHandleCallsNext();

            return new TestResult
            {
                Name = nameof(ValidationBehaviorTests),
                Category = "Application",
                Passed = true,
                Message = "All ValidationBehavior tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(ValidationBehaviorTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(ValidationBehaviorTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestHandleWithNoValidators()
    {
        var validators = Array.Empty<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest { Value = "test" };
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse { Result = "success" });
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        AssertTrue(nextCalled);
        AssertNotNull(result);
        AssertEqual("success", result.Result);
    }

    private async Task TestHandleWithValidRequest()
    {
        var validator = new TestRequestValidator();
        var validators = new[] { validator };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest { Value = "valid" };
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse { Result = "success" });
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        AssertTrue(nextCalled);
        AssertNotNull(result);
        AssertEqual("success", result.Result);
    }

    private async Task TestHandleWithInvalidRequest()
    {
        var validator = new TestRequestValidator();
        var validators = new[] { validator };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest { Value = "" }; // Invalid - empty value
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse { Result = "success" });
        };

        try
        {
            await behavior.Handle(request, next, CancellationToken.None);
            throw new AssertionException("Expected ValidationException to be thrown");
        }
        catch (FluentValidation.ValidationException ex)
        {
            AssertFalse(nextCalled);
            AssertTrue(ex.Errors.Count() > 0);
        }
    }

    private async Task TestHandleWithMultipleValidators()
    {
        var validator1 = new TestRequestValidator();
        var validator2 = new TestRequestValidator2();
        var validators = new IValidator<TestRequest>[] { validator1, validator2 };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest { Value = "valid", Number = 10 };
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse { Result = "success" });
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        AssertTrue(nextCalled);
        AssertNotNull(result);
        AssertEqual("success", result.Result);
    }

    private async Task TestHandleWithMultipleValidatorsOneFails()
    {
        var validator1 = new TestRequestValidator();
        var validator2 = new TestRequestValidator2();
        var validators = new IValidator<TestRequest>[] { validator1, validator2 };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest { Value = "valid", Number = -1 }; // Invalid - negative number
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse { Result = "success" });
        };

        try
        {
            await behavior.Handle(request, next, CancellationToken.None);
            throw new AssertionException("Expected ValidationException to be thrown");
        }
        catch (FluentValidation.ValidationException ex)
        {
            AssertFalse(nextCalled);
            AssertTrue(ex.Errors.Count() > 0);
            AssertTrue(ex.Errors.Any(e => e.PropertyName == "Number"));
        }
    }

    private async Task TestHandleCallsNext()
    {
        var validators = Array.Empty<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest { Value = "test" };
        var nextCallCount = 0;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCallCount++;
            return Task.FromResult(new TestResponse { Result = $"called-{nextCallCount}" });
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        AssertEqual(1, nextCallCount);
        AssertNotNull(result);
        AssertEqual("called-1", result.Result);
    }

    // Test request/response types
    private record TestRequest : IRequest<TestResponse>
    {
        public string Value { get; init; } = string.Empty;
        public int Number { get; init; }
    }

    private record TestResponse
    {
        public string Result { get; init; } = string.Empty;
    }

    // Test validators
    private class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Value)
                .NotEmpty()
                .WithMessage("Value cannot be empty");
        }
    }

    private class TestRequestValidator2 : AbstractValidator<TestRequest>
    {
        public TestRequestValidator2()
        {
            RuleFor(x => x.Number)
                .GreaterThan(0)
                .WithMessage("Number must be greater than 0");
        }
    }
}

