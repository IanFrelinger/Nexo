using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Nexo.Demo.Tests.Support
{
    /// <summary>
    /// Provides file hashing utilities for demo scenarios
    /// </summary>
    public static class FileHasher
    {
        /// <summary>
        /// Calculates SHA256 hash of a file
        /// </summary>
        public static string Sha256File(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}");

            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(path);
            var hash = sha256.ComputeHash(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
