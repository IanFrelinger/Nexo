using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Playtest.Models;
using Ashlar.Orchestration.Playtest.Ports;
using System.Text;
using System.Text.Json;

namespace Ashlar.Orchestration.Agents.Playtest;

/// <summary>
/// AI-driven player that makes gameplay decisions using LLM reasoning.
/// 
/// Responsibilities:
/// - Simulates player behavior using LLM (IModel)
/// - Makes gameplay decisions based on game state
/// - Executes actions via IGameRunner
/// - Records telemetry for analysis
/// - Supports different player profiles (casual, competitive, etc.)
/// 
/// Used in automated playtesting to generate realistic player behavior.
/// Inherits from BaseAgent for lifecycle management.
/// </summary>
public sealed class AIPlayerAgent : BaseAgent
{
    private readonly IGameRunner _gameRunner;
    private readonly IModel _model;
    private readonly string _profile;

    public AIPlayerAgent(
        AgentSpawnSpec spec,
        IGameRunner gameRunner,
        IModel model,
        string profile,
        ILogger<BaseAgent> logger) : base(spec, logger)
    {
        _gameRunner = gameRunner ?? throw new ArgumentNullException(nameof(gameRunner));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _profile = profile;
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Initializing AI player agent: {AgentId} with profile {Profile}", Spec.AgentId, _profile);
        return Task.CompletedTask;
    }

    protected override Task OnDependenciesResolvedAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("Dependencies resolved for {AgentId}", Spec.AgentId);
        return Task.CompletedTask;
    }

    protected override async Task<object> OnExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs,
        CancellationToken cancellationToken)
    {
        var actions = new List<PlayerAction>();
        var startTime = DateTimeOffset.UtcNow;
        var maxDuration = TimeSpan.FromMinutes(10);

        while (!cancellationToken.IsCancellationRequested &&
               DateTimeOffset.UtcNow - startTime < maxDuration)
        {
            // Get current game state
            var gameState = await _gameRunner.GetGameStateAsync(cancellationToken);

            if (gameState.IsGameOver)
            {
                Logger.LogInformation("Game over detected, ending AI player session");
                break;
            }

            // Decide next action using LLM
            var decision = await DecideActionAsync(gameState, actions, cancellationToken);

            // Execute action in game
            await _gameRunner.ExecuteActionAsync(decision.ActionType, decision.Parameters, cancellationToken);

            actions.Add(decision);

            // Small delay to simulate human reaction time
            await Task.Delay(100 + Random.Shared.Next(200), cancellationToken);
        }

        return new AIPlayerSession
        {
            PlayerId = Spec.AgentId,
            Profile = _profile,
            Actions = actions,
            Metrics = CalculateMetrics(actions, DateTimeOffset.UtcNow - startTime)
        };
    }

    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Shutting down AI player agent: {AgentId}", Spec.AgentId);
        return Task.CompletedTask;
    }

    private async Task<PlayerAction> DecideActionAsync(
        GameState state,
        IReadOnlyList<PlayerAction> previousActions,
        CancellationToken cancellationToken)
    {
        var systemPrompt = GetSystemPrompt();
        var userPrompt = BuildDecisionPrompt(state, previousActions);

        var input = new ModelInput(new[]
        {
            ("system", systemPrompt),
            ("user", userPrompt)
        });

        var output = await _model.CompleteAsync(input, cancellationToken);
        return ParseAction(output.Text, state);
    }

    private string GetSystemPrompt()
    {
        var basePrompt = """
            You are an AI playing a video game for testing purposes.
            Your goal is to thoroughly test the game mechanics and find issues.

            Respond with a JSON action:
            {
                "action": "action_name",
                "parameters": { ... },
                "reasoning": "brief explanation"
            }
            """;

        var profileAdditions = _profile.ToLowerInvariant() switch
        {
            "aggressive" => "\nPlay aggressively - engage combat frequently, take risks.",
            "passive" => "\nPlay defensively - avoid combat, focus on exploration.",
            "explorer" => "\nExplore thoroughly - visit all areas, interact with everything.",
            "speedrunner" => "\nMove quickly - find optimal paths, skip optional content.",
            "completionist" => "\nDo everything - complete all objectives, collect all items.",
            _ => ""
        };

        return basePrompt + profileAdditions;
    }

    private string BuildDecisionPrompt(GameState state, IReadOnlyList<PlayerAction> previousActions)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## Current Game State");
        sb.AppendLine($"- Health: {state.PlayerHealth}/{state.PlayerMaxHealth}");
        sb.AppendLine($"- Position: {state.PlayerPosition}");
        sb.AppendLine($"- Current Objective: {state.CurrentObjective ?? "None"}");
        sb.AppendLine($"- Currency: {state.Currency}");
        sb.AppendLine($"- Equipped Weapon: {state.EquippedWeapon ?? "None"}");

        if (state.NearbyEnemies.Any())
        {
            sb.AppendLine($"- Nearby Enemies: {string.Join(", ", state.NearbyEnemies)}");
        }

        if (state.NearbyItems.Any())
        {
            sb.AppendLine($"- Nearby Items: {string.Join(", ", state.NearbyItems)}");
        }

        sb.AppendLine();
        sb.AppendLine("## Available Actions");
        foreach (var action in state.AvailableActions)
        {
            sb.AppendLine($"- {action}");
        }

        if (previousActions.Any())
        {
            sb.AppendLine();
            sb.AppendLine("## Recent Actions");
            foreach (var action in previousActions.TakeLast(5))
            {
                sb.AppendLine($"- {action.ActionType}: {action.Reasoning ?? "No reasoning"}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("What action should you take next?");

        return sb.ToString();
    }

    private PlayerAction ParseAction(string output, GameState state)
    {
        try
        {
            var json = output;
            if (json.Contains("```"))
            {
                var start = json.IndexOf('{');
                var end = json.LastIndexOf('}');
                if (start >= 0 && end > start)
                {
                    json = json.Substring(start, end - start + 1);
                }
            }

            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var actionType = root.GetProperty("action").GetString() ?? "wait";
            var parameters = new Dictionary<string, object>();

            if (root.TryGetProperty("parameters", out var paramsElement))
            {
                foreach (var prop in paramsElement.EnumerateObject())
                {
                    parameters[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.Number => prop.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => prop.Value.GetString() ?? ""
                    };
                }
            }

            var reasoning = root.TryGetProperty("reasoning", out var r)
                ? r.GetString() : null;

            return new PlayerAction
            {
                ActionType = actionType,
                Timestamp = DateTimeOffset.UtcNow,
                Parameters = parameters,
                Reasoning = reasoning
            };
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to parse AI decision, defaulting to wait");
            return new PlayerAction
            {
                ActionType = "wait",
                Timestamp = DateTimeOffset.UtcNow,
                Reasoning = "Parse error fallback"
            };
        }
    }

    private PlayerMetrics CalculateMetrics(IReadOnlyList<PlayerAction> actions, TimeSpan duration)
    {
        return new PlayerMetrics
        {
            TotalActions = actions.Count,
            Kills = actions.Count(a => a.ActionType == "attack" &&
                a.Parameters.TryGetValue("killed", out var k) && k is true),
            Deaths = actions.Count(a => a.ActionType == "respawn"),
            ObjectivesCompleted = actions.Count(a => a.ActionType == "complete_objective"),
            TotalPlayTime = duration
        };
    }
}

