using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nexo.Guide.Rendering;
using Nexo.Guide.Services;

namespace Nexo.Guide.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly GuideService _guide;
    private readonly IRenderSurface2D _surface2D;
    private readonly IRenderSurface3D _surface3D;

    public MainViewModel(
        GuideService guide,
        IRenderSurface2D surface2D,
        IRenderSurface3D surface3D)
    {
        _guide = guide;
        _surface2D = surface2D;
        _surface3D = surface3D;

        _guide.OnNarration += narration => CurrentNarration = narration;
        _guide.OnBrickExecuted += entry =>
        {
            BrickLogEntries.Add(entry);
            HasBrickActivity = true;
        };
        _guide.OnRenderStarted += () =>
        {
            IsViewportVisible = true;
            ViewportWidth = 400;
        };

        Messages.Add(new ChatMessage
        {
            Role = "assistant",
            Text = "I'm Nexo's guide. As we talk, I can create live visuals to explain concepts — diagrams, charts, 3D models, animations. Everything is created in real-time, right here in the sandbox.\n\nAsk me anything, or try \"Show me the architecture.\""
        });

        Suggestions = new ObservableCollection<string>
        {
            "What is Nexo?",
            "Show me the architecture",
            "How is this different from ChatGPT?",
            "What could I build with this?"
        };
    }

    [ObservableProperty] private string inputText = "";
    [ObservableProperty] private string audience = "general";
    [ObservableProperty] private string currentNarration = "";
    [ObservableProperty] private bool isViewportVisible;
    [ObservableProperty] private double viewportWidth = 400;
    [ObservableProperty] private bool isTyping;
    [ObservableProperty] private bool hasBrickActivity;

    [ObservableProperty] private bool isChatPageActive = true;
    [ObservableProperty] private bool isArchPageActive;

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ObservableCollection<BrickLogEntry> BrickLogEntries { get; } = new();
    public ObservableCollection<string> Suggestions { get; set; } = new();
    public bool HasSuggestions => Suggestions?.Count > 0;
    public IRenderSurface2D RenderSurface2D => _surface2D;
    public IRenderSurface3D RenderSurface3D => _surface3D;

    public string ModelName => "llama3.1:8b";
    public int BrickCount => 24; // Render + Chat bricks
    public ObservableCollection<string> TopicsCovered { get; } = new();
    public ObservableCollection<BrickInfo> GuideBricks { get; } = new()
    {
        new BrickInfo("⚙️", "ContextInjection", "Selects relevant knowledge"),
        new BrickInfo("⚙️", "AudienceDetection", "Detects technical level"),
        new BrickInfo("🤖", "LocalInference", "Calls Ollama"),
        new BrickInfo("⚙️", "ResponseSafety", "Validates against facts"),
        new BrickInfo("⚙️", "ConversationMemory", "Manages chat history"),
        new BrickInfo("⚙️", "TopicTracker", "Suggests topics"),
        new BrickInfo("⚙️", "RenderDecision", "Decides if response needs visuals"),
    };

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        var text = InputText?.Trim();
        if (string.IsNullOrEmpty(text)) return;
        InputText = "";
        Messages.Add(new ChatMessage { Role = "user", Text = text });
        IsTyping = true;
        BrickLogEntries.Clear();

        try
        {
            var response = await _guide.SendAsync(text, Audience).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsTyping = false;
                Messages.Add(new ChatMessage
                {
                    Role = "assistant",
                    Text = response.Text,
                    BrickCount = response.BricksExecuted,
                    InferenceMs = response.InferenceMs
                });
                if (response.SuggestedQuestions is { Length: > 0 })
                {
                    Suggestions = new ObservableCollection<string>(response.SuggestedQuestions);
                    OnPropertyChanged(nameof(Suggestions));
                    OnPropertyChanged(nameof(HasSuggestions));
                }
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsTyping = false;
                Messages.Add(new ChatMessage
                {
                    Role = "assistant",
                    Text = $"Something went wrong: {ex.Message}"
                });
            });
        }
    }

    [RelayCommand]
    private async Task SendSuggestionAsync(string? question)
    {
        if (string.IsNullOrEmpty(question)) return;
        InputText = question;
        await SendMessageAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private void NavigateToChat()
    {
        IsChatPageActive = true;
        IsArchPageActive = false;
    }

    [RelayCommand]
    private void NavigateToArch()
    {
        IsChatPageActive = false;
        IsArchPageActive = true;
    }
}

public class ChatMessage
{
    public string Role { get; init; } = "";
    public string Text { get; init; } = "";
    public int? BrickCount { get; init; }
    public long? InferenceMs { get; init; }
    public bool IsUser => Role == "user";
    public bool IsAssistant => Role == "assistant";
    public bool HasMetrics => BrickCount.HasValue || InferenceMs.HasValue;
}

public class BrickLogEntry
{
    public string BrickName { get; init; } = "";
    public string Mode { get; init; } = "⚙️";
    public long ElapsedMs { get; init; }
}

public record BrickInfo(string Mode, string Name, string Purpose);
