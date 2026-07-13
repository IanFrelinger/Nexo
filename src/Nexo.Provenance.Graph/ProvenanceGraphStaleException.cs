namespace Nexo.Provenance.Graph;

/// <summary>
/// Fail-closed error when graph chain-head does not match the current authority chain head.
/// </summary>
public sealed class ProvenanceGraphStaleException : Exception
{
    public ProvenanceGraphStaleException(string graphChainHead, string currentChainHead)
        : base($"Provenance graph is stale: graph chain-head '{graphChainHead}' does not match current chain-head '{currentChainHead}'.")
    {
        GraphChainHead = graphChainHead;
        CurrentChainHead = currentChainHead;
    }

    public string GraphChainHead { get; }
    public string CurrentChainHead { get; }
}
