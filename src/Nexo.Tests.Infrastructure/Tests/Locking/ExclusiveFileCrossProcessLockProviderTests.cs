using Nexo.Tests.Infrastructure.Locking;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Locking;

public sealed class ExclusiveFileCrossProcessLockProviderTests
{
    [Fact]
    public async Task AcquireAsync_LockPath_UnderTempDirectory()
    {
        var name = "sdk-test-" + Guid.NewGuid();
        var provider = new ExclusiveFileCrossProcessLockProvider(
            new CrossProcessLockOptions
            {
                FileNamePrefix = "nexo-test",
                FileNameSuffix = ".lock",
                MaxWait = TimeSpan.FromSeconds(5),
            });

        await using (var l = await provider.AcquireAsync(name))
        {
            Assert.StartsWith(Path.GetTempPath(), l.LockPath, StringComparison.Ordinal);
            Assert.Contains("nexo-test", l.LockPath, StringComparison.Ordinal);
        }
    }
}
