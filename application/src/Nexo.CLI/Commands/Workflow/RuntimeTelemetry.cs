namespace Nexo.CLI.Commands.Workflow;

internal sealed record RuntimeTelemetry(
    long CpuTimeDeltaMs,
    long WorkingSetMb,
    long PrivateMemoryMb,
    long ManagedMemoryMb,
    int ThreadCount,
    string HardwareProfile);
