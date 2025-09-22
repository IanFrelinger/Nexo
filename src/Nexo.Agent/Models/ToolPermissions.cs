namespace Nexo.Agent.Models;

/// <summary>
/// Tool permissions flags.
/// </summary>
[Flags]
public enum ToolPermissions
{
    None = 0,
    FileRead = 1,
    FileWrite = 2,
    Network = 4,
    Process = 8,
    All = FileRead | FileWrite | Network | Process
}
