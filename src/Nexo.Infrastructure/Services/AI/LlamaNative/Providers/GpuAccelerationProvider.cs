using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Services.AI.LlamaNative.Providers
{
    public class GpuAccelerationProvider
    {
        private readonly ILogger<GpuAccelerationProvider> _logger;
        private bool _isInitialized = false;

        public GpuAccelerationProvider(ILogger<GpuAccelerationProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool DetectGpuSupport()
        {
            try
            {
                _logger.LogInformation("Detecting GPU support");

                // Check for CUDA support
                if (DetectCudaSupport())
                {
                    _logger.LogInformation("CUDA GPU support detected");
                    return true;
                }

                // Check for OpenCL support
                if (DetectOpenClSupport())
                {
                    _logger.LogInformation("OpenCL GPU support detected");
                    return true;
                }

                // Check for Metal support (macOS)
                if (DetectMetalSupport())
                {
                    _logger.LogInformation("Metal GPU support detected");
                    return true;
                }

                _logger.LogInformation("No GPU acceleration support detected");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting GPU support");
                return false;
            }
        }

        public async Task<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_isInitialized)
                    return true;

                _logger.LogInformation("Initializing GPU acceleration provider");

                // Simulate GPU initialization
                await Task.Delay(200, cancellationToken);

                var gpuSupported = DetectGpuSupport();
                if (gpuSupported)
                {
                    _logger.LogInformation("GPU acceleration initialized successfully");
                }
                else
                {
                    _logger.LogInformation("GPU acceleration not available, using CPU");
                }

                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing GPU acceleration");
                return false;
            }
        }

        public bool IsGpuAccelerationAvailable()
        {
            return _isInitialized && DetectGpuSupport();
        }

        public string GetGpuInfo()
        {
            try
            {
                if (DetectCudaSupport())
                {
                    return GetCudaInfo();
                }
                else if (DetectOpenClSupport())
                {
                    return GetOpenClInfo();
                }
                else if (DetectMetalSupport())
                {
                    return GetMetalInfo();
                }
                else
                {
                    return "No GPU acceleration available";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting GPU info");
                return $"Error: {ex.Message}";
            }
        }

        private bool DetectCudaSupport()
        {
            try
            {
                // Check for CUDA runtime
                var cudaPath = Environment.GetEnvironmentVariable("CUDA_PATH");
                if (!string.IsNullOrEmpty(cudaPath))
                {
                    return true;
                }

                // Check for CUDA libraries
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return System.IO.File.Exists("C:\\Program Files\\NVIDIA GPU Computing Toolkit\\CUDA");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return System.IO.Directory.Exists("/usr/local/cuda");
                }

                return false;
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
                // Check for OpenCL libraries
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
                // Check for Metal support on macOS
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

        private string GetCudaInfo()
        {
            try
            {
                var cudaVersion = Environment.GetEnvironmentVariable("CUDA_VERSION") ?? "Unknown";
                return $"CUDA {cudaVersion} - NVIDIA GPU acceleration available";
            }
            catch
            {
                return "CUDA - NVIDIA GPU acceleration available";
            }
        }

        private string GetOpenClInfo()
        {
            return "OpenCL - Cross-platform GPU acceleration available";
        }

        private string GetMetalInfo()
        {
            return "Metal - Apple GPU acceleration available";
        }
    }
}
