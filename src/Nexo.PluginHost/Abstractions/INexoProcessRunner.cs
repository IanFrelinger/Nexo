using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.PluginHost.Abstractions
{
    /// <summary>
    /// Abstraction for process operations to prevent plugins from direct process access.
    /// </summary>
    public interface INexoProcessRunner
    {
        /// <summary>
        /// Starts a process with the specified configuration.
        /// </summary>
        Task<INexoProcess> StartProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts a process with the specified command and arguments.
        /// </summary>
        Task<INexoProcess> StartProcessAsync(string fileName, string arguments = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about a running process by ID.
        /// </summary>
        INexoProcess GetProcessById(int processId);

        /// <summary>
        /// Gets all running processes.
        /// </summary>
        IEnumerable<INexoProcess> GetProcesses();

        /// <summary>
        /// Gets running processes by name.
        /// </summary>
        IEnumerable<INexoProcess> GetProcessesByName(string processName);

        /// <summary>
        /// Gets the current process.
        /// </summary>
        INexoProcess GetCurrentProcess();

        /// <summary>
        /// Checks if a process is running by ID.
        /// </summary>
        bool IsProcessRunning(int processId);

        /// <summary>
        /// Kills a process by ID.
        /// </summary>
        void KillProcess(int processId);

        /// <summary>
        /// Kills a process by ID with force.
        /// </summary>
        void KillProcess(int processId, bool force);
    }

    /// <summary>
    /// Abstraction for a process instance.
    /// </summary>
    public interface INexoProcess : IDisposable
    {
        /// <summary>
        /// Gets the process ID.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the process name.
        /// </summary>
        string ProcessName { get; }

        /// <summary>
        /// Gets the process start time.
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// Gets the process exit time.
        /// </summary>
        DateTime? ExitTime { get; }

        /// <summary>
        /// Gets the process exit code.
        /// </summary>
        int ExitCode { get; }

        /// <summary>
        /// Gets whether the process has exited.
        /// </summary>
        bool HasExited { get; }

        /// <summary>
        /// Gets the process working set memory size.
        /// </summary>
        long WorkingSet { get; }

        /// <summary>
        /// Gets the process virtual memory size.
        /// </summary>
        long VirtualMemorySize { get; }

        /// <summary>
        /// Gets the process CPU time.
        /// </summary>
        TimeSpan TotalProcessorTime { get; }

        /// <summary>
        /// Gets the process user time.
        /// </summary>
        TimeSpan UserProcessorTime { get; }

        /// <summary>
        /// Gets the process privileged time.
        /// </summary>
        TimeSpan PrivilegedProcessorTime { get; }

        /// <summary>
        /// Waits for the process to exit.
        /// </summary>
        Task WaitForExitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Waits for the process to exit with a timeout.
        /// </summary>
        Task<bool> WaitForExitAsync(int milliseconds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kills the process.
        /// </summary>
        void Kill();

        /// <summary>
        /// Kills the process with force.
        /// </summary>
        void Kill(bool force);

        /// <summary>
        /// Refreshes the process information.
        /// </summary>
        void Refresh();
    }
}
