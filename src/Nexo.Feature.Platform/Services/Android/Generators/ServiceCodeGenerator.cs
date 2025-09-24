using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Models;

namespace Nexo.Feature.Platform.Services.Android.Generators
{
    public class ServiceCodeGenerator
    {
        private readonly ILogger<ServiceCodeGenerator> _logger;

        public ServiceCodeGenerator(ILogger<ServiceCodeGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<ServiceFile>> GenerateServiceFilesAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidGenerationOptions androidOptions,
            CancellationToken cancellationToken)
        {
            var files = new List<ServiceFile>();

            try
            {
                _logger.LogInformation("Generating Service files for {ServiceCount} services", applicationLogic.Services.Count);

                foreach (var service in applicationLogic.Services)
                {
                    var serviceFile = new ServiceFile
                    {
                        FileName = $"{service}Service.kt",
                        FilePath = $"service/{service}Service.kt",
                        Content = GenerateServiceCode(service),
                        ServiceType = ServiceType.Background
                    };
                    files.Add(serviceFile);
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Service files");
                return files;
            }
        }

        private string GenerateServiceCode(string serviceName)
        {
            return $@"package com.example.app.service

import android.app.Service
import android.content.Intent
import android.os.IBinder
import kotlinx.coroutines.*
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class {serviceName}Service @Inject constructor() : Service() {{

    private val serviceScope = CoroutineScope(Dispatchers.IO + SupervisorJob())

    override fun onBind(intent: Intent?): IBinder? {{
        return null
    }}

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {{
        when (intent?.action) {{
            ACTION_START_{serviceName.ToUpper()}_SERVICE -> {{
                start{serviceName}Processing()
            }}
            ACTION_STOP_{serviceName.ToUpper()}_SERVICE -> {{
                stop{serviceName}Processing()
            }}
        }}
        return START_STICKY
    }}

    private fun start{serviceName}Processing() {{
        serviceScope.launch {{
            try {{
                // {serviceName} processing logic
                process{serviceName}Data()
            }} catch (e: Exception) {{
                // Handle error
                handle{serviceName}Error(e)
            }}
        }}
    }}

    private fun stop{serviceName}Processing() {{
        serviceScope.cancel()
    }}

    private suspend fun process{serviceName}Data() {{
        // Implement {serviceName} data processing
        delay(1000) // Simulate processing
    }}

    private fun handle{serviceName}Error(error: Exception) {{
        // Handle {serviceName} service errors
    }}

    override fun onDestroy() {{
        super.onDestroy()
        serviceScope.cancel()
    }}

    companion object {{
        const val ACTION_START_{serviceName.ToUpper()}_SERVICE = ""com.example.app.START_{serviceName.ToUpper()}_SERVICE""
        const val ACTION_STOP_{serviceName.ToUpper()}_SERVICE = ""com.example.app.STOP_{serviceName.ToUpper()}_SERVICE""
    }}
}}";
        }
    }
}
