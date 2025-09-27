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
    /// Interface tests for desktop code generation functionality
    /// </summary>
    public partial class DesktopCodeGenerationTests
    {
        #region Interface Tests

        [Fact]
        public void IDesktopCodeGenerator_Interface_IsDefined()
        {
            // Arrange & Act
            var generator = _desktopCodeGenerator as IDesktopCodeGenerator;

            // Assert
            Assert.NotNull(generator);
        }

        #endregion
    }
}
