using System.CommandLine;
using System.Text.Json;

namespace Ashlar.CLI.Commands.SelfExtendGenerated;

/// <summary>CLI command for domain knowledge extension.</summary>
public sealed class DomainKnowledgeExtensionCommand : Command, IComposableExtensionCommand
{
    /// <summary>Creates a new DomainKnowledgeExtensionCommand instance.</summary>
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

    /// <summary>Stable extension identifier for composition graphs.</summary>
    public string ExtensionId => "domain-knowledge";
    /// <summary>Dependencies.</summary>
    public IReadOnlyList<string> Dependencies { get; } = Array.Empty<string>();
}