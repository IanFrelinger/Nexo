using Nexo.Guide.Agent.Behaviors;
using Nexo.Guide.Knowledge;
using Nexo.Guide.Sandbox;
using Nexo.Guide.Services;
using Nexo.Guide.ViewModels;

namespace Nexo.Guide.Agent;

public class NexoGuideAgent : IDisposable
{
    private readonly ChatBehavior _chat;
    private readonly RenderBehavior _render;
    private readonly SandboxedExecutionEngine _engine;
    private readonly NexoKnowledgeBase _knowledge;
    private bool _suspended;
    private readonly CancellationTokenSource _cts = new();

    private readonly List<ConversationTurn> _history = new();
    private readonly HashSet<string> _topicsCovered = new();

    public event Action<string>? OnNarration;
    public event Action<BrickLogEntry>? OnBrickExecuted;
    public event Action? OnRenderStarted;

    public NexoGuideAgent(
        ChatBehavior chat,
        RenderBehavior render,
        SandboxedExecutionEngine engine)
    {
        _chat = chat;
        _render = render;
        _engine = engine;
        _knowledge = new NexoKnowledgeBase();
        _render.OnNarration += text => OnNarration?.Invoke(text);
        _render.OnBrickExecuted += entry => OnBrickExecuted?.Invoke(entry);
    }

    public async Task<AgentResponse> ProcessAsync(string userMessage, string audience)
    {
        if (_suspended)
            throw new InvalidOperationException("Agent is suspended (app backgrounded).");

        _engine.ResetTurnBudget();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var chatResult = await _chat.ExecuteAsync(new ChatInput(
            userMessage,
            audience,
            _history.ToArray(),
            _topicsCovered.ToArray(),
            _knowledge
        ), _cts.Token).ConfigureAwait(false);
        sw.Stop();

        int bricksExecuted = 0;
        if (chatResult.RenderPlan != null)
        {
            OnRenderStarted?.Invoke();
            var renderResult = await _render.ExecuteAsync(chatResult.RenderPlan, _cts.Token).ConfigureAwait(false);
            bricksExecuted = renderResult.BricksExecuted;
        }

        _history.Add(new ConversationTurn("user", userMessage));
        _history.Add(new ConversationTurn("assistant", chatResult.Text));
        if (chatResult.TopicCovered != null)
            _topicsCovered.Add(chatResult.TopicCovered);

        return new AgentResponse
        {
            Text = chatResult.Text,
            BricksExecuted = bricksExecuted + chatResult.ChatBricksUsed,
            InferenceMs = sw.ElapsedMilliseconds,
            SuggestedQuestions = chatResult.SuggestedQuestions
        };
    }

    public void Suspend() => _suspended = true;
    public void Resume() => _suspended = false;

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _history.Clear();
        _topicsCovered.Clear();
    }
}

public record ConversationTurn(string Role, string Content);

public class AgentResponse
{
    public string Text { get; init; } = "";
    public int BricksExecuted { get; init; }
    public long InferenceMs { get; init; }
    public string[]? SuggestedQuestions { get; init; }
}
