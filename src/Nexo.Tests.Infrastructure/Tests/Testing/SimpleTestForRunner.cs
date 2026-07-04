using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Ports;
using Nexo.Infrastructure.Testing;
using Nexo.Tests.Application.Helpers;

namespace Nexo.Tests.Infrastructure.Tests.Testing;

// Simple test class for testing TestRunnerAdapter
public class SimpleTestForRunner : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TestResult
        {
            Name = nameof(SimpleTestForRunner),
            Category = "TestRunner",
            Passed = true,
            Message = "Simple test for TestRunnerAdapter"
        });
    }
}
