using System;
using System.Collections.Generic;

namespace Nexo.Shared
{
    /// <summary>
    /// Cloud provider names and identifiers with case-insensitive matching.
    /// </summary>
    public static partial class SystemConstants
    {
        public static class CloudProviders
        {
            public const string AWS = "AWS";
            public const string Azure = "Azure";
            public const string GoogleCloud = "Google Cloud";
            public const string GCP = "GCP";
            public const string DigitalOcean = "DigitalOcean";
            public const string Linode = "Linode";
            public const string Vultr = "Vultr";
            public const string Heroku = "Heroku";
            public const string IBMCloud = "IBM Cloud";
            public const string OracleCloud = "Oracle Cloud";
            public const string AlibabaCloud = "Alibaba Cloud";
            public const string TencentCloud = "Tencent Cloud";
            public const string OnPremises = "On-Premises";

            /// <summary>
            /// Gets all cloud provider variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> AllVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                AWS, Azure, GoogleCloud, GCP, DigitalOcean, Linode, Vultr, Heroku, 
                IBMCloud, OracleCloud, AlibabaCloud, TencentCloud, OnPremises,
                "aws", "azure", "google cloud", "gcp", "digitalocean", "linode", "vultr", "heroku",
                "ibm cloud", "oracle cloud", "alibaba cloud", "tencent cloud", "on-premises"
            };

            /// <summary>
            /// Tries to match a cloud provider name case-insensitively.
            /// </summary>
            /// <param name="providerName">The cloud provider name to match.</param>
            /// <returns>The standardized cloud provider name or empty string if not found.</returns>
            public static string MatchCloudProvider(string providerName)
            {
                if (string.IsNullOrWhiteSpace(providerName))
                    return string.Empty;

                var normalizedName = providerName.Trim();
                
                if (AllVariations.Contains(normalizedName))
                    return normalizedName;
                
                return string.Empty;
            }
        }
    }
}
