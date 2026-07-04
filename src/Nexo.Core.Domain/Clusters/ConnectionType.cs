namespace Nexo.Core.Domain.Clusters;

/// <summary>Semantic type of a connection between cluster bricks or workflow ports.</summary>
public enum ConnectionType
{
    /// <summary>Output-to-input data flow between bricks.</summary>
    Data,

    /// <summary>Execution-order or control-flow edge.</summary>
    Control,

    /// <summary>Event trigger or notification edge.</summary>
    Event
}
