using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Ashlar.CLI.Commands;

namespace Ashlar.CLI;

/// <summary>Program.</summary>
static partial class Program
{
    private static Command BuildTrustCommand(Option<bool> jsonOpt)
    {
        // ashlar trust - Audit and access boundary (Phase 4)
        var trustCmd = new Command("trust", "Trust & Information Architecture: audit log and access boundary");
        var auditCountOpt = new Option<int>("--count", () => 50, "Max entries to show");
        var auditSinceOpt = new Option<string?>("--since", "Filter by time (e.g. 1h, 30m, or ISO date)");
        var auditUntilOpt = new Option<string?>("--until", "Filter until time (e.g. 1h, 30m, or ISO date)");
        var auditTypeOpt = new Option<string?>("--type", "Filter by event type (Sanitization, BoundaryChange, Classification)");
        var auditJsonOpt = new Option<bool>("--json", "Export as JSON (compliance format)");
        var auditMdOpt = new Option<bool>("--md", "Export as Markdown");
        var auditCsvOpt = new Option<bool>("--csv", "Export as CSV (compliance)");
        var trustAuditCmd = new Command("audit", "Show or export data decision audit log")
        {
            auditCountOpt, auditSinceOpt, auditUntilOpt, auditTypeOpt, auditJsonOpt, auditMdOpt, auditCsvOpt
        };
        trustAuditCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
            var json = CommandExecutionSupport.WantsJson(ctx.ParseResult, auditJsonOpt);
            Environment.Exit(await cmd.AuditAsync(
                ctx.ParseResult.GetValueForOption(auditCountOpt),
                ctx.ParseResult.GetValueForOption(auditSinceOpt),
                ctx.ParseResult.GetValueForOption(auditUntilOpt),
                ctx.ParseResult.GetValueForOption(auditTypeOpt),
                json,
                ctx.ParseResult.GetValueForOption(auditMdOpt),
                ctx.ParseResult.GetValueForOption(auditCsvOpt)));
        });
        trustCmd.AddCommand(trustAuditCmd);
        var trustPauseCmd = new Command("pause", "Pause observation (halt all data collection)");
        trustPauseCmd.AddOption(jsonOpt);
        trustPauseCmd.SetHandler(
            /// <summary>Async.</summary>
            /// <param name="formatJson">Format json.</param>
            async (bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.PauseAsync(formatJson));
            },
            jsonOpt);
        trustCmd.AddCommand(trustPauseCmd);
        var trustResumeCmd = new Command("resume", "Resume observation");
        trustResumeCmd.AddOption(jsonOpt);
        trustResumeCmd.SetHandler(
            /// <summary>Async.</summary>
            /// <param name="formatJson">Format json.</param>
            async (bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.ResumeAsync(formatJson));
            },
            jsonOpt);
        trustCmd.AddCommand(trustResumeCmd);
        var trustAllowCmd = new Command("allow", "Allow a category or source");
        trustAllowCmd.AddOption(new Option<string?>("--category", "Category to allow (e.g. file-paths, terminal-output)"));
        trustAllowCmd.AddOption(new Option<string?>("--source", "Source to allow (e.g. git, vscode)"));
        trustAllowCmd.AddOption(new Option<string?>("--project", "Project path for override (requires --source)"));
        trustAllowCmd.AddOption(jsonOpt);
        trustAllowCmd.SetHandler(
            /// <summary>Async.</summary>
            /// <param name="category">Category.</param>
            /// <param name="source">Source.</param>
            /// <param name="project">Project.</param>
            /// <param name="formatJson">Format json.</param>
            async (string? category, string? source, string? project, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.AllowAsync(category, source, project, formatJson));
            },
            trustAllowCmd.Options[0] as Option<string?> ?? throw new InvalidOperationException(),
            trustAllowCmd.Options[1] as Option<string?> ?? throw new InvalidOperationException(),
            trustAllowCmd.Options[2] as Option<string?> ?? throw new InvalidOperationException(),
            trustAllowCmd.Options[3] as Option<bool> ?? throw new InvalidOperationException());
        trustCmd.AddCommand(trustAllowCmd);
        var trustDenyCmd = new Command("deny", "Deny a category or source");
        trustDenyCmd.AddOption(new Option<string?>("--category", "Category to deny"));
        trustDenyCmd.AddOption(new Option<string?>("--source", "Source to deny"));
        trustDenyCmd.AddOption(new Option<string?>("--project", "Project path for override (requires --source)"));
        trustDenyCmd.AddOption(jsonOpt);
        trustDenyCmd.SetHandler(
            /// <summary>Async.</summary>
            /// <param name="category">Category.</param>
            /// <param name="source">Source.</param>
            /// <param name="project">Project.</param>
            /// <param name="formatJson">Format json.</param>
            async (string? category, string? source, string? project, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.DenyAsync(category, source, project, formatJson));
            },
            trustDenyCmd.Options[0] as Option<string?> ?? throw new InvalidOperationException(),
            trustDenyCmd.Options[1] as Option<string?> ?? throw new InvalidOperationException(),
            trustDenyCmd.Options[2] as Option<string?> ?? throw new InvalidOperationException(),
            trustDenyCmd.Options[3] as Option<bool> ?? throw new InvalidOperationException());
        trustCmd.AddCommand(trustDenyCmd);
        var trustBoundaryCmd = new Command("boundary", "Show access boundary status");
        trustBoundaryCmd.SetHandler(
            /// <summary>Async.</summary>
            /// <param name="formatJson">Format json.</param>
            async (bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.BoundaryAsync(formatJson));
            },
            jsonOpt);
        trustCmd.AddCommand(trustBoundaryCmd);
        var trustDashboardCmd = new Command("dashboard", "Compliance dashboard: boundary status + audit summary");
        trustDashboardCmd.AddOption(new Option<int>("--count", () => 50, "Max audit entries to include"));
        trustDashboardCmd.AddOption(jsonOpt);
        trustDashboardCmd.SetHandler(
            /// <summary>Async.</summary>
            /// <param name="count">Count.</param>
            /// <param name="formatJson">Format json.</param>
            async (int count, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.DashboardAsync(count, formatJson));
            },
            trustDashboardCmd.Options[0] as Option<int> ?? throw new InvalidOperationException(),
            trustDashboardCmd.Options[1] as Option<bool> ?? throw new InvalidOperationException());
        trustCmd.AddCommand(trustDashboardCmd);

        var trustPackCmd = new Command("pack", "Manage regulated trust policy packs");
        var trustPackListCmd = new Command("list", "List available trust policy packs");
        trustPackListCmd.AddOption(jsonOpt);
        trustPackListCmd.SetHandler(
            /// <summary>Async.</summary>
            /// <param name="formatJson">Format json.</param>
            async (bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.ListPolicyPacksAsync(formatJson));
            },
            jsonOpt);
        trustPackCmd.AddCommand(trustPackListCmd);

        var trustPackDescribeCmd = new Command("describe", "Show rules for a trust policy pack");
        var trustPackDescribeIdArg = new Argument<string>("packId", "Policy pack id");
        trustPackDescribeCmd.AddArgument(trustPackDescribeIdArg);
        trustPackDescribeCmd.AddOption(jsonOpt);
        trustPackDescribeCmd.SetHandler(
            /// <summary>Async.</summary>
            /// <param name="packId">Pack id.</param>
            /// <param name="formatJson">Format json.</param>
            async (string packId, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.DescribePolicyPackAsync(packId, formatJson));
            },
            trustPackDescribeIdArg,
            jsonOpt);
        trustPackCmd.AddCommand(trustPackDescribeCmd);

        var trustPackApplyCmd = new Command("apply", "Apply a trust policy pack by id");
        var trustPackIdOpt = new Option<string>("--id", "Policy pack id (strict-enterprise, internal-only, air-gapped)");
        trustPackIdOpt.IsRequired = true;
        trustPackApplyCmd.AddOption(trustPackIdOpt);
        trustPackApplyCmd.AddOption(jsonOpt);
        trustPackApplyCmd.SetHandler(
            /// <summary>Async.</summary>
            /// <param name="id">Id.</param>
            /// <param name="formatJson">Format json.</param>
            async (string id, bool formatJson) =>
            {
                var cmd = ServiceProvider.GetRequiredService<TrustCommand>();
                Environment.Exit(await cmd.ApplyPolicyPackAsync(id, formatJson));
            },
            trustPackIdOpt,
            jsonOpt);
        trustPackCmd.AddCommand(trustPackApplyCmd);
        trustCmd.AddCommand(trustPackCmd);
        return trustCmd;
    }
}
