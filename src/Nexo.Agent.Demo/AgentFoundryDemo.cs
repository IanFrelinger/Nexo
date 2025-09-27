using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;

namespace Nexo.Agent.Demo;

/// <summary>
/// Main AgentFoundryDemo class that orchestrates the demo functionality
/// </summary>
public partial class AgentFoundryDemo
{
    private readonly ITaskExecutionAgent _agent;
    private readonly ILogger _logger;
    private string _currentMode = "Interactive";
    private bool _selfHealEnabled = true;

    public AgentFoundryDemo(ITaskExecutionAgent agent, ILogger logger)
    {
        _agent = agent;
        _logger = logger;
    }

    public ITaskExecutionAgent Agent => _agent;
    public ILogger Logger => _logger;
    public string CurrentMode => _currentMode;
    public bool SelfHealEnabled => _selfHealEnabled;
}
