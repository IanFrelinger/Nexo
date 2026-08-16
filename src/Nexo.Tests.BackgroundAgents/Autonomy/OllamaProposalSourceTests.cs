using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Nexo.BackgroundAgents.Autonomy;
using Nexo.Core.Application.Autonomy;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Autonomy;

/// <summary>
/// The live proposer's model-independent guarantees. None of these need a model: they pin
/// what the proposer is HANDED (the objective's declarations, its own previous source, and
/// the policy-projected feedback — never a witness), how the repair prompt is shaped
/// (contract loudest, one-sentence ask, or the whole body when the operator asks for it),
/// and what comes back (provenance in the signature: provider, model, mode, attempt).
/// The rates themselves are measured live and recorded in docs/certification-evidence.md.
/// </summary>
public sealed class OllamaProposalSourceTests
{
    private const string Body = """
        Add a brick that classifies a scanned tag payload.

        Contract:
        - Input `payload` (string): the raw scanned text.
        - Output `isValid` (bool): whether the payload decodes to a tag reference.
        - Output `failureCode` (string): the codec's failure code when invalid; the EMPTY STRING when valid. NEVER null.

        Notes:
        - Prefer the codec already in the assembly.
        """;

    private const string PreviousSource = "namespace P;\npublic sealed class TagScanClassifier { }\n";
    private const string Feedback = "certification: REJECT at 'correctness' — 1 witness finding(s):\n  case 0: output['failureCode'] violates the contract; you produced <null>";

    [Fact]
    public void ExtractContract_PullsOnlyTheContractList()
    {
        var contract = OllamaProposalSource.ExtractContract(Body);

        contract.Should().StartWith("- Input `payload`");
        contract.Should().Contain("NEVER null");
        contract.Should().NotContain("Notes:").And.NotContain("Prefer the codec",
            "the narrative around the contract is what buried the signal for a small model");
        contract.Should().NotContain("classifies a scanned tag payload");
    }

    [Fact]
    public void ExtractContract_FallsBackToTheWholeBody_WhenThereIsNoContractHeading()
    {
        const string body = "Just do the thing.\n\n- not a contract item, no heading above";

        OllamaProposalSource.ExtractContract(body).Should().Be(body.Trim(),
            "an objective without a Contract: section still has to reach the model intact");
    }

    [Fact]
    public void RepairPrompt_CarriesProjectedFeedbackAndOwnSource_AndOnlyTheContractByDefault()
    {
        var source = new OllamaProposalSource(new HttpClient(new FakeOllama("")), new OllamaProposalOptions());
        var request = new ProposalRequest("tag-scan-classifier", "Classify a scanned tag payload", Body, null)
        {
            Repair = new RepairContext(PreviousSource, Feedback, 1),
        };

        var prompt = source.BuildPrompt(request);

        prompt.Should().Contain("you produced <null>", "the projection is passed through verbatim");
        prompt.Should().Contain(PreviousSource.TrimEnd(), "the proposer repairs its OWN source");
        prompt.Should().Contain("The contract it must satisfy:").And.Contain("NEVER null");
        prompt.Should().NotContain("Prefer the codec", "RepairWithContractOnly hands the model the contract, not the narrative");
        prompt.Should().Contain("Deterministic only");
        prompt.Should().NotContain("Objective: ", "the initial-proposal framing must not leak into a repair");
    }

    [Fact]
    public void BuildRepairPrompt_AlwaysCarriesTheWholeBody_EvenWithContractOnlyOn()
    {
        // Campaign 2: a compile fix needs the skeleton and API notes, which the contract
        // bullets do not carry — stripping them left the model re-emitting the same broken
        // source three times over. Contract-only is a certification-repair setting.
        var source = new OllamaProposalSource(new HttpClient(new FakeOllama("")), new OllamaProposalOptions { RepairWithContractOnly = true });
        var request = new ProposalRequest("tag-scan-classifier", "Classify a scanned tag payload", Body, null)
        {
            Repair = new RepairContext(PreviousSource, "certification: REJECT at 'build' — the candidate did not compile — 1 diagnostic(s):\n  line 3, col 1: CS0117: no", 1, RepairKind.Build),
        };

        var prompt = source.BuildPrompt(request);

        prompt.Should().Contain("Prefer the codec", "a build repair gets the whole objective, skeleton and API notes included");
        prompt.Should().Contain("including the skeleton and API notes");
        prompt.Should().Contain("CS0117");
    }

