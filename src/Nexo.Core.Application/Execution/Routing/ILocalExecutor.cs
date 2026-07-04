using Nexo.Core.Domain;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Application.Execution.Routing;

/// <summary>
/// Marker interface for executors that run jobs on the local node's GPU / CPU.
/// Registered in DI only when the NCR module detects sufficient local capability.
/// </summary>
public interface ILocalExecutor : IBrickExecutor
{
}
