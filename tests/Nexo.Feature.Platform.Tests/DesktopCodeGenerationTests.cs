using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Platform.Enums;

namespace Nexo.Feature.Platform.Tests
{
    /// <summary>
    /// Tests for desktop code generation functionality.
    /// Split into Success/ErrorHandling/Cancellation categories.
    /// </summary>
    public partial class DesktopCodeGenerationTests
    {
        private readonly Mock<ILogger<DesktopCodeGenerator>> _mockLogger;
        private readonly DesktopCodeGenerator _desktopCodeGenerator;

        public DesktopCodeGenerationTests()
        {
            _mockLogger = new Mock<ILogger<DesktopCodeGenerator>>();
            _desktopCodeGenerator = new DesktopCodeGenerator(_mockLogger.Object);
        }
    }
}