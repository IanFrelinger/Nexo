using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ashlar.Core.Application.Common.Models;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Ports;
using Ashlar.Infrastructure.Testing;
using Ashlar.Tests.Application.Helpers;

namespace Ashlar.Tests.Infrastructure.Tests.Testing;

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
