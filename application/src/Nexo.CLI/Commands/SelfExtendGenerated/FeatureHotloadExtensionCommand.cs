using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.SelfExtendGenerated;

public sealed class FeatureHotloadExtensionCommand : Command, IComposableExtensionCommand
{
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

    public string ExtensionId => "feature-hotload";
    public IReadOnlyList<string> Dependencies { get; } = new[] { "domain-knowledge", "ui-shell", "ui-workflow" };
}