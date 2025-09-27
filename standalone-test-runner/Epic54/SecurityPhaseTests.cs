using System;
using System.Collections.Generic;

namespace StandaloneTestRunner
{
    public partial class SecurityPhaseTests
    {
        private readonly bool _verbose;

        public SecurityPhaseTests(bool verbose = false)
        {
            _verbose = verbose;
        }

        public List<TestInfo> DiscoverSecurityTests()
        {
            return new List<TestInfo>
            {
                new TestInfo(
                    "epic5_4-security-authentication",
                    "Authentication Security",
                    "Tests authentication security mechanisms",
                    "Security",
                    "Critical",
                    2,
                    5,
                    new[] { "epic5_4", "security", "authentication" }
                ),
                new TestInfo(
                    "epic5_4-security-authorization",
                    "Authorization Security",
                    "Tests authorization security mechanisms",
                    "Security",
                    "Critical",
                    2,
                    5,
                    new[] { "epic5_4", "security", "authorization" }
                ),
                new TestInfo(
                    "epic5_4-security-data-encryption",
                    "Data Encryption Security",
                    "Tests data encryption and decryption",
                    "Security",
                    "Critical",
                    2,
                    5,
                    new[] { "epic5_4", "security", "data-encryption" }
                ),
                new TestInfo(
                    "epic5_4-security-network-security",
                    "Network Security",
                    "Tests network security configurations",
                    "Security",
                    "High",
                    2,
                    5,
                    new[] { "epic5_4", "security", "network" }
                ),
                new TestInfo(
                    "epic5_4-security-vulnerability-scanning",
                    "Vulnerability Scanning Security",
                    "Tests vulnerability scanning and assessment",
                    "Security",
                    "High",
                    2,
                    5,
                    new[] { "epic5_4", "security", "vulnerability-scanning" }
                )
            };
        }
    }
}
