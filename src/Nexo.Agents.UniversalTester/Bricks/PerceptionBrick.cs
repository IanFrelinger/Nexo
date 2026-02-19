using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester.Adapters;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;
using System.Text.Json;

namespace Nexo.Agents.UniversalTester.Bricks;

/// <summary>
/// Captures current state from any target application.
/// Agentic mode uses vision to augment capture when the adapter provides limited structural data (e.g. desktop apps).
/// </summary>
public class PerceptionBrick : Brick
{
    private readonly IProviderFactory? _providerFactory;
    private readonly ILogger<PerceptionBrick>? _logger;

    public PerceptionBrick(IProviderFactory? providerFactory = null, ILogger<PerceptionBrick>? logger = null)
    {
        _providerFactory = providerFactory;
        _logger = logger;

        Id = "universal-tester.perception";
        Name = "Perception";
        Version = "1.0.0";
        Icon = "👁️";
        Category = BrickCategory.Input;
        Description = "Captures current state from any target application";

        Interface = new BrickInterface
        {
            Inputs = [
                new BrickInputDefinition("adapter", "ITargetAdapter", "Target adapter to capture from"),
                new BrickInputDefinition("previousState", "PerceptionState", "Previous perception state for comparison", required: false),
                new BrickInputDefinition("captureScreenshot", "bool", "Whether to capture screenshot", required: false, defaultValue: true),
                new BrickInputDefinition("captureDOM", "bool", "Whether to capture DOM/structure", required: false, defaultValue: true),
                new BrickInputDefinition("capturePerformance", "bool", "Whether to capture performance metrics", required: false, defaultValue: true),
                new BrickInputDefinition("modelOverride", "string", "Per-brick model override (e.g. llava:7b for Ollama)", required: false)
            ],
            Outputs = [
                new BrickOutputDefinition("perception", "PerceptionState", "Captured perception state")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "perception-deterministic",
                Name = "Deterministic Perception",
                Description = "Captures state using adapter APIs",
                Executor = "DirectExecutor",
                Config = new Dictionary<string, object>()
            },
            Agentic = new AgenticImplementation
            {
                Id = "perception-agentic",
                Name = "AI-Augmented Perception",
                Description = "Captures state and uses vision to identify UI elements when adapter provides no DOM",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "You are a UI analyzer. Identify interactive elements from the screenshot.",
                    Temperature = 0.2,
                    MaxTokens = 2000
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "2-5s",
                    Deterministic = false,
                    RequiresNetwork = true,
                    ResourceUsage = ResourceUsage.Medium
                }
            }
        };

