using System.Collections.Generic;

namespace Nexo.Core.Application.Commands.Assembly
{
    /// <summary>
    /// Input for scanning assembly security
    /// </summary>
    public class ScanAssemblySecurityInput
    {
        public string AssemblyPath { get; set; } = string.Empty;
        public SecurityScanOptions ScanOptions { get; set; } = new SecurityScanOptions();
    }

    /// <summary>
    /// Options for security scanning
    /// </summary>
    public class SecurityScanOptions
    {
        public bool ScanForVulnerabilities { get; set; } = true;
        public bool ScanForMalware { get; set; } = true;
        public bool ScanForObfuscation { get; set; } = true;
        public bool ScanForPacking { get; set; } = true;
        public bool ScanForAntiDebugging { get; set; } = true;
        public bool ScanForCodeInjection { get; set; } = true;
        public bool ScanForPrivilegeEscalation { get; set; } = true;
        public bool ScanForDataExfiltration { get; set; } = true;
        public Dictionary<string, object> AdditionalOptions { get; set; } = new Dictionary<string, object>();
    }
}
