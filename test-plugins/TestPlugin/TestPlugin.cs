using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Contracts.Capabilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestPlugin
{
    public partial class TestPlugin : IPlugin
    {
        public string Name => "TestPlugin";
        public string Version => "1.0.0";
        public string Description => "A test plugin for unit testing";
        public string Author => "Test Author";
        public bool IsEnabled => true;

        public Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<PluginResult> ExecuteAsync(string[] args)
        {
            return Task.FromResult(new PluginResult { Success = true, Message = "TestPlugin executed successfully" });
        }
    }

    /// <summary>
    /// Test sensing capability implementation.
    /// </summary>
    public partial class TestSenseCapability : ISense
    {
        public string CapabilityName => "TestSense";

        public Task<object> SenseAsync(object input, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<object>($"Sensed: {input}");
        }
    }
}
