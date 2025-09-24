using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Infrastructure.Services.Platform
{
    public interface IUiFeatureDetector { Task<object> DetectAsync(string platform, CancellationToken ct); }
    public interface IDataFeatureDetector { Task<object> DetectAsync(string platform, CancellationToken ct); }
    public interface INetworkFeatureDetector { Task<object> DetectAsync(string platform, CancellationToken ct); }
    public interface IHardwareFeatureDetector { Task<object> DetectAsync(string platform, CancellationToken ct); }
    public interface ISecurityFeatureDetector { Task<object> DetectAsync(string platform, CancellationToken ct); }
    public interface IPerformanceFeatureDetector { Task<object> DetectAsync(string platform, CancellationToken ct); }
}
