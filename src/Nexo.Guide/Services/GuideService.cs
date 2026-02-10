using System.Text.Json;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Execution;
using Nexo.Guide.Agent.Behaviors;
using Nexo.Guide.Rendering;
using Nexo.Guide.ViewModels;

namespace Nexo.Guide.Services;

/// <summary>
/// Bridges the UI to the Nexo stack: runs the Guide chat behavior via IBehaviorExecutor,
/// then runs render (in-app) when the LLM returns a render plan. Ephemeral session state in memory.
/// </summary>
public class GuideService
{
    private readonly IBehaviorExecutor _behaviorExecutor;
    private readonly IBehaviorRegistry _behaviorRegistry;
    private readonly RenderBehavior _renderBehavior;
    private readonly AgentCard _agentCard;
    private readonly List<ConversationTurn> _history = new();
    private readonly HashSet<string> _topicsCovered = new();
    private bool _suspended;

    public event Action<string>? OnNarration;
    public event Action<BrickLogEntry>? OnBrickExecuted;
    public event Action? OnRenderStarted;

    public GuideService(
        IBehaviorExecutor behaviorExecutor,
        IBehaviorRegistry behaviorRegistry,
        RenderBehavior renderBehavior,
        AgentCard agentCard)
    {
        _behaviorExecutor = behaviorExecutor;
        _behaviorRegistry = behaviorRegistry;
        _renderBehavior = renderBehavior;
        _agentCard = agentCard;
        _renderBehavior.OnNarration += text => OnNarration?.Invoke(text);
        _renderBehavior.OnBrickExecuted += entry => OnBrickExecuted?.Invoke(entry);
    }

    public void Suspend() => _suspended = true;
    public void Resume() => _suspended = false;

    public void ClearSession()
    {
        _history.Clear();
        _topicsCovered.Clear();
    }

    public async Task<GuideResponse> SendAsync(string message, string audience)
    {
        if (_suspended)
            throw new InvalidOperationException("Agent is suspended (app backgrounded).");

        var sw = System.Diagnostics.Stopwatch.StartNew();

        var historyJson = JsonSerializer.Serialize(_history.Select(t => new { role = t.Role, content = t.Content }));
        var topicsCoveredJson = JsonSerializer.Serialize(_topicsCovered.ToArray());

        var input = new BehaviorInput(new Dictionary<string, object>
        {
            ["userMessage"] = message,
            ["audience"] = audience,
            ["historyJson"] = historyJson,
            ["topicsCoveredJson"] = topicsCoveredJson
        });

        var behavior = _behaviorRegistry.GetBehavior(GuideNexoSetup.GuideChatBehaviorId);
        if (behavior == null)
        {
            sw.Stop();
            return new GuideResponse
            {
                Text = "Guide chat behavior not registered.",
                BricksExecuted = 0,
                InferenceMs = sw.ElapsedMilliseconds,
                SuggestedQuestions = null
            };
        }

        var options = new ExecutionOptions
        {
            IsAirGapped = false,
            AuditMode = false,
            Provider = "ollama"
        };

        var result = await _behaviorExecutor.ExecuteAsync(_agentCard, behavior, input, options).ConfigureAwait(false);
        sw.Stop();

        var text = result.Outputs.TryGetValue("text", out var t) ? t?.ToString() ?? "" : "";
        var renderPlanJson = result.Outputs.TryGetValue("renderPlanJson", out var rp) ? rp?.ToString() : null;
        var suggestedQuestionsJson = result.Outputs.TryGetValue("suggestedQuestionsJson", out var sq) ? sq?.ToString() : null;
        var topicCovered = result.Outputs.TryGetValue("topicCovered", out var tc) ? tc?.ToString() : null;

        if (!string.IsNullOrEmpty(topicCovered))
            _topicsCovered.Add(topicCovered);

        _history.Add(new ConversationTurn("user", message));
        _history.Add(new ConversationTurn("assistant", text));

        var bricksExecuted = result.Success ? 7 : 0; // chat pipeline brick count
        RenderPlan? renderPlan = null;
        if (!string.IsNullOrWhiteSpace(renderPlanJson) && renderPlanJson != "null" && renderPlanJson.TrimStart().StartsWith('{'))
        {
            try
            {
                var el = JsonSerializer.Deserialize<JsonElement>(renderPlanJson);
                renderPlan = RenderPlan.FromLlmOutput(el);
            }
            catch { /* ignore */ }
        }

        if (renderPlan != null)
        {
            OnRenderStarted?.Invoke();
            var renderResult = await _renderBehavior.ExecuteAsync(renderPlan, CancellationToken.None).ConfigureAwait(false);
            bricksExecuted += renderResult.BricksExecuted;
        }

        string[]? suggestedQuestions = null;
        if (!string.IsNullOrWhiteSpace(suggestedQuestionsJson) && suggestedQuestionsJson.TrimStart().StartsWith('['))
        {
            try
            {
                suggestedQuestions = JsonSerializer.Deserialize<string[]>(suggestedQuestionsJson);
            }
            catch { }
        }

        return new GuideResponse
        {
            Text = text,
            BricksExecuted = bricksExecuted,
            InferenceMs = sw.ElapsedMilliseconds,
            SuggestedQuestions = suggestedQuestions ?? Array.Empty<string>()
        };
    }
}

public class GuideResponse
{
    public string Text { get; init; } = "";
    public int BricksExecuted { get; init; }
    public long InferenceMs { get; init; }
    public string[]? SuggestedQuestions { get; init; }
}

internal record ConversationTurn(string Role, string Content);
