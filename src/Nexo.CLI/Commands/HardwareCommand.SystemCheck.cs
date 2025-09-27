using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// System check functionality for hardware command.
    /// </summary>
    public partial class HardwareCommand
    {
        /// <summary>
        /// Checks system requirements
        /// </summary>
        private async Task CheckSystemRequirementsAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("Search Checking System Requirements");
                Console.WriteLine();

                var capabilities = await _hardwareChecker.CheckSystemCapabilitiesAsync(cancellationToken);
                var requirements = await _hardwareChecker.GetHardwareRequirementsAsync(cancellationToken);

                Console.WriteLine("List Requirements Check:");
                Console.WriteLine();

                // Memory check
                var memoryStatus = capabilities.AvailableMemoryBytes >= requirements.MinimumMemoryBytes ? "SUCCESS:" : "ERROR:";
                Console.WriteLine($"   Memory: {memoryStatus} {FormatBytes(capabilities.AvailableMemoryBytes)} / {FormatBytes(requirements.MinimumMemoryBytes)} (min)");
                if (capabilities.AvailableMemoryBytes >= requirements.RecommendedMemoryBytes)
                {
                    Console.WriteLine($"   Memory: SUCCESS: {FormatBytes(capabilities.AvailableMemoryBytes)} / {FormatBytes(requirements.RecommendedMemoryBytes)} (recommended)");
                }

                // CPU check
                var cpuStatus = capabilities.CpuCores >= requirements.MinimumCpuCores ? "SUCCESS:" : "ERROR:";
                Console.WriteLine($"   CPU Cores: {cpuStatus} {capabilities.CpuCores} / {requirements.MinimumCpuCores} (min)");
                if (capabilities.CpuCores >= requirements.RecommendedCpuCores)
                {
                    Console.WriteLine($"   CPU Cores: SUCCESS: {capabilities.CpuCores} / {requirements.RecommendedCpuCores} (recommended)");
                }

                var cpuFreqStatus = capabilities.CpuFrequencyGhz >= requirements.MinimumCpuFrequencyGhz ? "SUCCESS:" : "ERROR:";
                Console.WriteLine($"   CPU Frequency: {cpuFreqStatus} {capabilities.CpuFrequencyGhz:F1} GHz / {requirements.MinimumCpuFrequencyGhz:F1} GHz (min)");

                // Storage check
                var storageStatus = capabilities.AvailableStorageBytes >= requirements.MinimumStorageBytes ? "SUCCESS:" : "ERROR:";
                Console.WriteLine($"   Storage: {storageStatus} {FormatBytes(capabilities.AvailableStorageBytes)} / {FormatBytes(requirements.MinimumStorageBytes)} (min)");
                if (capabilities.AvailableStorageBytes >= requirements.RecommendedStorageBytes)
                {
                    Console.WriteLine($"   Storage: SUCCESS: {FormatBytes(capabilities.AvailableStorageBytes)} / {FormatBytes(requirements.RecommendedStorageBytes)} (recommended)");
                }

                // OS check
                var osStatus = requirements.SupportedOperatingSystems.Any(os => capabilities.OperatingSystem.Contains(os)) ? "SUCCESS:" : "WARNING:";
                Console.WriteLine($"   Operating System: {osStatus} {capabilities.OperatingSystem}");

                // Architecture check
                var archStatus = requirements.SupportedArchitectures.Contains(capabilities.Architecture) ? "SUCCESS:" : "ERROR:";
                Console.WriteLine($"   Architecture: {archStatus} {capabilities.Architecture}");

                Console.WriteLine();
                Console.WriteLine($"Overall Status: {(capabilities.CanRunNexo ? "SUCCESS: System meets requirements" : "ERROR: System does not meet requirements")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking system requirements");
                Console.WriteLine($"ERROR: Error: {ex.Message}");
            }
        }
    }
}
