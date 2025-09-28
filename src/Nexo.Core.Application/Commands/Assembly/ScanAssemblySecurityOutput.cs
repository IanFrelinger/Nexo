using System.Collections.Generic;
using Nexo.Shared.Models.Assembly;

namespace Nexo.Core.Application.Commands.Assembly
{
    /// <summary>
    /// Output for scanning assembly security
    /// </summary>
    public class ScanAssemblySecurityOutput
    {
        public string AssemblyPath { get; set; } = string.Empty;
        public List<SecurityIssue> SecurityIssues { get; set; } = new List<SecurityIssue>();
        public int IssueCount { get; set; }
        public int CriticalIssues { get; set; }
        public int HighIssues { get; set; }
        public int MediumIssues { get; set; }
        public int LowIssues { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
