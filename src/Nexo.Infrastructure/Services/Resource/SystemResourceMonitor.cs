using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Shared.Interfaces.Resource;
using System.Runtime.InteropServices;

namespace Nexo.Infrastructure.Services.Resource
{
    /// <summary>
    /// System resource monitor implementation for CPU, memory, and I/O tracking.
    /// </summary>
    public class SystemResourceMonitor : IResourceMonitor
    {
        private readonly ILogger<SystemResourceMonitor> _logger;
        private readonly PerformanceCounter? _cpuCounter;
        private readonly Process _currentProcess;

        public SystemResourceMonitor(ILogger<SystemResourceMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentProcess = Process.GetCurrentProcess();

            // Initialize performance counters (platform-specific)
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                }
                else
                {
                    _logger.LogInformation("Performance counters not available on this platform, using alternative monitoring");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize performance counters, using fallback monitoring");
            }

            _logger.LogInformation("System resource monitor initialized");
        }

        /// <summary>
        /// Gets the current CPU usage percentage.
        /// </summary>
        public async Task<double> GetCpuUsageAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                if (_cpuCounter != null)
                {
                    // Use performance counter on Windows
                    return await Task.FromResult(_cpuCounter.NextValue());
                }
                else
                {
                    // Fallback method using process CPU time
                    return await GetCpuUsageFallbackAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CPU usage");
                return await GetCpuUsageFallbackAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Gets the current memory usage information.
        /// </summary>
        public async Task<MemoryInfo> GetMemoryInfoAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var memoryInfo = new MemoryInfo();

                // Cross-platform memory info using GC
                var totalMemory = GC.GetTotalMemory(false);
                var processMemory = _currentProcess.WorkingSet64;
                
                // Estimate total system memory (this is approximate)
                memoryInfo.TotalBytes = Environment.WorkingSet * 100; // Estimate
                memoryInfo.AvailableBytes = memoryInfo.TotalBytes - totalMemory;

                return await Task.FromResult(memoryInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting memory info");
                return new MemoryInfo();
            }
        }

        /// <summary>
        /// Gets the current disk usage information for a specific path.
        /// </summary>
        public async Task<DiskInfo> GetDiskInfoAsync(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    path = Environment.CurrentDirectory;
                }

                var diskInfo = new DiskInfo { Path = path };
                var driveInfo = new DriveInfo(Path.GetPathRoot(path) ?? path);
                
                diskInfo.TotalBytes = driveInfo.TotalSize;
                diskInfo.AvailableBytes = driveInfo.AvailableFreeSpace;

                return await Task.FromResult(diskInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting disk info for path: {Path}", path);
                return new DiskInfo { Path = path };
            }
        }

        /// <summary>
        /// Gets comprehensive resource usage information.
        /// </summary>
        public async Task<SystemResourceUsage> GetResourceUsageAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var cpuUsage = await GetCpuUsageAsync(cancellationToken);
                var memoryInfo = await GetMemoryInfoAsync(cancellationToken);
                var diskInfo = await GetDiskInfoAsync(Environment.CurrentDirectory, cancellationToken);

                return new SystemResourceUsage
                {
                    CpuUsagePercentage = cpuUsage,
                    Memory = memoryInfo,
                    Disk = diskInfo,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comprehensive resource usage");
                return new SystemResourceUsage
                {
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Fallback method for CPU usage calculation using process CPU time.
        /// </summary>
        private async Task<double> GetCpuUsageFallbackAsync(CancellationToken cancellationToken)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var startCpuTime = _currentProcess.TotalProcessorTime;

                // Wait a short time to measure CPU usage
                await Task.Delay(100, cancellationToken);

                var endTime = DateTime.UtcNow;
                var endCpuTime = _currentProcess.TotalProcessorTime;

                var cpuUsedMs = (endCpuTime - startCpuTime).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;

                // Calculate CPU usage as percentage
                var cpuUsagePercent = cpuUsedMs / totalMsPassed * 100;

                return Math.Min(100, Math.Max(0, cpuUsagePercent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CPU usage fallback calculation");
                return 0;
            }
        }

        public void Dispose()
        {
            _cpuCounter?.Dispose();
            _currentProcess?.Dispose();
        }
    }
} 