using System.CommandLine;

namespace Ashlar.CLI.Commands.SelfExtendGenerated;

/// <summary>CLI command for self extend ui demo bundle.</summary>
public sealed class SelfExtendUiDemoBundleCommand : Command
{
    /// <summary>Creates a new SelfExtendUiDemoBundleCommand instance.</summary>
    public SelfExtendUiDemoBundleCommand() : base("self-extend-ui-demo-bundle", "Composed bundle of generated extension commands")
    {
        AddCommand(new DomainKnowledgeExtensionCommand());
        AddCommand(new UiShellExtensionCommand());
        AddCommand(new UiWorkflowExtensionCommand());
        AddCommand(new FeatureHotloadExtensionCommand());
    }
}