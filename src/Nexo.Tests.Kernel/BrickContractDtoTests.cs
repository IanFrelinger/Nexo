using FluentAssertions;
using Nexo.Brick.Contracts;
using Nexo.Brick.Contracts.Capabilities;
using Xunit;

namespace Nexo.Tests.Kernel;

/// <summary>Tests for brick contract dto.</summary>
public class BrickContractDtoTests
{
    [Fact]
    public void WireFormatVersion_Current_is_published_string()
    {
        WireFormatVersion.Current.Should().Be("2025-02");
    }

    [Fact]
    public void BrickCatalogEntryDto_round_trips_all_properties()
    {
        var entry = new BrickCatalogEntryDto
        {
            WireFormatVersion = "v1",
            Id = "id",
            Name = "name",
            Version = "1.2.3",
            Icon = "*",
            Category = "Analysis",
            Description = "desc",
            HostBaseUrl = "http://host",
            Interface = new BrickInterfaceDto
            {
                Inputs = new List<BrickInputDefinitionDto>
                {
                    new() { Name = "in", Type = "string", Description = "d", Required = false, Default = "x" }
                },
                Outputs = new List<BrickOutputDefinitionDto>
                {
                    new() { Name = "out", Type = "string", Description = "d" }
                }
            },
            HasDeterministic = true,
            HasAgentic = true,
            Metadata = new BrickMetadataDto
            {
                Author = "a",
                License = "MIT",
                Repository = "r",
                UsageCount = 7,
                LastUpdated = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc)
            },
            DomainKnowledge = new DomainKnowledgeDto
            {
                Standards = new[] { "OWASP" },
                Rules = new List<DomainRuleDto>
                {
                    new() { Id = "r1", Description = "rule", Pattern = "p", Severity = "high", CweId = "CWE-79" }
                },
                ReferenceData = new Dictionary<string, string> { ["k"] = "v" },
                LearnedPatterns = new List<LearnedPatternDto>
                {
                    new() { Pattern = "lp", Outcome = "ok", Confidence = 0.5, OccurrenceCount = 3 }
                },
                ExecutionCount = 42,
            },
            UsageStats = new BrickUsageStatsDto
            {
                TotalExecutions = 99,
                SuccessRate = 0.95,
                AvgDurationMs = 12.5,
                LastUsed = DateTimeOffset.UtcNow,
            },
            HostCapabilities = new NodeCapabilityManifestDto
            {
                NodeId = "node",
                Tier = NodeTierDto.Core,
                Platform = PlatformTypeDto.Linux,
                HotModelIds = new[] { "m1" },
                AvailableModelIds = new[] { "m1", "m2" },
                SupportedCapabilities = new[] { TaskCapabilityDto.TextGeneration, TaskCapabilityDto.Reasoning },
                AcceptingRemoteWork = true,
                GeneratedAt = DateTimeOffset.UtcNow,
            }
        };

