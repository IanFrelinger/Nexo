using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;

namespace Nexo.Feature.Platform.Services.Detectors
{
    public class HardwareFeatureDetector
    {
        private readonly ILogger<HardwareFeatureDetector> _logger;

        public HardwareFeatureDetector(ILogger<HardwareFeatureDetector> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<PlatformFeature>> DetectHardwareFeaturesAsync(PlatformType platformType, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Detecting hardware features for {PlatformType}", platformType);

                var features = new List<PlatformFeature>();

                // CPU features
                features.AddRange(DetectCpuFeatures());

                // Memory features
                features.AddRange(DetectMemoryFeatures());

                // GPU features
                features.AddRange(DetectGpuFeatures());

                // Storage features
                features.AddRange(DetectStorageFeatures());

                // Network hardware features
                features.AddRange(DetectNetworkHardwareFeatures());

                return features;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting hardware features");
                return new List<PlatformFeature>();
            }
        }

        public async Task<HardwareCapabilities> DetectHardwareCapabilitiesAsync(PlatformType platformType, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Detecting hardware capabilities for {PlatformType}", platformType);

                return new HardwareCapabilities
                {
                    CpuCores = Environment.ProcessorCount,
                    TotalMemory = GC.GetTotalMemory(false),
                    GpuAcceleration = DetectGpuAcceleration(),
                    StorageSpace = GetAvailableStorageSpace(),
                    NetworkInterfaces = GetNetworkInterfaceCount()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting hardware capabilities");
                return new HardwareCapabilities();
            }
        }

        private List<PlatformFeature> DetectCpuFeatures()
        {
            var features = new List<PlatformFeature>();

            // CPU architecture
            features.Add(new PlatformFeature
            {
                Name = "CPU_Architecture",
                Type = FeatureType.Hardware,
                Value = RuntimeInformation.ProcessArchitecture.ToString(),
                IsAvailable = true,
                Description = "CPU architecture information"
            });

            // CPU cores
            features.Add(new PlatformFeature
            {
                Name = "CPU_Cores",
                Type = FeatureType.Hardware,
                Value = Environment.ProcessorCount.ToString(),
                IsAvailable = true,
                Description = "Number of CPU cores"
            });

            return features;
        }

        private List<PlatformFeature> DetectMemoryFeatures()
        {
            var features = new List<PlatformFeature>();

            // Total memory
            var totalMemory = GC.GetTotalMemory(false);
            features.Add(new PlatformFeature
            {
                Name = "Total_Memory",
                Type = FeatureType.Hardware,
                Value = totalMemory.ToString(),
                IsAvailable = true,
                Description = "Total available memory in bytes"
            });

            return features;
        }

        private List<PlatformFeature> DetectGpuFeatures()
        {
            var features = new List<PlatformFeature>();

            // GPU acceleration support
            var gpuSupport = DetectGpuAcceleration();
            features.Add(new PlatformFeature
            {
                Name = "GPU_Acceleration",
                Type = FeatureType.Hardware,
                Value = gpuSupport.ToString(),
                IsAvailable = gpuSupport,
                Description = "GPU acceleration support"
            });

            // CUDA support
            var cudaSupport = DetectCudaSupport();
            features.Add(new PlatformFeature
            {
                Name = "CUDA_Support",
                Type = FeatureType.Hardware,
                Value = cudaSupport.ToString(),
                IsAvailable = cudaSupport,
                Description = "CUDA GPU computing support"
            });

            return features;
        }

        private List<PlatformFeature> DetectStorageFeatures()
        {
            var features = new List<PlatformFeature>();

            // Available storage space
            var storageSpace = GetAvailableStorageSpace();
            features.Add(new PlatformFeature
            {
                Name = "Available_Storage",
                Type = FeatureType.Hardware,
                Value = storageSpace.ToString(),
                IsAvailable = true,
                Description = "Available storage space in bytes"
            });

            return features;
        }

        private List<PlatformFeature> DetectNetworkHardwareFeatures()
        {
            var features = new List<PlatformFeature>();

            // Network interface count
            var interfaceCount = GetNetworkInterfaceCount();
            features.Add(new PlatformFeature
            {
                Name = "Network_Interfaces",
                Type = FeatureType.Hardware,
                Value = interfaceCount.ToString(),
                IsAvailable = interfaceCount > 0,
                Description = "Number of network interfaces"
            });

            return features;
        }

        private bool DetectGpuAcceleration()
        {
            try
            {
                // Check for CUDA support
                if (DetectCudaSupport())
                    return true;

                // Check for OpenCL support
                if (DetectOpenClSupport())
                    return true;

                // Check for Metal support (macOS)
                if (DetectMetalSupport())
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool DetectCudaSupport()
        {
            try
            {
                var cudaPath = Environment.GetEnvironmentVariable("CUDA_PATH");
                return !string.IsNullOrEmpty(cudaPath);
            }
            catch
            {
                return false;
            }
        }

        private bool DetectOpenClSupport()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return System.IO.File.Exists("C:\\Windows\\System32\\OpenCL.dll");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return System.IO.File.Exists("/usr/lib/x86_64-linux-gnu/libOpenCL.so");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return System.IO.File.Exists("/System/Library/Frameworks/OpenCL.framework/OpenCL");
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool DetectMetalSupport()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return System.IO.File.Exists("/System/Library/Frameworks/Metal.framework/Metal");
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private long GetAvailableStorageSpace()
        {
            try
            {
                var drive = new System.IO.DriveInfo(System.IO.Directory.GetCurrentDirectory());
                return drive.AvailableFreeSpace;
            }
            catch
            {
                return 0;
            }
        }

        private int GetNetworkInterfaceCount()
        {
            try
            {
                return System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}
