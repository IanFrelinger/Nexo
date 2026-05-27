using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Bricks.Owasp.Security;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Xunit;
using ExecutionContext = Nexo.Infrastructure.Execution.ExecutionContext;

namespace Nexo.Tests.Infrastructure.Tests.Bricks;

public class OWASPScannerBrickXunitTests
{
    private static OWASPScannerBrick CreateBrick(Mock<IProviderFactory>? factory = null)
    {
        factory ??= new Mock<IProviderFactory>();
        var logger = new Mock<ILogger<OWASPScannerBrick>>();
        return new OWASPScannerBrick(factory.Object, logger.Object);
    }

    private static ExecutionContext Context(string provider = "mock") => new()
    {
        AgentId = "agent",
        BehaviorId = "behavior",
        IsAirGapped = false,
        AuditMode = false,
        Provider = provider,
        Variables = new Dictionary<string, object>(),
    };

    [Fact]
    public void SecurityFinding_and_ScanMetadata_records_round_trip()
    {
        var finding = new SecurityFinding("id", "SQL Injection", "Critical", "CWE-89", "desc");
        finding.Id.Should().Be("id");
        finding.Type.Should().Be("SQL Injection");
        finding.Severity.Should().Be("Critical");
        finding.CweId.Should().Be("CWE-89");
        finding.Description.Should().Be("desc");

        var meta = new ScanMetadata(5, 100, "csharp");
        meta.RulesApplied.Should().Be(5);
        meta.LinesScanned.Should().Be(100);
        meta.Language.Should().Be("csharp");
    }

    [Fact]
    public async Task Deterministic_scan_returns_findings_and_metadata()
    {
        var brick = CreateBrick();
        var input = new BrickInput();
        input.Set("code", "var x = \"<script>\" + user;");
        input.Set("language", "javascript");

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, Context());
        output.Get<List<SecurityFinding>>("findings").Should().NotBeEmpty();
        output.Get<ScanMetadata>("metadata").LinesScanned.Should().BeGreaterThan(0);
        output.Summary.Should().Contain("vulnerabilities");
    }

    [Fact]
    public async Task Agentic_scan_parses_llm_xss_and_sqli_mentions()
    {
        var factory = new Mock<IProviderFactory>();
        factory.Setup(f => f.ExecuteLLMAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("SQL Injection and XSS issues found");

        var brick = CreateBrick(factory);
        var input = new BrickInput();
        input.Set("code", "unsafe code");

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, Context("mock"));
        var findings = output.Get<List<SecurityFinding>>("findings");
        findings.Should().Contain(f => f.Type.Contains("SQL", StringComparison.OrdinalIgnoreCase));
        findings.Should().Contain(f => f.Type.Contains("Cross-Site", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Unsupported_implementation_throws()
    {
        var brick = CreateBrick();
        var input = new BrickInput();
        input.Set("code", "x");
        var act = () => brick.ExecuteAsync(input, (ImplementationType)999, Context());
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
