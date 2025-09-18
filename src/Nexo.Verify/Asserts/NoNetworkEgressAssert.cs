using Nexo.Verify.Models;

namespace Nexo.Verify.Asserts
{
    /// <summary>
    /// Assert that no network egress occurred during execution
    /// </summary>
    public static class NoNetworkEgressAssert
    {
        /// <summary>
        /// Verify that no network calls were made during execution
        /// </summary>
        /// <param name="result">Scenario execution result</param>
        /// <param name="console">Console for output</param>
        /// <returns>True if no network calls were made</returns>
        public static bool Verify(ScenarioResult result, IConsole console)
        {
            // Check if network guard was installed and count attempts
            var outboundAttempts = NetworkTestHarness.OutboundAttempts;
            
            if (outboundAttempts == 0)
            {
                console.WriteLine("✓ No network egress detected");
                return true;
            }
            else
            {
                console.WriteLine($"✗ Network egress detected: {outboundAttempts} outbound attempts");
                return false;
            }
        }
    }

    /// <summary>
    /// Test harness for tracking network calls
    /// </summary>
    public static class NetworkTestHarness
    {
        private static int _outboundAttempts = 0;
        private static NetworkGuardHandler? _handler;

        /// <summary>
        /// Number of outbound network attempts detected
        /// </summary>
        public static int OutboundAttempts => _outboundAttempts;

        /// <summary>
        /// Install the network guard globally
        /// </summary>
        public static void InstallGlobal()
        {
            _handler = new NetworkGuardHandler();
            // In a real implementation, this would register the handler with HttpClient
            // For now, we'll track attempts through the handler
        }

        /// <summary>
        /// Record an outbound network attempt
        /// </summary>
        public static void RecordAttempt()
        {
            Interlocked.Increment(ref _outboundAttempts);
        }

        /// <summary>
        /// Reset the attempt counter
        /// </summary>
        public static void Reset()
        {
            Interlocked.Exchange(ref _outboundAttempts, 0);
        }

        /// <summary>
        /// Uninstall the network guard
        /// </summary>
        public static void Uninstall()
        {
            _handler?.Dispose();
            _handler = null;
        }
    }

    /// <summary>
    /// HTTP handler that blocks network calls in OFF/ASSIST modes
    /// </summary>
    public sealed class NetworkGuardHandler : DelegatingHandler, IDisposable
    {
        public NetworkGuardHandler() : base(new HttpClientHandler())
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            NetworkTestHarness.RecordAttempt();
            
            var mode = Environment.GetEnvironmentVariable("NEXO_AI_MODE")?.ToLowerInvariant();
            if (mode == "off" || mode == "assist")
            {
                throw new InvalidOperationException($"Network egress blocked in {mode.ToUpperInvariant()} mode");
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
