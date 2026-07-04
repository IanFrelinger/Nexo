using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.SelfExtendGenerated;

/// <summary>CLI command for feature hotload extension.</summary>
public sealed class FeatureHotloadExtensionCommand : Command, IComposableExtensionCommand
{
    /// <summary>Creates a new FeatureHotloadExtensionCommand instance.</summary>
    public FeatureHotloadExtensionCommand() : base("ext-feature-hotload", "Self-extend generated extension command")
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
    public string ExtensionId => "feature-hotload";
    /// <summary>Dependencies.</summary>
    public IReadOnlyList<string> Dependencies { get; } = new[] { "domain-knowledge", "ui-shell", "ui-workflow" };
}