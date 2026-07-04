namespace Nexo.Core.Domain.Export;

/// <summary>Target language or artifact type produced by workflow export.</summary>
public enum ExportTarget
{
    /// <summary>C# source code and project files.</summary>
    CSharp,

    /// <summary>Python source code.</summary>
    Python,

    /// <summary>TypeScript source code.</summary>
    TypeScript,

    /// <summary>Behavior tree definition for game AI hosts.</summary>
    BehaviorTree,

    /// <summary>Generic finite-state machine definition.</summary>
    StateMachine
}
