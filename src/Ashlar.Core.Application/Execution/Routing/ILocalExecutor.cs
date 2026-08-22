using Ashlar.Core.Domain;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Application.Execution.Routing;

/// <summary>
/// Marker interface for executors that run jobs on the local node's GPU / CPU.
/// Registered in DI only when the NCR module detects sufficient local capability.
/// </summary>
public interface ILocalExecutor : IBrickExecutor
{
}
