using FluentAssertions;
using Nexo.Contracts;
using Xunit;

namespace Nexo.Tests.Contracts;

public class ContractsDtoTests
{
    [Fact]
    public void AgentRequest_and_response_round_trip()
    {
        new AgentRequest("agent", "/in").AgentName.Should().Be("agent");
        new AgentResponse(true, "ok", new { x = 1 }).Success.Should().BeTrue();
    }

    [Fact]
    public void ValidationRequest_and_response_round_trip()
    {
        new ValidationRequest("cat=unit").Filter.Should().Be("cat=unit");
        new ValidationResponse(true, null, 10, 9, 1).FailedTests.Should().Be(1);
    }

    [Fact]
    public void OrchestrationRequest_and_response_round_trip()
    {
        var req = new OrchestrationRequest("build game", "{}", "agentic", "ollama", "L2", "us-east");
        req.PreferredRegion.Should().Be("us-east");
        new OrchestrationResponse(true, "done", null, 1, 0, null).Conflicts.Should().Be(1);
    }

    [Fact]
    public void StatusResponse_and_execution_records_round_trip()
    {
        new StatusResponse("prod", "ok", 5, 2, "pack", "1.0").ActivePackId.Should().Be("pack");
        new ExecutionBuildRequest("Dockerfile", "tag", ".", new Dictionary<string, string> { ["A"] = "B" })
            .BuildArgs!["A"].Should().Be("B");
        new ExecutionRunRequest("tag", new[] { "echo" }, null, null, "/work")
            .WorkingDirectory.Should().Be("/work");
        new ExecutionRunResponse(true, 0, "out", "err", "cid", 12.5).ExitCode.Should().Be(0);
    }

    [Fact]
    public void TrustStatusResponse_round_trip()
    {
        new TrustStatusResponse(false, "active", "pack-1", "2.0").ActivePolicyPackVersion.Should().Be("2.0");
    }

    [Fact]
    public void SmsIngressApprovalStoreKind_constants_exist()
    {
        SmsIngressApprovalStoreKind.Memory.Should().Be("Memory");
        SmsIngressApprovalStoreKind.DynamoDb.Should().Be("DynamoDb");
    }

    [Fact]
    public void SmsIngressExternalIds_builds_sid_or_hash_keys()
    {
        SmsIngressExternalIds.Build("+1", "tok", "SM123").Should().Be("sid:SM123");
        SmsIngressExternalIds.Build("+1", "tok", null).Should().Be("hash:+1:tok");
    }

    [Fact]
    public void Middleware_ingress_contracts_round_trip()
    {
        NexoIngressTransports.Http.Should().Be("http");
        new NexoIngressEnvelope("c1", NexoIngressTransports.WebSocket, "idem", "v1", "t1", "app")
            .TenantId.Should().Be("t1");
        new SmsInboundSimulationResponse(true, "tok", "ok", false).Accepted.Should().BeTrue();
        new IngressCatalogResponse(new[] { new IngressCatalogEntry("sms", "/sms", "sim") })
            .EntryPoints.Should().ContainSingle();
        new IngressContextResponse("c", "http", null, null, null, null).CorrelationId.Should().Be("c");
    }

    [Fact]
    public async Task UnsupportedSmsIngressApprovalStore_rejects_recording()
    {
        var store = new UnsupportedSmsIngressApprovalStore();
        var response = await store.TryRecordApprovalAsync("+1555", "token", "SM1", CancellationToken.None);

        response.Accepted.Should().BeFalse();
        response.Message.Should().Contain("not configured");
    }

    [Fact]
    public void SmsIngressDynamoDbOptions_has_defaults()
    {
        SmsIngressDynamoDbOptions.SectionPath.Should().Be("Nexo:SmsIngressDynamoDb");
        new SmsIngressDynamoDbOptions { TableName = "ingress-approvals" }.TableName
            .Should().Be("ingress-approvals");
    }

    [Fact]
    public void Remaining_api_and_ingress_contracts_round_trip()
    {
        new ExecutionBuildResponse(true, null, 12.5).DurationMs.Should().Be(12.5);
        new SmsInboundSimulationRequest("+1555", "yes", "SM1", "app").Body.Should().Be("yes");
        new OrchestrationRequest("goal", "{}", "agentic", "ollama", "L1", null).Request.Should().Be("goal");
        new StatusResponse("dev", "ok", 1, 0, null, null).Mode.Should().Be("dev");
    }

    [Fact]
    public void Orchestration_and_execution_contracts_expose_all_properties()
    {
        new OrchestrationResponse(false, "summary", new { ok = true }, 2, 1, "err")
            .ErrorCode.Should().Be("err");

        new AgentResponse(false, "failed", null).Message.Should().Be("failed");
        new ValidationResponse(false, "bad", 5, 3, 2).Passed.Should().BeFalse();

        new ExecutionRunRequest(
                "img",
                new[] { "echo", "hi" },
                new Dictionary<string, string> { ["K"] = "V" },
                new Dictionary<string, string> { ["/host"] = "/container" },
                "/work")
            .EnvironmentVariables!["K"].Should().Be("V");

        new ExecutionRunResponse(false, 1, "stdout", "stderr", "cid-1", 99.0)
            .StandardError.Should().Be("stderr");
    }

    [Fact]
    public void Contracts_records_touch_all_properties_for_coverage()
    {
        var orchestrationRequest = new OrchestrationRequest(
            "build",
            "{}",
            "agentic",
            "ollama",
            "L2",
            "us-west");
        orchestrationRequest.RuntimeSpecJson.Should().Be("{}");
        orchestrationRequest.PreferModel.Should().Be("agentic");
        orchestrationRequest.Provider.Should().Be("ollama");
        orchestrationRequest.BarrierLevel.Should().Be("L2");

        var orchestrationResponse = new OrchestrationResponse(
            true,
            "done",
            new { ok = true },
            1,
            2,
            "none");
        orchestrationResponse.Summary.Should().Be("done");
        orchestrationResponse.Output.Should().NotBeNull();
        orchestrationResponse.Escalations.Should().Be(2);

        var status = new StatusResponse("prod", "healthy", 10, 3, "pack", "3.0");
        status.Message.Should().Be("healthy");
        status.TotalAgents.Should().Be(10);
        status.ActiveAgents.Should().Be(3);
        status.ActivePackVersion.Should().Be("3.0");

        var envelope = new NexoIngressEnvelope("c1", NexoIngressTransports.Http, "idem", "v1", "tenant", "app");
        envelope.CorrelationId.Should().Be("c1");
        envelope.Transport.Should().Be(NexoIngressTransports.Http);
        envelope.IdempotencyKey.Should().Be("idem");
        envelope.PayloadVersion.Should().Be("v1");
        envelope.AppId.Should().Be("app");

        var context = new IngressContextResponse("c1", "http", "tenant", "app", "idem", "v1");
        context.Transport.Should().Be("http");
        context.TenantId.Should().Be("tenant");
        context.AppId.Should().Be("app");

        var smsReq = new SmsInboundSimulationRequest("+1", "yes", "SM9", "app-id");
        smsReq.From.Should().Be("+1");
        smsReq.MessageSid.Should().Be("SM9");
        smsReq.AppId.Should().Be("app-id");

        var smsResp = new SmsInboundSimulationResponse(true, "token", "accepted", false);
        smsResp.ApprovalToken.Should().Be("token");
        smsResp.Message.Should().Be("accepted");
    }
}
