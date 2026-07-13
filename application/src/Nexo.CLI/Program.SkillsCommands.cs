using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Nexo.CLI.Commands;

namespace Nexo.CLI;

/// <summary>Program.</summary>
static partial class Program
{
    private static Command BuildSkillsCommand(Option<bool> jsonOpt)
    {
        var skillsCmd = new Command("skills", "Inspect and govern Nexo agent skills");

        var listCmd = new Command("list", "Advertise visible skills for a trust tier")
        {
            new Option<string>("--tier", () => "Trusted", "Trust tier (Trusted, Untrusted, Unknown)"),
            jsonOpt,
        };
        listCmd.SetHandler(
            async (string tier, bool json) =>
            {
                var cmd = ServiceProvider.GetRequiredService<SkillsCommand>();
                Environment.Exit(await cmd.ListAsync(tier, json));
            },
            listCmd.Options[0] as Option<string> ?? throw new InvalidOperationException(),
            jsonOpt);
        skillsCmd.AddCommand(listCmd);

        var loadCmd = new Command("load", "Load full skill instructions (SKILL.md)")
        {
            new Argument<string>("name", "Skill name"),
            new Option<string>("--tier", () => "Trusted", "Trust tier"),
        };
        loadCmd.SetHandler(
            async (string name, string tier) =>
            {
                var cmd = ServiceProvider.GetRequiredService<SkillsCommand>();
                Environment.Exit(await cmd.LoadAsync(name, tier));
            },
            loadCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException(),
            loadCmd.Options[0] as Option<string> ?? throw new InvalidOperationException());
        skillsCmd.AddCommand(loadCmd);

        var approvalsCmd = new Command("approvals", "Human-in-the-loop skill script approvals");
        var approvalsListCmd = new Command("list", "List pending skill script approvals")
        {
            jsonOpt,
        };
        approvalsListCmd.SetHandler(
            async (bool json) =>
            {
                var cmd = ServiceProvider.GetRequiredService<SkillsCommand>();
                Environment.Exit(await cmd.ListApprovalsAsync(json));
            },
            jsonOpt);
        approvalsCmd.AddCommand(approvalsListCmd);

        var approveCmd = new Command("approve", "Approve a pending skill script")
        {
            new Argument<string>("request-id", "Pending approval request id"),
            jsonOpt,
        };
        approveCmd.SetHandler(
            async (string requestId, bool json) =>
            {
                var cmd = ServiceProvider.GetRequiredService<SkillsCommand>();
                Environment.Exit(await cmd.ApproveAsync(requestId, json));
            },
            approveCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException(),
            jsonOpt);
        approvalsCmd.AddCommand(approveCmd);

        var denyCmd = new Command("deny", "Deny a pending skill script")
        {
            new Argument<string>("request-id", "Pending approval request id"),
            jsonOpt,
        };
        denyCmd.SetHandler(
            async (string requestId, bool json) =>
            {
                var cmd = ServiceProvider.GetRequiredService<SkillsCommand>();
                Environment.Exit(await cmd.DenyAsync(requestId, json));
            },
            denyCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException(),
            jsonOpt);
        approvalsCmd.AddCommand(denyCmd);

        skillsCmd.AddCommand(approvalsCmd);
        return skillsCmd;
    }
}
