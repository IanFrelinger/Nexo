namespace Nexo.Core.Application.Common.Ports;

/// <summary>Action returned by a loop body to continue or break iteration.</summary>
public enum LoopAction
{
    /// <summary>Continue to the next item.</summary>
    Continue = 0,

    /// <summary>Stop iteration early.</summary>
    Break = 1
}
