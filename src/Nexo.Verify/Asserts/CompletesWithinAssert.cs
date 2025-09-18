using Nexo.Verify.Models;

namespace Nexo.Verify.Asserts
{
    /// <summary>
    /// Assert that execution completed within a specified time limit
    /// </summary>
    public static class CompletesWithinAssert
    {
        /// <summary>
        /// Verify that execution completed within the specified time limit
        /// </summary>
        /// <param name="elapsedMs">Actual elapsed time in milliseconds</param>
        /// <param name="limitMs">Maximum allowed time in milliseconds</param>
        /// <param name="console">Console for output</param>
        /// <returns>True if execution completed within the limit</returns>
        public static bool Verify(int elapsedMs, int limitMs, IConsole console)
        {
            if (elapsedMs <= limitMs)
            {
                console.WriteLine($"✓ Completed within time limit: {elapsedMs}ms <= {limitMs}ms");
                return true;
            }
            else
            {
                console.WriteLine($"✗ Exceeded time limit: {elapsedMs}ms > {limitMs}ms");
                return false;
            }
        }
    }
}
