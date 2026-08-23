using Ashlar.Tests.Infrastructure.Locking;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Locking;

/// <summary>Tests for exclusive file cross process lock provider.</summary>
public sealed class ExclusiveFileCrossProcessLockProviderTests
{
    [Fact]
    public async Task AcquireAsync_LockPath_UnderTempDirectory()
    {
        var name = "sdk-test-" + Guid.NewGuid();
        var provider = new ExclusiveFileCrossProcessLockProvider(
            new CrossProcessLockOptions
            {
                FileNamePrefix = "ashlar-test",
                FileNameSuffix = ".lock",
                MaxWait = TimeSpan.FromSeconds(5),
            });

        await using (var l = await provider.AcquireAsync(name))
        {
            Assert.StartsWith(Path.GetTempPath(), l.LockPath, StringComparison.Ordinal);
            Assert.Contains("ashlar-test", l.LockPath, StringComparison.Ordinal);
        }
    }
}