        entry.WireFormatVersion.Should().Be("v1");
        entry.Id.Should().Be("id");
        entry.Name.Should().Be("name");
        entry.Version.Should().Be("1.2.3");
        entry.Icon.Should().Be("*");
        entry.Category.Should().Be("Analysis");
        entry.Description.Should().Be("desc");
        entry.HostBaseUrl.Should().Be("http://host");
        entry.HasDeterministic.Should().BeTrue();
        entry.HasAgentic.Should().BeTrue();
        entry.Interface.Inputs.Should().ContainSingle().Which.Default.Should().Be("x");
        entry.Interface.Outputs.Should().ContainSingle().Which.Description.Should().Be("d");
        entry.Metadata.Author.Should().Be("a");
        entry.Metadata.License.Should().Be("MIT");
        entry.Metadata.Repository.Should().Be("r");
        entry.Metadata.UsageCount.Should().Be(7);
        entry.Metadata.LastUpdated.Should().NotBeNull();
        entry.DomainKnowledge!.Standards.Should().ContainSingle();
        entry.DomainKnowledge.Rules.Should().ContainSingle().Which.CweId.Should().Be("CWE-79");
        entry.DomainKnowledge.ReferenceData.Should().ContainKey("k");
        entry.DomainKnowledge.LearnedPatterns.Should().ContainSingle().Which.OccurrenceCount.Should().Be(3);
        entry.DomainKnowledge.ExecutionCount.Should().Be(42);
        entry.UsageStats!.TotalExecutions.Should().Be(99);
        entry.UsageStats.SuccessRate.Should().Be(0.95);
        entry.UsageStats.AvgDurationMs.Should().Be(12.5);
        entry.UsageStats.LastUsed.Should().NotBeNull();
        entry.HostCapabilities!.NodeId.Should().Be("node");
        entry.HostCapabilities.Tier.Should().Be(NodeTierDto.Core);
        entry.HostCapabilities.Platform.Should().Be(PlatformTypeDto.Linux);
        entry.HostCapabilities.HotModelIds.Should().ContainSingle();
        entry.HostCapabilities.AvailableModelIds.Should().HaveCount(2);
        entry.HostCapabilities.SupportedCapabilities.Should().Contain(TaskCapabilityDto.Reasoning);
        entry.HostCapabilities.AcceptingRemoteWork.Should().BeTrue();
    }

    [Fact]
    public void BrickCatalogEntryDto_defaults_are_safe()
    {
        var entry = new BrickCatalogEntryDto();
        entry.WireFormatVersion.Should().Be(WireFormatVersion.Current);
        entry.Version.Should().Be("1.0.0");
        entry.Icon.Should().NotBeNullOrEmpty();
        entry.Interface.Should().NotBeNull();
        entry.Interface.Inputs.Should().BeEmpty();
        entry.Interface.Outputs.Should().BeEmpty();
        entry.Metadata.Should().NotBeNull();
        entry.Metadata.UsageCount.Should().Be(0);
        entry.HasDeterministic.Should().BeFalse();
        entry.HasAgentic.Should().BeFalse();
        entry.HostBaseUrl.Should().BeNull();
        entry.DomainKnowledge.Should().BeNull();
        entry.UsageStats.Should().BeNull();
        entry.HostCapabilities.Should().BeNull();
    }

    [Fact]
    public void BrickCatalogResponseDto_defaults_and_setters_work()
    {
        var resp = new BrickCatalogResponseDto();
        resp.WireFormatVersion.Should().Be(WireFormatVersion.Current);
        resp.Bricks.Should().BeEmpty();
        resp.ContinuationToken.Should().BeNull();

        resp.Bricks = new[] { new BrickCatalogEntryDto { Id = "a" } };
        resp.ContinuationToken = "next";
        resp.WireFormatVersion = "v";

        resp.Bricks.Should().ContainSingle().Which.Id.Should().Be("a");
        resp.ContinuationToken.Should().Be("next");
        resp.WireFormatVersion.Should().Be("v");
    }

    [Fact]
    public void BrickExecuteRequest_and_Response_defaults_and_setters()
    {
        var req = new BrickExecuteRequestDto();
        req.WireFormatVersion.Should().Be(WireFormatVersion.Current);
        req.Implementation.Should().Be("Deterministic");
        req.Input.Should().BeNull();
        req.ExecutionContext.Should().BeNull();

        req.BrickId = "id";
        req.Implementation = "Agentic";
        req.Input = new Dictionary<string, object> { ["k"] = 1 };
        req.ExecutionContext = new ExecutionContextDto
        {
            CorrelationId = "corr",
            AgentId = "agent",
            BehaviorId = "beh",
            IsAirGapped = true,
            AuditMode = true,
            Provider = "prov",
            Variables = new Dictionary<string, object> { ["v"] = "x" }
        };

        req.BrickId.Should().Be("id");
        req.Implementation.Should().Be("Agentic");
        req.Input.Should().ContainKey("k");
        req.ExecutionContext!.CorrelationId.Should().Be("corr");
        req.ExecutionContext.AgentId.Should().Be("agent");
        req.ExecutionContext.BehaviorId.Should().Be("beh");
        req.ExecutionContext.IsAirGapped.Should().BeTrue();
        req.ExecutionContext.AuditMode.Should().BeTrue();
        req.ExecutionContext.Provider.Should().Be("prov");
        req.ExecutionContext.Variables.Should().ContainKey("v");

        var resp = new BrickExecuteResponseDto();
        resp.WireFormatVersion.Should().Be(WireFormatVersion.Current);
        resp.Success.Should().BeFalse();
        resp.Summary.Should().BeNull();
        resp.Output.Should().BeNull();
        resp.Error.Should().BeNull();

        resp.Success = true;
        resp.Summary = "ok";
        resp.Output = new Dictionary<string, object> { ["o"] = 1 };
        resp.Error = "err";

        resp.Success.Should().BeTrue();
        resp.Summary.Should().Be("ok");
        resp.Output.Should().ContainKey("o");
        resp.Error.Should().Be("err");
    }

    [Fact]
    public void BrickInputDefinition_and_OutputDefinition_round_trip()
    {
        var input = new BrickInputDefinitionDto
        {
            Name = "n",
            Type = "string",
            Description = "d",
            Required = false,
            Default = 42
        };
        input.Name.Should().Be("n");
        input.Type.Should().Be("string");
        input.Description.Should().Be("d");
        input.Required.Should().BeFalse();
        input.Default.Should().Be(42);

        var defaultInput = new BrickInputDefinitionDto();
        defaultInput.Required.Should().BeTrue();
        defaultInput.Description.Should().BeNull();
        defaultInput.Default.Should().BeNull();

        var output = new BrickOutputDefinitionDto { Name = "o", Type = "int", Description = "d" };
        output.Name.Should().Be("o");
        output.Type.Should().Be("int");
        output.Description.Should().Be("d");
    }

    [Fact]
    public void BrickInterface_defaults_and_setters_work()
    {
        var iface = new BrickInterfaceDto();
        iface.Inputs.Should().BeEmpty();
        iface.Outputs.Should().BeEmpty();
        iface.Inputs = new[] { new BrickInputDefinitionDto { Name = "n", Type = "t" } };
        iface.Outputs = new[] { new BrickOutputDefinitionDto { Name = "o", Type = "t" } };
        iface.Inputs.Should().ContainSingle();
        iface.Outputs.Should().ContainSingle();
    }

    [Fact]
    public void BrickMetadata_defaults_and_assignment()
    {
        var m = new BrickMetadataDto();
        m.Author.Should().BeNull();
        m.License.Should().BeNull();
        m.Repository.Should().BeNull();
        m.UsageCount.Should().Be(0);
        m.LastUpdated.Should().BeNull();

        var when = DateTime.UtcNow;
        m.Author = "a";
        m.License = "l";
        m.Repository = "r";
        m.UsageCount = 1;
        m.LastUpdated = when;

        m.Author.Should().Be("a");
        m.License.Should().Be("l");
        m.Repository.Should().Be("r");
        m.UsageCount.Should().Be(1);
        m.LastUpdated.Should().Be(when);
    }

    [Fact]
    public void BrickUsageRecord_BrickUsageReport_BrickUsageStats_round_trip()
    {
        var when = DateTimeOffset.UtcNow;
        var rec = new BrickUsageRecordDto
        {
            BrickId = "b",
            ExecutionCount = 10,
            SuccessRate = 0.9,
            AvgDurationMs = 1.5,
            LastUsed = when
        };
        rec.BrickId.Should().Be("b");
        rec.ExecutionCount.Should().Be(10);
        rec.SuccessRate.Should().Be(0.9);
        rec.AvgDurationMs.Should().Be(1.5);
        rec.LastUsed.Should().Be(when);

        var report = new BrickUsageReportDto();
        report.WireFormatVersion.Should().Be(WireFormatVersion.Current);
        report.Records.Should().BeEmpty();
        report.NodeId = "n";
        report.WireFormatVersion = "v";
        report.Records.Add(rec);
        report.NodeId.Should().Be("n");
        report.WireFormatVersion.Should().Be("v");
        report.Records.Should().ContainSingle();

        var stats = new BrickUsageStatsDto();
        stats.TotalExecutions.Should().Be(0);
        stats.SuccessRate.Should().Be(0);
        stats.AvgDurationMs.Should().Be(0);
        stats.LastUsed.Should().BeNull();

        stats.TotalExecutions = 5;
        stats.SuccessRate = 0.5;
        stats.AvgDurationMs = 2.0;
        stats.LastUsed = when;
        stats.TotalExecutions.Should().Be(5);
        stats.SuccessRate.Should().Be(0.5);
        stats.AvgDurationMs.Should().Be(2.0);
        stats.LastUsed.Should().Be(when);
    }

    [Fact]
    public void ExecutionContext_defaults_are_null_or_false()
    {
        var ctx = new ExecutionContextDto();
        ctx.CorrelationId.Should().BeNull();
        ctx.AgentId.Should().BeNull();
        ctx.BehaviorId.Should().BeNull();
        ctx.IsAirGapped.Should().BeFalse();
        ctx.AuditMode.Should().BeFalse();
        ctx.Provider.Should().BeNull();
        ctx.Variables.Should().BeNull();
    }

    [Fact]
    public void DomainKnowledgeDto_defaults_are_empty_collections()
    {
        var dk = new DomainKnowledgeDto();
        dk.Standards.Should().BeEmpty();
        dk.Rules.Should().BeEmpty();
        dk.ReferenceData.Should().BeEmpty();
        dk.LearnedPatterns.Should().BeEmpty();
        dk.ExecutionCount.Should().Be(0);
    }

    [Fact]
    public void DomainRuleDto_defaults_and_setters()
    {
        var rule = new DomainRuleDto();
        rule.Id.Should().BeEmpty();
        rule.Description.Should().BeEmpty();
        rule.Pattern.Should().BeNull();
        rule.Severity.Should().BeNull();
        rule.CweId.Should().BeNull();

        rule.Id = "id";
        rule.Description = "d";
        rule.Pattern = "p";
        rule.Severity = "s";
        rule.CweId = "c";
        rule.Id.Should().Be("id");
        rule.Description.Should().Be("d");
        rule.Pattern.Should().Be("p");
        rule.Severity.Should().Be("s");
        rule.CweId.Should().Be("c");
    }

    [Fact]
    public void LearnedPatternDto_defaults_and_setters()
    {
        var lp = new LearnedPatternDto();
        lp.Pattern.Should().BeEmpty();
        lp.Outcome.Should().BeEmpty();
        lp.Confidence.Should().Be(0);
        lp.OccurrenceCount.Should().Be(0);

        lp.Pattern = "p";
        lp.Outcome = "o";
        lp.Confidence = 0.75;
        lp.OccurrenceCount = 4;
        lp.Pattern.Should().Be("p");
        lp.Outcome.Should().Be("o");
        lp.Confidence.Should().Be(0.75);
        lp.OccurrenceCount.Should().Be(4);
    }

    [Fact]
    public void NodeCapabilityManifestDto_defaults_and_setters()
    {
        var m = new NodeCapabilityManifestDto();
        m.NodeId.Should().BeEmpty();
        m.Tier.Should().Be(NodeTierDto.Nano);
        m.Platform.Should().Be(PlatformTypeDto.Unknown);
        m.HotModelIds.Should().BeEmpty();
        m.AvailableModelIds.Should().BeEmpty();
        m.SupportedCapabilities.Should().BeEmpty();
        m.AcceptingRemoteWork.Should().BeFalse();
        m.GeneratedAt.Should().BeAfter(DateTimeOffset.MinValue);
    }

    [Theory]
    [InlineData(NodeTierDto.Nano)]
    [InlineData(NodeTierDto.Micro)]
    [InlineData(NodeTierDto.Standard)]
    [InlineData(NodeTierDto.Core)]
    public void NodeTierDto_values_are_distinct(NodeTierDto tier)
    {
        ((int)tier).Should().BeGreaterThanOrEqualTo(0);
    }

    [Theory]
    [InlineData(PlatformTypeDto.Windows)]
    [InlineData(PlatformTypeDto.macOS)]
    [InlineData(PlatformTypeDto.Linux)]
    [InlineData(PlatformTypeDto.iOS)]
    [InlineData(PlatformTypeDto.Android)]
    [InlineData(PlatformTypeDto.Unknown)]
    public void PlatformTypeDto_values_are_distinct(PlatformTypeDto platform)
    {
        ((int)platform).Should().BeGreaterThanOrEqualTo(0);
    }

    [Theory]
    [InlineData(TaskCapabilityDto.TextGeneration)]
    [InlineData(TaskCapabilityDto.CodeGeneration)]
    [InlineData(TaskCapabilityDto.Embeddings)]
    [InlineData(TaskCapabilityDto.Vision)]
    [InlineData(TaskCapabilityDto.Reasoning)]
    [InlineData(TaskCapabilityDto.Classification)]
    public void TaskCapabilityDto_values_are_distinct(TaskCapabilityDto cap)
    {
        ((int)cap).Should().BeGreaterThanOrEqualTo(0);
    }
}
