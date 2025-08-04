using Nexo.Core.Application.Enums;
using Nexo.Core.Application.Interfaces;

namespace Nexo.Shared.Models
{
    /// <summary>
    /// Configuration for test execution.
    /// </summary>
    public class TestConfiguration
    {
        public string Framework { get; set; }
        public string Filter { get; set; }
        public bool RunInParallel { get; set; }
        public int TimeoutSeconds { get; set; }
    }
}