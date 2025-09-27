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
    /// Enum tests for API Gateway functionality
    /// </summary>
    public partial class APIGatewayTests
    {
        #region Enum Tests

        [Fact]
        public void ServiceStatus_EnumValues_AreDefined()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(ServiceStatus), ServiceStatus.Active));
            Assert.True(Enum.IsDefined(typeof(ServiceStatus), ServiceStatus.Inactive));
            Assert.True(Enum.IsDefined(typeof(ServiceStatus), ServiceStatus.Maintenance));
            Assert.True(Enum.IsDefined(typeof(ServiceStatus), ServiceStatus.Overloaded));
            Assert.True(Enum.IsDefined(typeof(ServiceStatus), ServiceStatus.Error));
        }

        [Fact]
        public void HealthStatus_EnumValues_AreDefined()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(HealthStatus), HealthStatus.Healthy));
            Assert.True(Enum.IsDefined(typeof(HealthStatus), HealthStatus.Degraded));
            Assert.True(Enum.IsDefined(typeof(HealthStatus), HealthStatus.Unhealthy));
            Assert.True(Enum.IsDefined(typeof(HealthStatus), HealthStatus.Unknown));
        }

        [Fact]
        public void RoutingStrategy_EnumValues_AreDefined()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(RoutingStrategy), RoutingStrategy.RoundRobin));
            Assert.True(Enum.IsDefined(typeof(RoutingStrategy), RoutingStrategy.LeastConnections));
            Assert.True(Enum.IsDefined(typeof(RoutingStrategy), RoutingStrategy.WeightedRoundRobin));
            Assert.True(Enum.IsDefined(typeof(RoutingStrategy), RoutingStrategy.IPHash));
            Assert.True(Enum.IsDefined(typeof(RoutingStrategy), RoutingStrategy.Random));
        }

        [Fact]
        public void AuthenticationMethod_EnumValues_AreDefined()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(AuthenticationMethod), AuthenticationMethod.None));
            Assert.True(Enum.IsDefined(typeof(AuthenticationMethod), AuthenticationMethod.ApiKey));
            Assert.True(Enum.IsDefined(typeof(AuthenticationMethod), AuthenticationMethod.BearerToken));
            Assert.True(Enum.IsDefined(typeof(AuthenticationMethod), AuthenticationMethod.OAuth2));
            Assert.True(Enum.IsDefined(typeof(AuthenticationMethod), AuthenticationMethod.JWT));
        }

        #endregion
    }
}
