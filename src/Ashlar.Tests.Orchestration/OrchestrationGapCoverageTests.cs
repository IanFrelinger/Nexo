using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Agents.Capabilities;
using Ashlar.Orchestration.Agents.Planning;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Assets.Models;
using Xunit;

namespace Ashlar.Tests.Orchestration;

/// <summary>Tests for orchestration gap coverage.</summary>
public class OrchestrationGapCoverageTests
{
    [Fact]
    public void AgentCapabilityRegistry_registers_and_queries_capabilities()
    {
        var registry = new AgentCapabilityRegistry();
        var cap = new AgentCapability
        {
            AgentId = "a1",
            Domain = "security",
            Capabilities = new[] { "scan", "audit" },
            SupportedModalities = new[] { "text", "image" },
            SupportedLanguages = new[] { "csharp" },
            SupportsLearning = true,
            Metadata = new Dictionary<string, object> { ["tier"] = "core" },
        };
        registry.Register("a1", cap);
        registry.GetCapability("a1").Should().Be(cap);
        registry.GetCapability("missing").Should().BeNull();
        registry.FindAgentsWithCapability("scan").Should().Contain("a1");
        registry.FindAgentsWithModality("image").Should().Contain("a1");
        registry.GetAllCapabilities().Should().ContainKey("a1");
    }

    [Fact]
    public void Architect_models_round_trip()
    {
        var spec = new AgentSpawnSpec
        {
            AgentId = "agent-1",
            Name = "Agent One",
            Domain = "Combat",
            Goal = "Design combat",
            Goals = new[] { "Design combat", "Balance" },
            Description = "desc",
            ClusterId = "c1",
            ReportsToAgentId = "lead",
            CommandChain = new[] { "lead", "architect" },
            OllamaModel = "llama",
            Dependencies = new[] { "dep" },
            Priority = 2,
            RequiredCapabilities = new[] { "reasoning" },
            Metadata = new Dictionary<string, object?> { ["k"] = 1 },
        };
        spec.Name.Should().Be("Agent One");
        spec.Goals.Should().HaveCount(2);

        var constraint = new AgentConstraint { Type = "Security", Description = "No PII", IsMandatory = false };
        constraint.IsMandatory.Should().BeFalse();

        var resources = new ResourceRequirements
        {
            EstimatedComputeSeconds = 30,
            RequiredContextTokens = 8000,
            RequiredMemoryMB = 512,
        };
        resources.RequiredMemoryMB.Should().Be(512);

        var agents = new[] { spec };
        var result = new DecompositionResult
        {
            Agents = agents,
            OriginalRequest = "build game",
            Reasoning = "split work",
            Confidence = 0.9,
        };
        result.IsValid.Should().BeTrue();

        var example = new DecompositionExample
        {
            Id = "ex-1",
            Request = "build game",
            Result = result,
            DomainTags = new[] { "Combat" },
            Keywords = new[] { "game" },
            QualityScore = 0.8,
        };
        example.Keywords.Should().Contain("game");
    }

    [Fact]
    public void Asset_manifest_counts_assets_by_type()
    {
        var prompt = new GenerationPrompt { TextPrompt = "icon" };
        var assets = new[]
        {
            new AssetOutput
            {
                AssetId = "a1",
                AssetType = AssetType.Image,
                FilePath = "/img.png",
                MimeType = "image/png",
                Prompt = prompt,
                FileSizeBytes = 10,
            },
            new AssetOutput
            {
                AssetId = "a2",
                AssetType = AssetType.Image,
                FilePath = "/img2.png",
                MimeType = "image/png",
                Prompt = prompt,
                FileSizeBytes = 20,
            },
        };
        var manifest = new AssetManifest { ProjectId = "p1", Assets = assets };
        manifest.AssetCounts[AssetType.Image].Should().Be(2);
        manifest.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task PlanningAgent_returns_first_planning_question_on_execute()
    {
        var spec = new AgentSpawnSpec
        {
            AgentId = "plan-1",
            Domain = "Planning",
            Goal = "Guide planning",
        };
        var agent = new PlanningAgent(spec, NullLogger<Ashlar.Orchestration.Agents.BaseAgent>.Instance);
        await agent.InitializeAsync();

        var result = await agent.ExecuteAsync();
        result.Should().BeOfType<PlanningResponse>();
        var response = (PlanningResponse)result;
        response.Type.Should().Be(PlanningResponseType.Question);
        response.Question.Should().NotBeNull();
        response.Question!.Id.Should().Be("genre");
        response.Context.Should().ContainKey("questionCount");
    }

    [Fact]
    public void PlanningQuestion_and_PlanningResponse_support_all_properties()
    {
        var question = new PlanningQuestion
        {
            Id = "q1",
            Text = "What genre?",
            Category = "design",
            Options = new[] { "RPG", "FPS" },
        };
        question.Options.Should().HaveCount(2);

        var response = new PlanningResponse
        {
            Type = PlanningResponseType.Recommendation,
            Recommendation = "Use RPG",
            RecommendedFeatures = new List<string> { "quests" },
            IterationPriorities = new List<string> { "combat" },
            Considerations = new List<string> { "scope" },
            Progress = 1.0,
            Context = new Dictionary<string, object> { ["done"] = true },
        };
        response.Type.Should().Be(PlanningResponseType.Recommendation);
        response.Context.Should().ContainKey("done");
    }

    [Fact]
    public async Task PlanningAgent_generate_recommendation_with_model_when_all_questions_answered()
    {
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput("""
                {
                  "summary": "Build a small action prototype",
                  "recommendedFeatures": ["combat loop"],
                  "iterationPriorities": ["movement"],
                  "considerations": ["keep scope small"]
                }
                """));

        var spec = new AgentSpawnSpec
        {
            AgentId = "plan-full",
            Domain = "Planning",
            Goal = "Guide planning",
        };
        var agent = new TestPlanningAgent(
            spec,
            NullLogger<Ashlar.Orchestration.Agents.BaseAgent>.Instance,
            model.Object);
        await agent.InitializeAsync();
        agent.PrepareForRecommendation(new Dictionary<string, string>
        {
            ["genre"] = "Action",
            ["scope"] = "Small (prototype)",
            ["target_audience"] = "Casual players",
            ["art_style"] = "Pixel art",
            ["core_mechanic"] = "Combat",
            ["platform"] = "PC",
        });

        var final = (PlanningResponse)await agent.ExecuteAsync();
        final.Type.Should().Be(PlanningResponseType.Recommendation);
        final.Recommendation.Should().Contain("prototype");
        final.RecommendedFeatures.Should().Contain("combat loop");
    }

    /// <summary>Test planning agent.</summary>
    private sealed class TestPlanningAgent : PlanningAgent
    {
        public TestPlanningAgent(AgentSpawnSpec spec, ILogger<Ashlar.Orchestration.Agents.BaseAgent> logger, IModel? model)
            : base(spec, logger, model)
        {
        }

        public void PrepareForRecommendation(IReadOnlyDictionary<string, string> answers)
        {
            var indexField = typeof(PlanningAgent).GetField("_currentQuestionIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            indexField.SetValue(this, 6);

            var responsesField = typeof(PlanningAgent).GetField("_userResponses", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var responses = (Dictionary<string, string>)responsesField.GetValue(this)!;
            foreach (var (key, value) in answers)
                responses[key] = value;

            State = AgentState.Ready;
        }
    }
}
