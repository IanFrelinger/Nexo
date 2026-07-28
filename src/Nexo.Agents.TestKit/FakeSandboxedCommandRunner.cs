using Nexo.Core.Application.Execution.Ports;

namespace Nexo.Agents.TestKit;

/// <summary>
/// Wraps the SAME <see cref="ISandboxedCommandRunner"/> port production uses, with a
/// scripted run of results. Lets a test prove the repair loop feeds a failed
/// verification back into the next draft and stops on success — with no container
/// anywhere in sight.
/// </summary>
public sealed class FakeSandboxedCommandRunner : ISandboxedCommandRunner
{
    private readonly Scripted<ProcessCommandResult> _results;

    /// <summary>Creates a runner from a scripted sequence of results.</summary>
    public FakeSandboxedCommandRunner(Scripted<ProcessCommandResult> results) =>
        _results = results ?? throw new ArgumentNullException(nameof(results));

    /// <summary>Creates a runner from an explicit run of results.</summary>
    public FakeSandboxedCommandRunner(params ProcessCommandResult[] results)
        : this(new Scripted<ProcessCommandResult>(results)) { }

    /// <summary>Every spec this runner was asked to execute, in order.</summary>
    public List<SandboxSpec> Specs { get; } = new();

    /// <summary>How many times the runner was invoked.</summary>
    public int Calls => _results.Calls;

    /// <inheritdoc />
    public Task<ProcessCommandResult> RunAsync(
        SandboxSpec spec,
        CancellationToken cancellationToken = default)
    {
        Specs.Add(spec);
        return Task.FromResult(_results.Next());
    }

    /// <summary>A failing result carrying a canned diagnostic log.</summary>
    public static ProcessCommandResult Failure(string log, int exitCode = 1) =>
        new(exitCode, "", log);

    /// <summary>A successful result.</summary>
    public static ProcessCommandResult Success(string stdout = "") =>
        new(0, stdout, "");

    /// <summary>
    /// The canonical repair-loop script: verification fails once with a diagnostic,
    /// then succeeds.
    /// </summary>
    public static FakeSandboxedCommandRunner FailThenSucceed(string failureLog) =>
        new(Failure(failureLog), Success());
}
