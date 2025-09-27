using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models;
using Nexo.Infrastructure.Compilation;
using Xunit;

namespace Nexo.Infrastructure.Tests.Compilation
{
    /// <summary>
    /// Tests for idempotent rebuilds and compilation failure diagnostics.
    /// This class acts as an orchestrator, delegating specific test categories to partial class implementations.
    /// </summary>
    public partial class RoslynCompilationIdempotentTests
    {
        private readonly RoslynCompilationService _compiler;
        private readonly ILogger<RoslynCompilationService> _logger;
        private readonly string _tempDirectory;

        public RoslynCompilationIdempotentTests()
        {
            _logger = new LoggerFactory().CreateLogger<RoslynCompilationService>();
            _compiler = new RoslynCompilationService(_logger);
            _tempDirectory = Path.Combine(Path.GetTempPath(), "nexo_compilation_test", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }
        // This class acts as an orchestrator for various test categories,
        // with specific categories defined in partial classes.
    }
}