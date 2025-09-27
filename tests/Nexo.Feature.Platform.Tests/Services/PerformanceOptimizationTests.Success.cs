using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Nexo.Feature.Platform.Tests.Services;

/// <summary>
/// Success test cases for PerformanceOptimization.
/// This class acts as an orchestrator, delegating specific test categories to partial class implementations.
/// </summary>
public partial class PerformanceOptimizationTests
{
    private readonly PerformanceOptimization _performanceOptimization;
    private readonly ILogger<PerformanceOptimizationTests> _logger;

    public PerformanceOptimizationTests()
    {
        _logger = new LoggerFactory().CreateLogger<PerformanceOptimizationTests>();
        _performanceOptimization = new PerformanceOptimization(NullLogger<PerformanceOptimization>.Instance);
    }
}