using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Tools.Visual.Contracts;

namespace Nexo.Agent.Tools.Visual.Implementations;

/// <summary>
/// Unity-specific visual analyzer for game screenshots and UI analysis.
/// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
/// </summary>
public sealed partial class UnityVisualAnalyzer : IVisualAnalyzer
{
    private readonly ILogger<UnityVisualAnalyzer> _logger;
    private readonly IVisualAnalyzer _baseAnalyzer;

    public UnityVisualAnalyzer(ILogger<UnityVisualAnalyzer> logger, IVisualAnalyzer baseAnalyzer)
    {
        _logger = logger;
        _baseAnalyzer = baseAnalyzer;
    }

    public string Id => "tool.unity.visual.analyze";
    public string Name => "Unity Visual Analyzer";
    public string Description => "Analyzes Unity game screenshots for gameplay, UI, and performance insights";
    public string Version => "1.0.0";
    public ToolPermissions Permissions => ToolPermissions.FileRead;

}