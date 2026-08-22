using System.Text.Json.Serialization;

namespace Ashlar.BackgroundAgents.Observations;

/// <summary>
/// Categorises observations so consumers can subscribe to a slice without
/// parsing arbitrary summary strings.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ObservationKind
{
    /// <summary>Result of running a build (e.g. <c>dotnet build</c>) on some project.</summary>
    Build,

    /// <summary>Result of running a test slice.</summary>
    Test,

    /// <summary>Result of running static analysis / linting.</summary>
    Analysis,

    /// <summary>An action an agent took that other agents may want to know about (file written, PR opened, etc.).</summary>
    AgentAction,

    /// <summary>An external signal (operator command, mode change, manual intervention).</summary>
    UserSignal
}
