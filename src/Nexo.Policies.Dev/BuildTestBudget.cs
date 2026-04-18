using System.Collections.Concurrent;
using Nexo.Abstractions;

namespace Nexo.Policies.Dev;

/// <summary>
/// Caps how many <c>dotnet.build</c>, <c>forge.build</c>, and <c>dotnet.test</c> tool calls a single
/// agent cycle is allowed to issue. Without this, a confused planner can burn
/// the entire 5-minute deadline on repeated build/test invocations and never
/// reach the actual code change.
///
/// <para>The cap is intentionally small (default 1 build + 2 tests per cycle)
/// because each invocation is multi-second and produces large stdout. Operators
/// can override per-cycle caps via the <c>NEXO_BUILD_BUDGET</c> and
/// <c>NEXO_TEST_BUDGET</c> environment variables.</para>
///
/// <para>Counters are per-process and are reset at the cycle boundary by
/// callers via <see cref="Reset"/>. They are deliberately not per-snapshot tick
/// because <c>WorldSnapshot.Tick</c> is incremented inside the loop and would
/// silently allow N×budget calls.</para>
/// </summary>
public sealed class BuildTestBudget : IPolicy
{
    /// <summary>Default maximum number of <c>dotnet.build</c> calls per cycle.</summary>
    public const int DefaultBuildBudget = 1;

    /// <summary>Default maximum number of <c>dotnet.test</c> calls per cycle.</summary>
    public const int DefaultTestBudget = 2;

    private readonly int _buildBudget;
    private readonly int _testBudget;
    private int _builds;
    private int _tests;

    public BuildTestBudget(int? buildBudget = null, int? testBudget = null)
    {
        _buildBudget = buildBudget
            ?? ReadEnvBudget("NEXO_BUILD_BUDGET", DefaultBuildBudget);
        _testBudget = testBudget
            ?? ReadEnvBudget("NEXO_TEST_BUDGET", DefaultTestBudget);
    }

    /// <summary>Resets per-cycle counters. Callers MUST invoke this between cycles.</summary>
    public void Reset()
    {
        _builds = 0;
        _tests = 0;
    }

    public bool Approve(ToolCall call, WorldSnapshot s, out string reason)
    {
        reason = "OK";
        if (string.Equals(call.Id, "dotnet.build", StringComparison.Ordinal)
            || string.Equals(call.Id, "forge.build", StringComparison.Ordinal))
        {
            // Increment-and-test is intentional: the increment counts towards the
            // budget even if we deny so a runaway loop doesn't keep hammering the
            // policy with the same call shape.
            var n = System.Threading.Interlocked.Increment(ref _builds);
            if (n > _buildBudget)
            {
                reason = $"Build budget exceeded for this cycle ({n}/{_buildBudget}); inspect previous build output before retrying.";
                return false;
            }
        }
        else if (string.Equals(call.Id, "dotnet.test", StringComparison.Ordinal))
        {
            var n = System.Threading.Interlocked.Increment(ref _tests);
            if (n > _testBudget)
            {
                reason = $"Test budget exceeded for this cycle ({n}/{_testBudget}); inspect previous test output before retrying.";
                return false;
            }
        }
        return true;
    }

    private static int ReadEnvBudget(string envVar, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(envVar);
        if (string.IsNullOrWhiteSpace(raw)) return fallback;
        return int.TryParse(raw.Trim(), out var parsed) && parsed >= 0 ? parsed : fallback;
    }
}
