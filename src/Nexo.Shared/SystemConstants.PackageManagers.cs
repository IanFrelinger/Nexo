using System;
using System.Collections.Generic;

namespace Nexo.Shared
{
    /// <summary>
    /// Package manager names and identifiers with case-insensitive matching.
    /// </summary>
    public static partial class SystemConstants
    {
        public static class PackageManagers
        {
            public const string NuGet = "NuGet";
            public const string NPM = "npm";
            public const string Yarn = "yarn";
            public const string PNPM = "pnpm";
            public const string Maven = "Maven";
            public const string Gradle = "Gradle";
            public const string PIP = "pip";
            public const string Conda = "conda";
            public const string Composer = "Composer";
            public const string Gem = "gem";
            public const string Cargo = "cargo";
            public const string GoModules = "go modules";
            public const string Chocolatey = "Chocolatey";
            public const string Homebrew = "Homebrew";
            public const string APT = "apt";
            public const string YUM = "yum";
            public const string DNF = "dnf";
            public const string Pacman = "pacman";

            /// <summary>
            /// Gets all package manager variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> AllVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                NuGet, NPM, Yarn, PNPM, Maven, Gradle, PIP, Conda, Composer, Gem, Cargo, 
                GoModules, Chocolatey, Homebrew, APT, YUM, DNF, Pacman,
                "nuget", "npm", "yarn", "pnpm", "maven", "gradle", "pip", "conda", "composer", 
                "gem", "cargo", "go modules", "chocolatey", "homebrew", "apt", "yum", "dnf", "pacman"
            };

            /// <summary>
            /// Tries to match a package manager name case-insensitively.
            /// </summary>
            /// <param name="managerName">The package manager name to match.</param>
            /// <returns>The standardized package manager name or empty string if not found.</returns>
            public static string MatchPackageManager(string managerName)
            {
                if (string.IsNullOrWhiteSpace(managerName))
                    return string.Empty;

                var normalizedName = managerName.Trim();
                
                if (AllVariations.Contains(normalizedName))
                    return normalizedName;
                
                return string.Empty;
            }
        }
    }
}
