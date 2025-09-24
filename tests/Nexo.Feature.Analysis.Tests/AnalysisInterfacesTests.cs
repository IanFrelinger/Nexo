using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Analysis.Tests.Commands;

namespace Nexo.Feature.Analysis.Tests;

public class AnalysisInterfacesTests
{
    [Fact(Timeout = 10000)]
    public void Analysis_Interfaces_WorkCorrectly()
    {
        var command = new AnalysisValidationCommand(NullLogger<AnalysisValidationCommand>.Instance);
        var result = command.ValidateAnalysisInterfaces(timeoutMs: 5000);
        Assert.True(result, "Analysis interfaces should work correctly");
    }
}
