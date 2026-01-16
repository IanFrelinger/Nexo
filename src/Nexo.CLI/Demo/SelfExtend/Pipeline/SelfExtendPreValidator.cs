using Nexo.Core.Application.Orchestration;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline;

public sealed class SelfExtendPreValidator : IPreValidator
{
    public ValueTask<(bool ok, string? reason)> ValidateAsync(object input, CancellationToken ct)
    {
        if (input is not SelfExtendContext ctx)
        {
            return ValueTask.FromResult((false, (string?)"input must be SelfExtendContext"));
        }

        if (string.IsNullOrWhiteSpace(ctx.RepoRoot) || !new DirectoryInfo(ctx.RepoRoot).Exists)
        {
            return ValueTask.FromResult((false, (string?)"RepoRoot missing or does not exist"));
        }

        if (string.IsNullOrWhiteSpace(ctx.Spec.CommandName))
        {
            return ValueTask.FromResult((false, (string?)"CommandName missing"));
        }

        // allow only [a-z0-9-] for generated demo commands
        foreach (var ch in ctx.Spec.CommandName)
        {
            var ok = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '-';
            if (!ok) return ValueTask.FromResult((false, (string?)$"CommandName contains invalid char '{ch}'"));
        }

        return ValueTask.FromResult((true, (string?)null));
    }
}

