using System;

namespace Nexo.Infrastructure.Tests.Services.Security
{
    /// <summary>
    /// Mock classes for testing
    /// </summary>
    public class ApiKeyInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Strength { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class ComplianceStatus
    {
        public bool GDPRCompliance { get; set; }
        public bool HIPAACompliance { get; set; }
        public bool SOXCompliance { get; set; }
        public bool ISO27001Compliance { get; set; }
    }
}
