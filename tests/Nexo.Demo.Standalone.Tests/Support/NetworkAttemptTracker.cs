using System;
using System.Threading;

namespace Nexo.Demo.Tests.Support
{
    /// <summary>
    /// Tracks network attempts for demo scenarios
    /// </summary>
    public static class NetworkAttemptTracker
    {
        private static int _networkAttempts = 0;

        /// <summary>
        /// Gets the current number of network attempts
        /// </summary>
        public static int GetNetworkAttempts() => _networkAttempts;

        /// <summary>
        /// Increments the network attempt counter
        /// </summary>
        public static void IncrementNetworkAttempts() 
        {
            Interlocked.Increment(ref _networkAttempts);
        }

        /// <summary>
        /// Resets the network attempt counter
        /// </summary>
        public static void ResetNetworkAttempts() => Interlocked.Exchange(ref _networkAttempts, 0);
    }
}
