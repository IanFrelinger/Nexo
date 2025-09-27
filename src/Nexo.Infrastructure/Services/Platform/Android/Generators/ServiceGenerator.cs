using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Services.Platform.Android.Generators
{
    public partial class ServiceGenerator
    {
        private readonly ILogger<ServiceGenerator> _logger;

        public ServiceGenerator(ILogger<ServiceGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<GeneratedFile>> GenerateServicesAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var files = new List<GeneratedFile>();

            try
            {
                _logger.LogInformation("Generating Services for {ServiceCount} services", applicationLogic.Services.Count);

                foreach (var service in applicationLogic.Services)
                {
                    var serviceFile = new GeneratedFile
                    {
                        Name = $"{service}Service.kt",
                        Content = GenerateServiceCode(service),
                        Type = "Service"
                    };
                    files.Add(serviceFile);
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Services");
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
