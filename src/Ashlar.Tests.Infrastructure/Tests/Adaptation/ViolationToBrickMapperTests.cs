using FluentAssertions;
using Ashlar.Core.Application.Analysis.Models;
using Ashlar.Core.Domain.Values;
using Ashlar.Infrastructure.Adaptation;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Adaptation;

/// <summary>Tests for violation to brick mapper.</summary>
[Trait("Category", "Adaptation")]
public sealed class ViolationToBrickMapperTests
{
    [Fact]
    public void GetBrickIdForFile_ObservationContextBrick_ReturnsObservationContext()
    {
        var brickId = ViolationToBrickMapper.GetBrickIdForFile("/path/to/ObservationContextBrick.cs");
        brickId.Should().Be("observation.context");
    }

    [Fact]
    public void GetBrickIdForFile_OWASPScannerBrick_ReturnsOwaspScanner()
    {
        var brickId = ViolationToBrickMapper.GetBrickIdForFile("/path/to/OWASPScannerBrick.cs");
        brickId.Should().Be("owasp-scanner");
    }

    [Fact]
    public void GetBrickIdForFile_UnknownFile_ReturnsNull()
    {
        var brickId = ViolationToBrickMapper.GetBrickIdForFile("/path/to/SomeOtherClass.cs");
        brickId.Should().BeNull();
    }

    [Fact]
    public void ToFailureType_ReturnsRule()
    {
        var violation = new Violation
        {
            Rule = "EmptyCatch",
            Message = "Empty catch block",
            FilePath = "test.cs",
            Severity = RiskLevel.Medium,
        };

        var failureType = ViolationToBrickMapper.ToFailureType(violation);
        failureType.Should().Be("EmptyCatch");
    }
}
