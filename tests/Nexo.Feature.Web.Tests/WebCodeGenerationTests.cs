using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Web.Models;
using Nexo.Feature.Web.Enums;
using Nexo.Feature.Web.Services;
using Nexo.Feature.Web.UseCases;
using Moq;
using Nexo.Feature.Web.Interfaces;

namespace Nexo.Feature.Web.Tests
{
    /// <summary>
    /// Tests for web code generation functionality.
    /// This class acts as an orchestrator, delegating specific test categories to partial class implementations.
    /// </summary>
    public partial class WebCodeGenerationTests
    {
        private readonly ILogger<WebCodeGenerationTests> _logger;

        public WebCodeGenerationTests()
        {
            _logger = NullLogger<WebCodeGenerationTests>.Instance;
        }
    }
}