        DefaultImplementation = ImplementationType.Agentic;
    }
    
    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var adapter = input.Get<ITargetAdapter>("adapter");
        var previousState = input.Get<PerceptionState?>("previousState", null);
        var captureScreenshot = input.Get<bool>("captureScreenshot", true);
        var captureDOM = input.Get<bool>("captureDOM", true);
        var capturePerformance = input.Get<bool>("capturePerformance", true);
        var modelOverride = input.Get<string>("modelOverride", null);

        var state = await CaptureFromAdapterAsync(adapter, previousState, captureScreenshot, captureDOM, capturePerformance, cancellationToken);

        // Agentic: augment with vision when adapter provides few/no interactive elements
        if (implementation == ImplementationType.Agentic &&
            !context.IsAirGapped &&
            _providerFactory != null &&
            _providerFactory.IsProviderAvailable(context.Provider) &&
            state.Screenshot != null &&
            state.Screenshot.Length > 0 &&
            state.InteractiveElements.Count == 0)
        {
            try
            {
                var augmentedElements = await AugmentWithVisionAsync(state, context, modelOverride, cancellationToken);
                if (augmentedElements.Count > 0)
                {
                    state = state with { InteractiveElements = augmentedElements };
                    _logger?.LogDebug("Perception augmented with {Count} AI-derived elements", augmentedElements.Count);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Agentic perception augmentation failed; using raw capture");
            }
        }

        return new BrickOutput
        {
            ["perception"] = state,
            Summary = $"Captured {state.InteractiveElements.Count} interactive elements, {state.Errors.Count} errors"
        };
    }

    private static async Task<PerceptionState> CaptureFromAdapterAsync(
        ITargetAdapter adapter,
        PerceptionState? previousState,
        bool captureScreenshot,
        bool captureDOM,
        bool capturePerformance,
        CancellationToken cancellationToken)
    {
        var state = new PerceptionState
        {
            Timestamp = DateTime.UtcNow,

            Screenshot = captureScreenshot ? await adapter.CaptureScreenshotAsync(cancellationToken) : null,
            PreviousScreenshot = previousState?.Screenshot,

            DomSnapshot = captureDOM ? await adapter.GetStructureAsync(cancellationToken) : null,
            InteractiveElements = await adapter.GetInteractiveElementsAsync(cancellationToken),
            AccessibilityTree = captureDOM ? await adapter.GetAccessibilityTreeAsync(cancellationToken) : null,

            GameState = await adapter.GetGameStateAsync(cancellationToken),
            VisibleObjects = await adapter.GetVisibleObjectsAsync(cancellationToken),
            PlayerState = await adapter.GetPlayerStateAsync(cancellationToken),

            LastApiResponse = await adapter.GetLastApiResponseAsync(cancellationToken),
            AvailableEndpoints = await adapter.GetAvailableEndpointsAsync(cancellationToken),

            TerminalOutput = await adapter.GetTerminalOutputAsync(cancellationToken),
            CurrentPrompt = await adapter.GetCurrentPromptAsync(cancellationToken),

            ConsoleLog = await adapter.GetConsoleLogAsync(cancellationToken),
            Errors = await adapter.GetErrorsAsync(cancellationToken),
            Warnings = await adapter.GetWarningsAsync(cancellationToken),
            Performance = capturePerformance ? await adapter.GetPerformanceAsync(cancellationToken) : null,
            CurrentUrl = await adapter.GetCurrentUrlAsync(cancellationToken),
            WindowTitle = await adapter.GetWindowTitleAsync(cancellationToken),
        };

        if (state.Screenshot != null && state.PreviousScreenshot != null)
        {
            state = state with
            {
                VisualChangePercent = CalculateVisualChange(state.Screenshot, state.PreviousScreenshot)
            };
        }

        return state;
    }

    private async Task<IReadOnlyList<InteractiveElement>> AugmentWithVisionAsync(
        PerceptionState state,
        IExecutionContext context,
        string? modelOverride,
        CancellationToken cancellationToken)
    {
        if (_providerFactory == null || state.Screenshot == null)
            return Array.Empty<InteractiveElement>();

        var prompt = BuildVisionAugmentPrompt();
        var response = await _providerFactory.ExecuteVisionAsync(
            context.Provider,
            "You are a UI analyzer. Identify interactive elements (buttons, inputs, links, menus) from the screenshot. Return only valid JSON.",
            prompt,
            state.Screenshot,
            modelOverride != null ? new { model = modelOverride } : new { },
            cancellationToken);

        return ParseVisionElements(response);
    }

    private static string BuildVisionAugmentPrompt()
    {
        return """
            Look at the screenshot. Identify all interactive UI elements: buttons, text inputs, links, menus, tabs, etc.
            For each element provide approximate pixel coordinates (origin top-left of image).

            Return JSON only (no markdown):
            {
              "elements": [
                { "id": "btn_send", "type": "button", "label": "Send", "x": 820, "y": 580, "width": 60, "height": 30 },
                { "id": "input_chat", "type": "input", "label": "Message", "x": 590, "y": 570, "width": 200, "height": 40 }
              ]
            }
            Use "id" as unique identifier, "type" as button/input/link/menu/tab/etc, "label" as visible text.
            """;
    }

    private static IReadOnlyList<InteractiveElement> ParseVisionElements(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("elements", out var arr))
                return Array.Empty<InteractiveElement>();

            var list = new List<InteractiveElement>();
            foreach (var el in arr.EnumerateArray())
            {
                var id = el.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
                var type = el.TryGetProperty("type", out var t) ? t.GetString() ?? "button" : "button";
                var label = el.TryGetProperty("label", out var l) ? l.GetString() : null;
                var x = el.TryGetProperty("x", out var xProp) ? xProp.GetDouble() : 0;
                var y = el.TryGetProperty("y", out var yProp) ? yProp.GetDouble() : 0;
                var w = el.TryGetProperty("width", out var wProp) ? wProp.GetDouble() : 40;
                var h = el.TryGetProperty("height", out var hProp) ? hProp.GetDouble() : 40;

                list.Add(new InteractiveElement
                {
                    Id = id,
                    Type = type,
                    Label = label,
                    Description = label,
                    Bounds = new BoundingBox { X = x, Y = y, Width = w, Height = h },
                    IsVisible = true,
                    IsEnabled = true
                });
            }
            return list;
        }
        catch
        {
            return Array.Empty<InteractiveElement>();
        }
    }
    
    private static double CalculateVisualChange(byte[] current, byte[] previous)
    {
        try
        {
            using var currentImage = Image.Load<Rgba32>(current);
            using var previousImage = Image.Load<Rgba32>(previous);
            
            if (currentImage.Width != previousImage.Width || currentImage.Height != previousImage.Height)
                return 1.0; // Completely different
            
            int differentPixels = 0;
            int totalPixels = currentImage.Width * currentImage.Height;
            int sampleRate = Math.Max(1, totalPixels / 10000); // Sample ~10k pixels max
            
            for (int i = 0; i < totalPixels; i += sampleRate)
            {
                int x = i % currentImage.Width;
                int y = i / currentImage.Width;
                
                if (!PixelsMatch(currentImage[x, y], previousImage[x, y]))
                    differentPixels++;
            }
            
            return (double)differentPixels / (totalPixels / sampleRate);
        }
        catch
        {
            return 0.0; // Can't compare
        }
    }
    
    private static bool PixelsMatch(Rgba32 a, Rgba32 b, int tolerance = 10)
    {
        return Math.Abs(a.R - b.R) <= tolerance &&
               Math.Abs(a.G - b.G) <= tolerance &&
               Math.Abs(a.B - b.B) <= tolerance;
    }
}
