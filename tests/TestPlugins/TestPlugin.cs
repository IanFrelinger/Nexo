using Nexo.Core.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestPlugins
{
    public class TestPlugin : IPlugin
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
    }
}
