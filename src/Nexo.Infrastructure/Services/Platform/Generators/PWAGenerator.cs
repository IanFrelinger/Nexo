using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Platform;

namespace Nexo.Infrastructure.Services.Platform.Generators
{
    /// <summary>
    /// Generates Progressive Web App (PWA) features for web applications
    /// </summary>
    public class PWAGenerator
    {
        private readonly ILogger<PWAGenerator> _logger;

        public PWAGenerator(ILogger<PWAGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generates PWA features from application logic
        /// </summary>
        public async Task<PWAGenerationResult> GeneratePWAFilesAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting PWA generation for application: {ApplicationName}", 
                applicationLogic.ApplicationName);

            var result = new PWAGenerationResult
            {
                Success = false,
                PWAFiles = new List<GeneratedPWAFile>()
            };

            try
            {
                // Generate service worker
                var serviceWorker = await GenerateServiceWorkerAsync(applicationLogic, options, cancellationToken);
                result.PWAFiles.Add(serviceWorker);

                // Generate manifest
                var manifest = await GenerateManifestAsync(applicationLogic, options, cancellationToken);
                result.PWAFiles.Add(manifest);

                // Generate offline page
                var offlinePage = await GenerateOfflinePageAsync(applicationLogic, options, cancellationToken);
                result.PWAFiles.Add(offlinePage);

                // Generate install prompt
                var installPrompt = await GenerateInstallPromptAsync(applicationLogic, options, cancellationToken);
                result.PWAFiles.Add(installPrompt);

                result.Success = true;
                _logger.LogInformation("PWA generation completed. Generated {Count} files", 
                    result.PWAFiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PWA generation");
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private async Task<GeneratedPWAFile> GenerateServiceWorkerAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var serviceWorker = new GeneratedPWAFile
            {
                Name = "ServiceWorker",
                Type = PWAFileType.ServiceWorker,
                Content = GenerateServiceWorkerContent(applicationLogic, options),
                FilePath = "public/sw.js"
            };

            await Task.Delay(100, cancellationToken); // Simulate work
            return serviceWorker;
        }

        private async Task<GeneratedPWAFile> GenerateManifestAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var manifest = new GeneratedPWAFile
            {
                Name = "Manifest",
                Type = PWAFileType.Manifest,
                Content = GenerateManifestContent(applicationLogic, options),
                FilePath = "public/manifest.json"
            };

            await Task.Delay(100, cancellationToken); // Simulate work
            return manifest;
        }

        private async Task<GeneratedPWAFile> GenerateOfflinePageAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var offlinePage = new GeneratedPWAFile
            {
                Name = "OfflinePage",
                Type = PWAFileType.OfflinePage,
                Content = GenerateOfflinePageContent(applicationLogic, options),
                FilePath = "public/offline.html"
            };

            await Task.Delay(100, cancellationToken); // Simulate work
            return offlinePage;
        }

        private async Task<GeneratedPWAFile> GenerateInstallPromptAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var installPrompt = new GeneratedPWAFile
            {
                Name = "InstallPrompt",
                Type = PWAFileType.InstallPrompt,
                Content = GenerateInstallPromptContent(applicationLogic, options),
                FilePath = "src/components/InstallPrompt.tsx"
            };

            await Task.Delay(100, cancellationToken); // Simulate work
            return installPrompt;
        }

        private string GenerateServiceWorkerContent(ApplicationLogic applicationLogic, WebGenerationOptions options)
        {
            return $@"
const CACHE_NAME = '{applicationLogic.ApplicationName.ToLower()}-v1';
const urlsToCache = [
  '/',
  '/static/js/bundle.js',
  '/static/css/main.css',
  '/manifest.json'
];

self.addEventListener('install', (event) => {{
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then((cache) => cache.addAll(urlsToCache))
  );
}});

self.addEventListener('fetch', (event) => {{
  event.respondWith(
    caches.match(event.request)
      .then((response) => {{
        if (response) {{
          return response;
        }}
        return fetch(event.request);
      }})
  );
}});";
        }

        private string GenerateManifestContent(ApplicationLogic applicationLogic, WebGenerationOptions options)
        {
            return $@"{{
  ""name"": ""{applicationLogic.ApplicationName}"",
  ""short_name"": ""{applicationLogic.ApplicationName}"",
  ""description"": ""{applicationLogic.Description}"",
  ""start_url"": ""/"",
  ""display"": ""standalone"",
  ""background_color"": ""#ffffff"",
  ""theme_color"": ""#000000"",
  ""icons"": [
    {{
      ""src"": ""/icon-192x192.png"",
      ""sizes"": ""192x192"",
      ""type"": ""image/png""
    }},
    {{
      ""src"": ""/icon-512x512.png"",
      ""sizes"": ""512x512"",
      ""type"": ""image/png""
    }}
  ]
}}";
        }

        private string GenerateOfflinePageContent(ApplicationLogic applicationLogic, WebGenerationOptions options)
        {
            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Offline - {applicationLogic.ApplicationName}</title>
</head>
<body>
    <div class=""offline-container"">
        <h1>You're offline</h1>
        <p>This app is not available offline. Please check your internet connection.</p>
        <button onclick=""window.location.reload()"">Try Again</button>
    </div>
</body>
</html>";
        }

        private string GenerateInstallPromptContent(ApplicationLogic applicationLogic, WebGenerationOptions options)
        {
            return $@"
import React, {{ useState, useEffect }} from 'react';

const InstallPrompt: React.FC = () => {{
  const [deferredPrompt, setDeferredPrompt] = useState<any>(null);
  const [showInstallPrompt, setShowInstallPrompt] = useState(false);

  useEffect(() => {{
    window.addEventListener('beforeinstallprompt', (e) => {{
      e.preventDefault();
      setDeferredPrompt(e);
      setShowInstallPrompt(true);
    }});
  }}, []);

  const handleInstallClick = async () => {{
    if (deferredPrompt) {{
      deferredPrompt.prompt();
      const {{ outcome }} = await deferredPrompt.userChoice;
      setDeferredPrompt(null);
      setShowInstallPrompt(false);
    }}
  }};

  if (!showInstallPrompt) return null;

  return (
    <div className=""install-prompt"">
      <p>Install {applicationLogic.ApplicationName} for a better experience!</p>
      <button onClick={{handleInstallClick}}>Install</button>
      <button onClick={{() => setShowInstallPrompt(false)}}>Not now</button>
    </div>
  );
}};

export default InstallPrompt;";
        }
    }

    /// <summary>
    /// Result of PWA generation
    /// </summary>
    public class PWAGenerationResult
    {
        public bool Success { get; set; }
        public List<GeneratedPWAFile> PWAFiles { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Generated PWA file
    /// </summary>
    public class GeneratedPWAFile
    {
        public string Name { get; set; } = string.Empty;
        public PWAFileType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }

    /// <summary>
    /// PWA file types
    /// </summary>
    public enum PWAFileType
    {
        ServiceWorker,
        Manifest,
        OfflinePage,
        InstallPrompt
    }
}
