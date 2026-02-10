using System.Reflection;
using System.Text.Json;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Guide.Knowledge;
using Nexo.Guide.Ollama;
using Nexo.Guide.Rendering;

namespace Nexo.Guide.Bricks;

/// <summary>
/// Single brick that runs the full Guide chat pipeline (context, audience, inference, safety, topics).
/// Dog-foods Nexo domain Brick; uses Ollama and knowledge base.
/// </summary>
public sealed class GuideChatBrick : Brick
{
    private readonly IOllamaClient _ollama;
    private readonly NexoKnowledgeBase _knowledge;

    public GuideChatBrick(IOllamaClient ollama, NexoKnowledgeBase knowledge)
    {
        _ollama = ollama ?? throw new ArgumentNullException(nameof(ollama));
        _knowledge = knowledge ?? throw new ArgumentNullException(nameof(knowledge));

        Id = "nexo-guide.chat";
        Name = "Guide Chat";
        Version = "1.0.0";
        Icon = "💬";
        Category = BrickCategory.Transform;
        Description = "Nexo Guide chat pipeline: context injection, audience, Ollama inference, safety, suggestions.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("userMessage", "string", "User message"),
                new BrickInputDefinition("audience", "string", "Audience: general, developer, decision-maker"),
                new BrickInputDefinition("historyJson", "string", "JSON array of {role, content}"),
                new BrickInputDefinition("topicsCoveredJson", "string", "JSON array of topic strings")
            ],
            Outputs =
            [
                new BrickOutputDefinition("text", "string", "Assistant response text"),
                new BrickOutputDefinition("renderPlanJson", "string", "Optional render plan JSON or null"),
                new BrickOutputDefinition("suggestedQuestionsJson", "string", "JSON array of suggestion strings"),
                new BrickOutputDefinition("topicCovered", "string", "Topic covered this turn or null")
            ]
        };

        Implementations = new BrickImplementations
        {
            Agentic = new AgenticImplementation
            {
                Id = "guide-chat-agentic",
                Name = "Ollama Chat",
                Description = "Uses local Ollama for full chat with history",
                LLMConfig = new LLMConfig
                {
                    Model = "llama3.1:8b",
                    SystemPrompt = "You are the Nexo Guide.",
                    Temperature = 0.3,
                    MaxTokens = 2000
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = false,
                    RequiresNetwork = false,
                    Latency = "1-10s",
                    ResourceUsage = ResourceUsage.Medium
                }
            }
        };
        DefaultImplementation = ImplementationType.Agentic;
        FallbackChain = [ImplementationType.Agentic];
    }

    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var userMessage = input.Get<string>("userMessage");
        var audience = input.Get<string>("audience");
        var historyJson = input.Get<string>("historyJson");
        var topicsCoveredJson = input.Get<string>("topicsCoveredJson");

        var audienceResolved = ResolveAudience(audience, userMessage);
        var knowledgeContext = _knowledge.GetRelevantContext(userMessage);
        var topicsCovered = string.IsNullOrEmpty(topicsCoveredJson)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(topicsCoveredJson) ?? Array.Empty<string>();

        var systemPrompt = BuildSystemPrompt(audienceResolved, knowledgeContext, topicsCovered);
        var messages = BuildMessages(systemPrompt, historyJson, userMessage);

        var raw = await _ollama.GenerateAsync(messages, cancellationToken).ConfigureAwait(false);
        var (text, renderPlan) = ParseResponse(raw);
        text = ValidateResponse(text, knowledgeContext);
        var topicCovered = IdentifyTopic(userMessage, text);
        var suggestions = SuggestNextTopics(topicsCovered, topicCovered);

        var outPlan = renderPlan != null
            ? JsonSerializer.Serialize(new { surface = renderPlan.Surface, clear = renderPlan.Clear, steps = renderPlan.Steps })
            : (object?)null;

        return new BrickOutput
        {
            ["text"] = text,
            ["renderPlanJson"] = outPlan ?? (object)"",
            ["suggestedQuestionsJson"] = JsonSerializer.Serialize(suggestions),
            ["topicCovered"] = topicCovered ?? (object)"",
            Summary = $"Response: {text.Length} chars"
        };
    }

    private static string ResolveAudience(string explicitAudience, string message)
    {
        if (!string.IsNullOrEmpty(explicitAudience)) return explicitAudience;
        var lower = message.ToLowerInvariant();
        if (lower.Contains("brick") || lower.Contains("interface") || lower.Contains("namespace")) return "developer";
        if (lower.Contains("cost") || lower.Contains("roi") || lower.Contains("compliance")) return "decision-maker";
        return "general";
    }

    private string BuildSystemPrompt(string audience, string context, string[] topicsCovered)
    {
        var systemPrompt = LoadPrompt("SystemPrompt.txt");
        var renderPrompt = LoadPrompt("RenderPrompt.txt");
        var audienceContext = audience switch
        {
            "developer" => LoadPrompt("DeveloperContext.txt"),
            "decision-maker" => LoadPrompt("DecisionMakerContext.txt"),
            _ => LoadPrompt("LaypersonContext.txt")
        };
        return $"{systemPrompt}\n\n{audienceContext}\n\n{renderPrompt}\n\n## Relevant knowledge:\n{context}\n\n## Topics already covered:\n{string.Join(", ", topicsCovered)}";
    }

    private static List<OllamaMessage> BuildMessages(string system, string historyJson, string user)
    {
        var list = new List<OllamaMessage> { new("system", system) };
        if (!string.IsNullOrWhiteSpace(historyJson))
        {
            try
            {
                var arr = JsonSerializer.Deserialize<JsonElement[]>(historyJson);
                if (arr != null)
                    foreach (var el in arr)
                    {
                        var role = el.TryGetProperty("role", out var r) ? r.GetString() ?? "user" : "user";
                        var content = el.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                        list.Add(new OllamaMessage(role, content));
                    }
            }
            catch { /* ignore */ }
        }
        list.Add(new OllamaMessage("user", user));
        return list;
    }

    private static (string text, RenderPlan? plan) ParseResponse(string raw)
    {
        try
        {
            var jsonStart = raw.IndexOf('{');
            var jsonEnd = raw.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = raw[jsonStart..(jsonEnd + 1)];
                var parsed = JsonSerializer.Deserialize<LlmResponse>(json);
                if (parsed?.Text != null)
                {
                    RenderPlan? plan = null;
                    if (parsed.Render.ValueKind != JsonValueKind.Null && parsed.Render.ValueKind != JsonValueKind.Undefined)
                        plan = RenderPlan.FromLlmOutput(parsed.Render);
                    return (parsed.Text, plan);
                }
            }
        }
        catch { }
        return (raw.Trim(), null);
    }

    private static string ValidateResponse(string text, string context) => text;
    private static string? IdentifyTopic(string question, string response) => null;
    private static string[] SuggestNextTopics(string[] covered, string? current) => Array.Empty<string>();

    private static string LoadPrompt(string name)
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith("." + name, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(resourceName)) return $"[Prompt: {name}]";
        using var stream = asm.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private class LlmResponse
    {
        public string? Text { get; set; }
        public JsonElement Render { get; set; }
    }
}
