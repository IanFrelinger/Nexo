using System.Text.RegularExpressions;
using Ashlar.Core.Application.Orchestration;
using Ashlar.Core.Domain.Bricks.Ports;

namespace Ashlar.Bricks.SqlProfile;

/// <summary>
/// In-process SQL syntax smoke validator — no container, no sandbox.
/// Accepts a single statement starting with a known verb.
/// </summary>
public sealed class SqlParseValidator : IPostValidator<GeneratedArtifact>
{
    private static readonly Regex StatementStart = new(
        @"^\s*(SELECT|INSERT|UPDATE|DELETE|WITH|CREATE|DROP|EXPLAIN)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <inheritdoc />
    public ValueTask<(bool ok, string? reason)> ValidateAsync(
        GeneratedArtifact output,
        CancellationToken ct)
    {
        if (output is null || string.IsNullOrWhiteSpace(output.Content))
            return ValueTask.FromResult<(bool, string?)>((false, "SQL artifact is empty"));

        var sql = output.Content.Trim().TrimEnd(';');
        if (!StatementStart.IsMatch(sql))
        {
            return ValueTask.FromResult<(bool, string?)>((false,
                "SQL must start with SELECT/INSERT/UPDATE/DELETE/WITH/CREATE/DROP/EXPLAIN"));
        }

        // Cheap balance check for parentheses — enough for the acceptance loop.
        var depth = 0;
        foreach (var ch in sql)
        {
            if (ch == '(') depth++;
            else if (ch == ')') depth--;
            if (depth < 0)
                return ValueTask.FromResult<(bool, string?)>((false, "Unbalanced closing parenthesis"));
        }

        if (depth != 0)
            return ValueTask.FromResult<(bool, string?)>((false, "Unbalanced parentheses"));

        return ValueTask.FromResult<(bool, string?)>((true, null));
    }
}
