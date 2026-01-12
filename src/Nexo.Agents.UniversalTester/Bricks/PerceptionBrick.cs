using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester.Adapters;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Nexo.Agents.UniversalTester.Bricks;

/// <summary>
/// Captures current state from any target application.
/// </summary>
public class PerceptionBrick : Brick
{
    private readonly ILogger<PerceptionBrick>? _logger;
    
    public PerceptionBrick(ILogger<PerceptionBrick>? logger = null)
    {
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
                new BrickInputDefinition("capturePerformance", "bool", "Whether to capture performance metrics", required: false, defaultValue: true)
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
            }
        };
        
        DefaultImplementation = ImplementationType.Deterministic;
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
        
        var state = new PerceptionState
        {
            Timestamp = DateTime.UtcNow,
            
            // Visual
            Screenshot = captureScreenshot ? await adapter.CaptureScreenshotAsync(cancellationToken) : null,
            PreviousScreenshot = previousState?.Screenshot,
            
            // Structural
            DomSnapshot = captureDOM ? await adapter.GetStructureAsync(cancellationToken) : null,
            InteractiveElements = await adapter.GetInteractiveElementsAsync(cancellationToken),
            AccessibilityTree = captureDOM ? await adapter.GetAccessibilityTreeAsync(cancellationToken) : null,
            
            // Game
            GameState = await adapter.GetGameStateAsync(cancellationToken),
            VisibleObjects = await adapter.GetVisibleObjectsAsync(cancellationToken),
            PlayerState = await adapter.GetPlayerStateAsync(cancellationToken),
            
            // API
            LastApiResponse = await adapter.GetLastApiResponseAsync(cancellationToken),
            AvailableEndpoints = await adapter.GetAvailableEndpointsAsync(cancellationToken),
            
            // CLI
            TerminalOutput = await adapter.GetTerminalOutputAsync(cancellationToken),
            CurrentPrompt = await adapter.GetCurrentPromptAsync(cancellationToken),
            
            // Universal
            ConsoleLog = await adapter.GetConsoleLogAsync(cancellationToken),
            Errors = await adapter.GetErrorsAsync(cancellationToken),
            Warnings = await adapter.GetWarningsAsync(cancellationToken),
            Performance = capturePerformance ? await adapter.GetPerformanceAsync(cancellationToken) : null,
            CurrentUrl = await adapter.GetCurrentUrlAsync(cancellationToken),
            WindowTitle = await adapter.GetWindowTitleAsync(cancellationToken),
        };
        
        // Calculate visual change if we have previous state
        if (state.Screenshot != null && state.PreviousScreenshot != null)
        {
            state = state with
            {
                VisualChangePercent = CalculateVisualChange(state.Screenshot, state.PreviousScreenshot)
            };
        }
        
        return new BrickOutput
        {
            ["perception"] = state,
            Summary = $"Captured {state.InteractiveElements.Count} interactive elements, {state.Errors.Count} errors"
        };
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
