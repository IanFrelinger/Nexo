using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Nexo.Feature.Factory.Testing.Models
{
    /// <summary>
    /// Implementation of ITestContext for test execution.
    /// </summary>
    public sealed class TestContext : ITestContext
    {
        public string SessionId { get; }
        public TestConfiguration Configuration { get; }
        public IDictionary<string, object> SharedData { get; }
        public CancellationToken CancellationToken { get; }
        public ILogger Logger { get; }

        public TestContext(
            string sessionId,
            TestConfiguration configuration,
            IDictionary<string, object> sharedData,
            CancellationToken cancellationToken,
            ILogger logger)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            SharedData = sharedData ?? throw new ArgumentNullException(nameof(sharedData));
            CancellationToken = cancellationToken;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new test context with default configuration.
        /// </summary>
        /// <param name="sessionId">The session ID</param>
        /// <param name="logger">The logger</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>New test context</returns>
        public static TestContext CreateDefault(string sessionId, ILogger logger, CancellationToken cancellationToken = default)
        {
            var configuration = new TestConfiguration();
            var sharedData = new Dictionary<string, object>();
            
            return new TestContext(sessionId, configuration, sharedData, cancellationToken, logger);
        }

        /// <summary>
        /// Creates a new test context with custom configuration.
        /// </summary>
        /// <param name="sessionId">The session ID</param>
        /// <param name="configuration">The test configuration</param>
        /// <param name="logger">The logger</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>New test context</returns>
        public static TestContext CreateCustom(
            string sessionId, 
            TestConfiguration configuration, 
            ILogger logger, 
            CancellationToken cancellationToken = default)
        {
            var sharedData = new Dictionary<string, object>();
            
            return new TestContext(sessionId, configuration, sharedData, cancellationToken, logger);
        }
    }
}
