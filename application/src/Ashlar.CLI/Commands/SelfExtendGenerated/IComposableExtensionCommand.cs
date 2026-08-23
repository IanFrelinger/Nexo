namespace Ashlar.CLI.Commands.SelfExtendGenerated;

/// <summary>Contract for composable extension command.</summary>
public interface IComposableExtensionCommand
{
    /// <summary>Extension id.</summary>
    string ExtensionId { get; }
    /// <summary>Dependencies.</summary>
    IReadOnlyList<string> Dependencies { get; }
}