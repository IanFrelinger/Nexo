using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.SelfExtendGenerated;

public sealed class DomainKnowledgeExtensionCommand : Command, IComposableExtensionCommand
{
    public DomainKnowledgeExtensionCommand() : base("ext-domain-knowledge", "Self-extend generated extension command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new
            {
                ok = true,
                command = Name,
                extensionId = ExtensionId,
                dependencies = Dependencies
            }));
            Environment.ExitCode = 0;
        });
    }

    public string ExtensionId => "domain-knowledge";
    public IReadOnlyList<string> Dependencies { get; } = Array.Empty<string>();
}