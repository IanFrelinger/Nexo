namespace Nexo.CLI.Commands.SelfExtendGenerated;

public interface IComposableExtensionCommand
{
    string ExtensionId { get; }
    IReadOnlyList<string> Dependencies { get; }
}