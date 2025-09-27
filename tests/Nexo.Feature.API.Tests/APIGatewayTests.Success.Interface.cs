using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using Nexo.Feature.API.Interfaces;
using Nexo.Feature.API.Models;
using Nexo.Feature.API.Services;
using Nexo.Feature.API.Enums;
using System.Net;
using System.Threading;

namespace Nexo.Feature.API.Tests
{
    /// <summary>
    /// Interface tests for API Gateway functionality
    /// </summary>
    public partial class APIGatewayTests
    {
        #region Interface Tests

        [Fact]
        public void IAPIGateway_Interface_IsDefined()
        {
            // Assert
            Assert.NotNull(typeof(IAPIGateway));
            Assert.True(typeof(IAPIGateway).IsInterface);
        }

        #endregion
    }
}
