using System.Runtime.CompilerServices;

namespace Nexo.Tests.CLI;

/// <summary>
/// The canonical console writers for this test assembly, captured once at
/// assembly load.
///
/// Several suites here redirect <see cref="Console.Out"/> / <see cref="Console.Error"/>
/// to a <see cref="StringWriter"/> to assert on command output. The natural-looking
/// pattern is fatal:
///
///     var originalOut = Console.Out;   // may ALREADY be another test's writer
///     Console.SetOut(myWriter);
///     try { ... } finally { Console.SetOut(originalOut); }   // restores the foreign writer
///
/// Because <c>Console.Out</c> is process-global, "whatever was installed when this
/// test started" is not necessarily the real console. If a previous test leaked its
/// writer, this test faithfully restores that foreign writer — and every later test
/// then writes into a buffer nobody reads (empty-JSON assertion failures), or into a
/// disposed one (ObjectDisposedException from Console.WriteLine). Tests run
/// sequentially here, so this is ordering contamination, not a race: it explains why
/// a DIFFERENT test failed on each run.
///
/// Restoring to <see cref="Out"/> / <see cref="Error"/> instead makes the teardown
/// idempotent — any test can hand the console back to a known-good writer regardless
/// of what it inherited.
/// </summary>
internal static class ConsoleCapture
{
    /// <summary>The real stdout writer, as it was before any test redirected it.</summary>
    public static TextWriter Out { get; private set; } = Console.Out;

    /// <summary>The real stderr writer, as it was before any test redirected it.</summary>
    public static TextWriter Error { get; private set; } = Console.Error;

    [ModuleInitializer]
    internal static void Capture()
    {
        Out = Console.Out;
        Error = Console.Error;
    }
}
