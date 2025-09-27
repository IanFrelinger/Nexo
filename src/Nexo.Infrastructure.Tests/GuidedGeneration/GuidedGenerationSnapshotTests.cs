using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.GuidedGeneration;
using Nexo.Infrastructure.GuidedGeneration;
using Xunit;

namespace Nexo.Infrastructure.Tests.GuidedGeneration
{
    /// <summary>
    /// Snapshot tests for guided generation flow including invalid inputs and resume behaviors.
    /// This class acts as an orchestrator, delegating specific test categories to partial class implementations.
    /// </summary>
    public partial class GuidedGenerationSnapshotTests
    {
        private readonly GuidedGenerationService _service;
        private readonly ILogger<GuidedGenerationService> _logger;

        public GuidedGenerationSnapshotTests()
        {
            _logger = new LoggerFactory().CreateLogger<GuidedGenerationService>();
            _service = new GuidedGenerationService(null, _logger);
        }
    }
}