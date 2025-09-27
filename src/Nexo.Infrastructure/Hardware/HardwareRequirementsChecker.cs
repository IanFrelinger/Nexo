using System;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;

namespace Nexo.Infrastructure.Hardware
{
    /// <summary>
    /// Service for checking hardware requirements and providing cloud fallback options
    /// </summary>
    public partial class HardwareRequirementsChecker : IHardwareRequirementsChecker
    {
        private readonly ILogger<HardwareRequirementsChecker> _logger;

        public HardwareRequirementsChecker(ILogger<HardwareRequirementsChecker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

    }
}
