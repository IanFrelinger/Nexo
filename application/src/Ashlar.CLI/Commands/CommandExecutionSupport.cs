using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Logging;
using Ashlar.CLI.Formatting;
using Ashlar.Core.Application.Common.Models;

namespace Ashlar.CLI.Commands;

/// <summary>Command execution support.</summary>
internal static class CommandExecutionSupport
{
    /// <summary>
    /// True when the caller asked for machine-readable output with the root's global
    /// <c>--format-json</c>.
    ///
    /// <para>The option is located by ALIAS, never by name alone. System.CommandLine strips the
    /// leading <c>--</c> from an option's Name, so a <c>o.Name == "--format-json"</c> lookup
    /// matches nothing and reports "no JSON asked for" on every invocation. Both spellings are
    /// matched here so this helper cannot fail the same silent way. <c>docker</c>,
    /// <c>test portable</c>, and <c>test multi-env</c> call this helper.</para>
    /// </summary>
    internal static bool WantsJson(ParseResult parseResult)
    {
        var formatJson = parseResult.RootCommandResult.Command.Options
            .OfType<Option<bool>>()
            .FirstOrDefault(o => o.HasAlias("--format-json")
                              || string.Equals(o.Name, "format-json", StringComparison.Ordinal));

        return formatJson is not null && parseResult.GetValueForOption(formatJson);
    }

    /// <summary>
    /// True when the caller asked for verbose output with the root's global <c>--verbose</c>.
    ///
    /// <para>Same alias lookup as <see cref="WantsJson"/>. A <c>o.Name == "--verbose"</c> test
    /// matches nothing because System.CommandLine strips the leading <c>--</c>.</para>
    /// </summary>
    internal static bool WantsVerbose(ParseResult parseResult)
    {
        var verbose = parseResult.RootCommandResult.Command.Options
            .OfType<Option<bool>>()
            .FirstOrDefault(o => o.HasAlias("--verbose")
                              || string.Equals(o.Name, "verbose", StringComparison.Ordinal));

        return verbose is not null && parseResult.GetValueForOption(verbose);
    }

    /// <summary>
    /// True when the caller asked for JSON via a command-local bool option
    /// (usually <c>--json</c>) OR the root global <c>--format-json</c>.
    ///
    /// <para>A local <c>GetValueForOption(jsonOpt)</c> alone drops a leading
    /// <c>--format-json command …</c> because that token binds the root option,
    /// not the command-local one. Every JSON rendering that spells its own
    /// <c>--json</c> must go through this OR.</para>
    /// </summary>
    internal static bool WantsJson(ParseResult parseResult, Option<bool> localJson)
    {
        return parseResult.GetValueForOption(localJson) || WantsJson(parseResult);
    }

    /// <summary>
    /// True when the caller asked for verbose via a command-local
    /// <c>--verbose</c> OR the root global of the same spelling.
    /// </summary>
    internal static bool WantsVerbose(ParseResult parseResult, Option<bool> localVerbose)
    {
        return parseResult.GetValueForOption(localVerbose) || WantsVerbose(parseResult);
    }

    /// <summary>
    /// The refusal a command with no JSON rendering returns when <c>--format-json</c> was passed, and
    /// null when it was not — so a handler can read <c>RefuseJsonFormat(...) ?? RealWork(...)</c>.
    ///
    /// <para><c>--format-json</c> is a GLOBAL option on the root, so it parses on every command in the
    /// tree, including the ones that only ever print prose. Accepting it and printing prose anyway
    /// hands a caller who is piping into a parser exit 0 over unparseable text: a green light above a
    /// broken pipeline, which is worse than no output at all. Refusing is the honest answer until the
    /// command grows a JSON rendering of its own — and it does not prejudge what that rendering
    /// should be.</para>
    /// </summary>
    internal static int? RefuseJsonFormat(ParseResult parseResult, string command, TextWriter stderr)
    {
        if (!WantsJson(parseResult))
        {
            return null;
        }

        stderr.WriteLine($"`{command}` has no JSON rendering, so --format-json cannot be honoured here. "
            + "Refusing rather than printing prose a JSON reader cannot parse.");
        // Usage, not a failed verification — 65 stays reserved for a course that did not pass.
        return 1;
    }

    internal static Progress<ProgressReport>? CreateProgressReporter(
        bool verbose,
        bool json,
        ILogger logger,
        IConsoleRenderer renderer)
    {
        if (!verbose && json)
            return null;

        return new Progress<ProgressReport>(report =>
        {
            if (json)
            {
                logger.LogInformation(
                    "Progress: {Percentage}% - {Message}",
                    report.Percentage,
                    report.Message);
                return;
            }

            renderer.RenderProgress(report);
        });
    }

    internal static int RenderDomainFailure(
        ILogger logger,
        IConsoleRenderer renderer,
        Exception exception,
        string logMessage,
        int exitCode)
    {
        logger.LogError(exception, logMessage);
        var code = exception switch
        {
            Ashlar.Core.Domain.Exceptions.AnalysisException analysis => analysis.ErrorCode,
            Ashlar.Core.Domain.Exceptions.ValidationException validation => validation.ErrorCode,
            _ => null
        };
        var suggestion = exception switch
        {
            Ashlar.Core.Domain.Exceptions.AnalysisException analysis => analysis.Suggestion,
            Ashlar.Core.Domain.Exceptions.ValidationException validation => validation.Suggestion,
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(code))
            renderer.RenderErrorWithCode(exception.Message, code!, suggestion);
        else
            renderer.RenderError(exception.Message);

        return exitCode;
    }
}
