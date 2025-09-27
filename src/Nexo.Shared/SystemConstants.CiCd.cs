using System;
using System.Collections.Generic;

namespace Nexo.Shared
{
    /// <summary>
    /// CI/CD platform names and identifiers with case-insensitive matching.
    /// </summary>
    public static partial class SystemConstants
    {
        public static class CiCdPlatforms
        {
            public const string GitHubActions = "GitHub Actions";
            public const string GitLabCI = "GitLab CI";
            public const string AzureDevOps = "Azure DevOps";
            public const string Jenkins = "Jenkins";
            public const string CircleCI = "CircleCI";
            public const string TravisCI = "Travis CI";
            public const string TeamCity = "TeamCity";
            public const string Bamboo = "Bamboo";
            public const string Concourse = "Concourse";
            public const string Drone = "Drone";
            public const string Buildkite = "Buildkite";
            public const string AppVeyor = "AppVeyor";

            /// <summary>
            /// Gets all CI/CD platform variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> AllVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                GitHubActions, GitLabCI, AzureDevOps, Jenkins, CircleCI, TravisCI, 
                TeamCity, Bamboo, Concourse, Drone, Buildkite, AppVeyor,
                "github actions", "gitlab ci", "azure devops", "jenkins", "circleci", "travis ci",
                "teamcity", "bamboo", "concourse", "drone", "buildkite", "appveyor"
            };

            /// <summary>
            /// Tries to match a CI/CD platform name case-insensitively.
            /// </summary>
            /// <param name="platformName">The CI/CD platform name to match.</param>
            /// <returns>The standardized CI/CD platform name or empty string if not found.</returns>
            public static string MatchCiCdPlatform(string platformName)
            {
                if (string.IsNullOrWhiteSpace(platformName))
                    return string.Empty;

                var normalizedName = platformName.Trim();
                
                if (AllVariations.Contains(normalizedName))
                    return normalizedName;
                
                return string.Empty;
            }
        }
    }
}
