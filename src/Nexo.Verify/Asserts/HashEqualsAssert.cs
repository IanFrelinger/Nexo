using System.Security.Cryptography;
using System.Text;
using Nexo.Verify.Models;

namespace Nexo.Verify.Asserts
{
    /// <summary>
    /// Assert that a file's SHA256 hash matches the expected value
    /// </summary>
    public static class HashEqualsAssert
    {
        /// <summary>
        /// Verify that the specified file has the expected SHA256 hash
        /// </summary>
        /// <param name="filePath">Path to the file to check</param>
        /// <param name="expectedSha256">Expected SHA256 hash (case-insensitive)</param>
        /// <param name="console">Console for output</param>
        /// <returns>True if the hash matches</returns>
        public static bool Verify(string filePath, string expectedSha256, IConsole console)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    console.WriteLine($"ERROR: File not found: {filePath}");
                    return false;
                }

                var actualHash = ComputeSha256Hash(filePath);
                var expectedHash = expectedSha256.ToLowerInvariant().Trim();

                if (actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    console.WriteLine($"✓ Hash matches: {Path.GetFileName(filePath)}");
                    return true;
                }
                else
                {
                    console.WriteLine($"✗ Hash mismatch for {Path.GetFileName(filePath)}");
                    console.WriteLine($"  Expected: {expectedHash}");
                    console.WriteLine($"  Actual:   {actualHash}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                console.WriteLine($"ERROR: Failed to compute hash for {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Compute SHA256 hash of a file
        /// </summary>
        private static string ComputeSha256Hash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
