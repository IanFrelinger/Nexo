using System.CommandLine;

namespace Nexo.CLI.Commands.SelfExtendGenerated;

public sealed class SelfExtendUiDemoBundleCommand : Command
{
    public SelfExtendUiDemoBundleCommand() : base("self-extend-ui-demo-bundle", "Composed bundle of generated extension commands")
    {
        AddCommand(new DomainKnowledgeExtensionCommand());
        AddCommand(new UiShellExtensionCommand());
        AddCommand(new UiWorkflowExtensionCommand());
        AddCommand(new FeatureHotloadExtensionCommand());
    }
}