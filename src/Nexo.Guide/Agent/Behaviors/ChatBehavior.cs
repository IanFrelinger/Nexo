using System.Reflection;
using System.Text.Json;
using Nexo.Guide.Agent;
using Nexo.Guide.Knowledge;
using Nexo.Guide.Ollama;
using Nexo.Guide.Rendering;

namespace Nexo.Guide.Agent.Behaviors;

public class ChatBehavior
{
    private readonly IOllamaClient _ollama;

    public ChatBehavior(IOllamaClient ollama)
    {
        _ollama = ollama;
    }

    public async Task<ChatResult> ExecuteAsync(ChatInput input, CancellationToken ct)
    {
        var audience = DetectAudience(input.Audience, input.UserMessage);
        var context = input.Knowledge.GetRelevantContext(input.UserMessage);
        var systemPrompt = BuildSystemPrompt(audience, context, input.TopicsCovered);
        var messages = BuildMessages(systemPrompt, input.History, input.UserMessage);

        var raw = await _ollama.GenerateAsync(messages, ct).ConfigureAwait(false);
        var (text, renderPlan) = ParseResponse(raw);
        text = ValidateResponse(text, context);
        var topic = IdentifyTopic(input.UserMessage, text);
        var suggestions = SuggestNextTopics(input.TopicsCovered, topic);

        return new ChatResult(
            Text: text,
            RenderPlan: renderPlan,
            TopicCovered: topic,
            SuggestedQuestions: suggestions,
            ChatBricksUsed: 7);
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
                var parsed = JsonSerializer.Deserialize<LlmResponse>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed?.Text != null)
                {
                    RenderPlan? plan = null;
                    if (parsed.Render.ValueKind != JsonValueKind.Null &&
                        parsed.Render.ValueKind != JsonValueKind.Undefined)
                    {
                        plan = RenderPlan.FromLlmOutput(parsed.Render);
                    }
                    return (parsed.Text, plan);
                }
            }
        }
        catch
        {
            // Not JSON — plain text response
        }
        return (raw.Trim(), null);
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

        return $"{systemPrompt}\n\n{audienceContext}\n\n{renderPrompt}\n\n" +
               $"## Relevant knowledge:\n{context}\n\n" +
               $"## Topics already covered (don't repeat):\n{string.Join(", ", topicsCovered)}";
    }

    private static string DetectAudience(string explicitAudience, string message)
    {
        if (!string.IsNullOrEmpty(explicitAudience)) return explicitAudience;
        var lower = message.ToLowerInvariant();
        if (lower.Contains("brick") || lower.Contains("interface") || lower.Contains("namespace"))
            return "developer";
        if (lower.Contains("cost") || lower.Contains("roi") || lower.Contains("compliance"))
            return "decision-maker";
        return "general";
    }

    private static string ValidateResponse(string text, string context) => text;

    private static string? IdentifyTopic(string question, string response) => null;

    private static string[] SuggestNextTopics(string[] covered, string? current) => Array.Empty<string>();

    private static List<OllamaMessage> BuildMessages(string system, ConversationTurn[] history, string user)
    {
        var list = new List<OllamaMessage> { new("system", system) };
        foreach (var turn in history)
            list.Add(new OllamaMessage(turn.Role, turn.Content));
        list.Add(new OllamaMessage("user", user));
        return list;
    }

    private static string LoadPrompt(string name)
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith("." + name, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(resourceName))
            return $"[Prompt not found: {name}]";
        using var stream = asm.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

public record ChatInput(
    string UserMessage,
    string Audience,
    ConversationTurn[] History,
    string[] TopicsCovered,
    NexoKnowledgeBase Knowledge);

public record ChatResult(
    string Text,
    RenderPlan? RenderPlan,
    string? TopicCovered,
    string[] SuggestedQuestions,
    int ChatBricksUsed);

public class LlmResponse
{
    public string? Text { get; set; }
    public JsonElement Render { get; set; }
}
