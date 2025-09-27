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
    /// Tests for API Gateway functionality.
    /// Split into Success/ErrorHandling/Cancellation categories.
    /// </summary>
    public partial class APIGatewayTests
    {
        private readonly Mock<ILogger<APIGateway>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpHandler;
        private readonly HttpClient _httpClient;
        private readonly APIGateway _apiGateway;

        public APIGatewayTests()
        {
            _mockLogger = new Mock<ILogger<APIGateway>>();
            _mockHttpHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpHandler.Object);
            _apiGateway = new APIGateway(_mockLogger.Object, _httpClient);
        }

        #region Helper Methods

        private void SetupMockHttpResponse(HttpStatusCode statusCode, string content)
        {
            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content)
                });
        }

        #endregion
    }
}