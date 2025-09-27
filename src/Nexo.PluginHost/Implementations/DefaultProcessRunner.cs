using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.PluginHost.Abstractions;

namespace Nexo.PluginHost.Implementations
{
    /// <summary>
    /// Default implementation of INexoProcessRunner that wraps System.Diagnostics.Process operations.
    /// </summary>
    public partial class DefaultProcessRunner : INexoProcessRunner
    {
        public async Task<INexoProcess> StartProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken = default)
        {
            var process = Process.Start(startInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start process");

            return new DefaultProcess(process);
        }

        public async Task<INexoProcess> StartProcessAsync(string fileName, string arguments = null, CancellationToken cancellationToken = default)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? string.Empty,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            return await StartProcessAsync(startInfo, cancellationToken);
        }

        public INexoProcess GetProcessById(int processId)
        {
            var process = Process.GetProcessById(processId);
            return new DefaultProcess(process);
        }

        public IEnumerable<INexoProcess> GetProcesses()
        {
            return Process.GetProcesses().Select(p => new DefaultProcess(p));
        }

        public IEnumerable<INexoProcess> GetProcessesByName(string processName)
        {
            return Process.GetProcessesByName(processName).Select(p => new DefaultProcess(p));
        }

        public INexoProcess GetCurrentProcess()
        {
            return new DefaultProcess(Process.GetCurrentProcess());
        }

        public bool IsProcessRunning(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        public void KillProcess(int processId)
        {
            var process = Process.GetProcessById(processId);
            process.Kill();
        }

        public void KillProcess(int processId, bool force)
        {
            var process = Process.GetProcessById(processId);
            if (force)
            {
                process.Kill(entireProcessTree: true);
            }
            else
            {
                process.Kill();
            }
        }
    }

    /// <summary>
    /// Default implementation of INexoProcess that wraps System.Diagnostics.Process.
    /// </summary>
    public partial class DefaultProcess : INexoProcess
    {
        private readonly Process _process;
        private bool _disposed;

        public DefaultProcess(Process process)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
        }

        public int Id => _process.Id;
        public string ProcessName => _process.ProcessName;
        public DateTime StartTime => _process.StartTime;
        public DateTime? ExitTime => _process.HasExited ? _process.ExitTime : null;
        public int ExitCode => _process.ExitCode;
        public bool HasExited => _process.HasExited;
        public long WorkingSet => _process.WorkingSet64;
        public long VirtualMemorySize => _process.VirtualMemorySize64;
        public TimeSpan TotalProcessorTime => _process.TotalProcessorTime;
        public TimeSpan UserProcessorTime => _process.UserProcessorTime;
        public TimeSpan PrivilegedProcessorTime => _process.PrivilegedProcessorTime;

        public async Task WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            await _process.WaitForExitAsync(cancellationToken);
        }

        public async Task<bool> WaitForExitAsync(int milliseconds, CancellationToken cancellationToken = default)
        {
            return await _process.WaitForExitAsync(milliseconds, cancellationToken);
        }

        public void Kill()
        {
            _process.Kill();
        }

        public void Kill(bool force)
        {
            if (force)
            {
                _process.Kill(entireProcessTree: true);
            }
            else
            {
                _process.Kill();
            }
        }

        public void Refresh()
        {
            _process.Refresh();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _process?.Dispose();
                _disposed = true;
            }
        }
    }
}
