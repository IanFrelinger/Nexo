namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Provides the local instance's peer ID for mesh attribution (P2.2).
/// </summary>
public interface IPeerIdProvider
{
    /// <summary>
    /// Gets this instance's peer ID (used as SourcePeerId when broadcasting).
    /// </summary>
    string GetPeerId();
}
