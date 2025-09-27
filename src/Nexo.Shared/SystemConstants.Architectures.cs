using System;
using System.Collections.Generic;

namespace Nexo.Shared
{
    /// <summary>
    /// Architecture names and identifiers with case-insensitive matching.
    /// </summary>
    public static partial class SystemConstants
    {
        public static class Architectures
        {
            public const string X86 = "x86";
            public const string X64 = "x64";
            public const string AMD64 = "amd64";
            public const string ARM = "arm";
            public const string ARM64 = "arm64";
            public const string AARCH64 = "aarch64";
            public const string PPC64 = "ppc64";
            public const string PPC64LE = "ppc64le";
            public const string S390X = "s390x";
            public const string RISCV64 = "riscv64";
            public const string Unknown = "unknown";

            /// <summary>
            /// Gets all architecture variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> AllVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                X86, X64, AMD64, ARM, ARM64, AARCH64, PPC64, PPC64LE, S390X, RISCV64,
                "X86", "X64", "AMD64", "ARM", "ARM64", "AARCH64", "PPC64", "PPC64LE", "S390X", "RISCV64"
            };

            /// <summary>
            /// Tries to match an architecture name case-insensitively.
            /// </summary>
            /// <param name="architectureName">The architecture name to match.</param>
            /// <returns>The standardized architecture name or Unknown if not found.</returns>
            public static string MatchArchitecture(string architectureName)
            {
                if (string.IsNullOrWhiteSpace(architectureName))
                    return Unknown;

                var normalizedName = architectureName.Trim();
                
                if (AllVariations.Contains(normalizedName))
                    return normalizedName.ToLowerInvariant();
                
                return Unknown;
            }
        }
    }
}