    [Fact]
    public void RepairPrompt_CarriesTheWholeBody_WhenContractOnlyIsSwitchedOff()
    {
        var options = new OllamaProposalOptions { RepairWithContractOnly = false };
        var source = new OllamaProposalSource(new HttpClient(new FakeOllama("")), options);
        var request = new ProposalRequest("tag-scan-classifier", "Classify a scanned tag payload", Body, null)
        {
            Repair = new RepairContext(PreviousSource, Feedback, 1),
        };

        var prompt = source.BuildPrompt(request);

        prompt.Should().Contain("Prefer the codec", "a larger model may want the full objective");
        prompt.Should().Contain("NEVER null");
    }

    [Fact]
    public void InitialPrompt_StatesTheObjective_AndTheHardConstraints()
    {
        var source = new OllamaProposalSource(new HttpClient(new FakeOllama("")), new OllamaProposalOptions());
        var request = new ProposalRequest("tag-scan-classifier", "Classify a scanned tag payload", Body, null);

        var prompt = source.BuildPrompt(request);

        prompt.Should().StartWith("Objective: ");
        prompt.Should().Contain("Hard constraints (the gate rejects violations):");
        prompt.Should().NotContain("REJECTED", "no repair framing on a first proposal");
    }

    [Fact]
    public async Task ProposeAsync_SignatureEncodesProviderModelModeAndAttempt_AndOptionsReachTheDaemon()
    {
        var fake = new FakeOllama("Here you go:\n```csharp\nnamespace P;\npublic sealed class TagScanClassifier { }\n```\n");
        var options = new OllamaProposalOptions { Model = "qwen2.5-coder:14b", Temperature = 0.35, MaxTokens = 2048 };
        var source = new OllamaProposalSource(new HttpClient(fake), options);
        var request = new ProposalRequest("tag-scan-classifier", "t", Body, null)
        {
            Repair = new RepairContext(PreviousSource, Feedback, 2),
        };

        var proposed = await source.ProposeAsync(request);

        proposed.Should().NotBeNull();
        proposed!.TypeName.Should().Be("P.TagScanClassifier");
        proposed.SourceCode.Should().Contain("class TagScanClassifier");
        proposed.ProposerSignature.Should().StartWith("model:ollama:qwen2.5-coder-14b:repair2:",
            "which model produced this, and that it was the second repair, is provenance the certificate binds");

        // The dials really reach the daemon — that is what makes the same loop serve different models.
        using var sent = JsonDocument.Parse(fake.LastRequestBody!);
        sent.RootElement.GetProperty("model").GetString().Should().Be("qwen2.5-coder:14b");
        sent.RootElement.GetProperty("options").GetProperty("temperature").GetDouble().Should().Be(0.35);
        sent.RootElement.GetProperty("options").GetProperty("num_predict").GetInt32().Should().Be(2048);
        sent.RootElement.GetProperty("stream").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task ProposeAsync_ReturnsNull_WhenTheModelProducesNoCode()
    {
        var source = new OllamaProposalSource(new HttpClient(new FakeOllama("I cannot help with that.")), new OllamaProposalOptions());

        var proposed = await source.ProposeAsync(new ProposalRequest("x", "t", Body, null));

        proposed.Should().BeNull("no proposal is a normal outcome, never evidence about a candidate");
    }

    /// <summary>A stand-in daemon: records the last request body, answers with a canned response.</summary>
    private sealed class FakeOllama : HttpMessageHandler
    {
        private readonly string _response;

        public FakeOllama(string response) => _response = response;

        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Serialize(new { response = _response });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
        }
    }
}
