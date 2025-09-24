using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Platform.Services.Generators;

/// <summary>
/// Generates Progressive Web App features
/// </summary>
public class PWAGenerator
{
    private readonly ILogger<PWAGenerator> _logger;

    public PWAGenerator(ILogger<PWAGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProgressiveWebAppResult> GenerateProgressiveWebAppFeaturesAsync(
        StandardizedApplicationLogic applicationLogic,
        WebGenerationOptions webOptions,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        if (applicationLogic == null)
            throw new ArgumentNullException(nameof(applicationLogic));
        
        if (webOptions == null)
            throw new ArgumentNullException(nameof(webOptions));

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting Progressive Web App features generation");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = new ProgressiveWebAppResult
            {
                Success = true,
                GeneratedAt = DateTime.UtcNow
            };

            // Generate PWA features
            var pwaFeatures = await GeneratePWAFeaturesAsync(applicationLogic, webOptions, cancellationToken);
            result.PWAFeatures = pwaFeatures;

            // Generate service worker
            result.ServiceWorker = GenerateServiceWorker(applicationLogic, webOptions);

            // Generate manifest
            result.Manifest = GenerateManifest(applicationLogic, webOptions);

            // Generate offline support
            result.OfflineSupport = GenerateOfflineSupport(applicationLogic, webOptions);

            stopwatch.Stop();
            result.Success = true;
            result.Message = $"Progressive Web App features generation completed in {stopwatch.ElapsedMilliseconds}ms";
            result.GenerationTime = stopwatch.Elapsed;

            _logger.LogInformation("Progressive Web App features generation completed successfully");
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Progressive Web App features generation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Progressive Web App features generation");
            return new ProgressiveWebAppResult
            {
                Success = false,
                Message = $"Progressive Web App features generation failed: {ex.Message}",
                GenerationTime = stopwatch.Elapsed
            };
        }
    }

    private async Task<List<WebUIPattern>> GeneratePWAFeaturesAsync(
        StandardizedApplicationLogic applicationLogic,
        WebGenerationOptions webOptions,
        CancellationToken cancellationToken)
    {
        var features = new List<WebUIPattern>();

        try
        {
            _logger.LogDebug("Generating PWA features for application");

            // Generate push notifications
            var pushNotifications = new WebUIPattern
            {
                Name = "PushNotifications",
                Type = WebUIPatternType.PushNotifications,
                Content = GeneratePushNotificationsFeature(applicationLogic, webOptions),
                GeneratedAt = DateTime.UtcNow
            };
            features.Add(pushNotifications);

            // Generate background sync
            var backgroundSync = new WebUIPattern
            {
                Name = "BackgroundSync",
                Type = WebUIPatternType.BackgroundSync,
                Content = GenerateBackgroundSyncFeature(applicationLogic, webOptions),
                GeneratedAt = DateTime.UtcNow
            };
            features.Add(backgroundSync);

            // Generate install prompt
            var installPrompt = new WebUIPattern
            {
                Name = "InstallPrompt",
                Type = WebUIPatternType.InstallPrompt,
                Content = GenerateInstallPromptFeature(applicationLogic, webOptions),
                GeneratedAt = DateTime.UtcNow
            };
            features.Add(installPrompt);

            return features;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PWA features");
            return features;
        }
    }

    private string GenerateServiceWorker(StandardizedApplicationLogic applicationLogic, WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("const CACHE_NAME = 'app-cache-v1';");
        sb.AppendLine("const urlsToCache = [");
        sb.AppendLine("  '/',");
        sb.AppendLine("  '/static/js/bundle.js',");
        sb.AppendLine("  '/static/css/main.css'");
        sb.AppendLine("];");
        sb.AppendLine();
        sb.AppendLine("self.addEventListener('install', (event) => {");
        sb.AppendLine("  event.waitUntil(");
        sb.AppendLine("    caches.open(CACHE_NAME)");
        sb.AppendLine("      .then((cache) => cache.addAll(urlsToCache))");
        sb.AppendLine("  );");
        sb.AppendLine("});");
        sb.AppendLine();
        sb.AppendLine("self.addEventListener('fetch', (event) => {");
        sb.AppendLine("  event.respondWith(");
        sb.AppendLine("    caches.match(event.request)");
        sb.AppendLine("      .then((response) => {");
        sb.AppendLine("        if (response) {");
        sb.AppendLine("          return response;");
        sb.AppendLine("        }");
        sb.AppendLine("        return fetch(event.request);");
        sb.AppendLine("      })");
        sb.AppendLine("  );");
        sb.AppendLine("});");
        
        return sb.ToString();
    }

    private string GenerateManifest(StandardizedApplicationLogic applicationLogic, WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("{");
        sb.AppendLine("  \"name\": \"My Progressive Web App\",");
        sb.AppendLine("  \"short_name\": \"MyPWA\",");
        sb.AppendLine("  \"description\": \"A progressive web application\",");
        sb.AppendLine("  \"start_url\": \"/\",");
        sb.AppendLine("  \"display\": \"standalone\",");
        sb.AppendLine("  \"background_color\": \"#ffffff\",");
        sb.AppendLine("  \"theme_color\": \"#000000\",");
        sb.AppendLine("  \"icons\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"src\": \"/icon-192x192.png\",");
        sb.AppendLine("      \"sizes\": \"192x192\",");
        sb.AppendLine("      \"type\": \"image/png\"");
        sb.AppendLine("    },");
        sb.AppendLine("    {");
        sb.AppendLine("      \"src\": \"/icon-512x512.png\",");
        sb.AppendLine("      \"sizes\": \"512x512\",");
        sb.AppendLine("      \"type\": \"image/png\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private string GenerateOfflineSupport(StandardizedApplicationLogic applicationLogic, WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("class OfflineSupport {");
        sb.AppendLine("  constructor() {");
        sb.AppendLine("    this.isOnline = navigator.onLine;");
        sb.AppendLine("    this.setupEventListeners();");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  setupEventListeners() {");
        sb.AppendLine("    window.addEventListener('online', () => {");
        sb.AppendLine("      this.isOnline = true;");
        sb.AppendLine("      this.syncPendingData();");
        sb.AppendLine("    });");
        sb.AppendLine();
        sb.AppendLine("    window.addEventListener('offline', () => {");
        sb.AppendLine("      this.isOnline = false;");
        sb.AppendLine("      this.showOfflineMessage();");
        sb.AppendLine("    });");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  async syncPendingData() {");
        sb.AppendLine("    // Sync any pending data when back online");
        sb.AppendLine("    console.log('Syncing pending data...');");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  showOfflineMessage() {");
        sb.AppendLine("    // Show offline message to user");
        sb.AppendLine("    console.log('You are offline');");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("export default OfflineSupport;");
        
        return sb.ToString();
    }

    private string GeneratePushNotificationsFeature(StandardizedApplicationLogic applicationLogic, WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("class PushNotificationManager {");
        sb.AppendLine("  constructor() {");
        sb.AppendLine("    this.isSupported = 'Notification' in window && 'serviceWorker' in navigator;");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  async requestPermission() {");
        sb.AppendLine("    if (!this.isSupported) {");
        sb.AppendLine("      throw new Error('Push notifications not supported');");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    const permission = await Notification.requestPermission();");
        sb.AppendLine("    return permission === 'granted';");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  async subscribe() {");
        sb.AppendLine("    const registration = await navigator.serviceWorker.ready;");
        sb.AppendLine("    const subscription = await registration.pushManager.subscribe({");
        sb.AppendLine("      userVisibleOnly: true,");
        sb.AppendLine("      applicationServerKey: 'your-vapid-public-key'");
        sb.AppendLine("    });");
        sb.AppendLine();
        sb.AppendLine("    return subscription;");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("export default PushNotificationManager;");
        
        return sb.ToString();
    }

    private string GenerateBackgroundSyncFeature(StandardizedApplicationLogic applicationLogic, WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("class BackgroundSyncManager {");
        sb.AppendLine("  constructor() {");
        sb.AppendLine("    this.pendingActions = [];");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  async registerBackgroundSync(tag, data) {");
        sb.AppendLine("    if ('serviceWorker' in navigator && 'sync' in window.ServiceWorkerRegistration.prototype) {");
        sb.AppendLine("      const registration = await navigator.serviceWorker.ready;");
        sb.AppendLine("      await registration.sync.register(tag);");
        sb.AppendLine("      this.pendingActions.push({ tag, data });");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  async processPendingActions() {");
        sb.AppendLine("    for (const action of this.pendingActions) {");
        sb.AppendLine("      try {");
        sb.AppendLine("        await this.executeAction(action);");
        sb.AppendLine("        this.pendingActions = this.pendingActions.filter(a => a !== action);");
        sb.AppendLine("      } catch (error) {");
        sb.AppendLine("        console.error('Failed to process pending action:', error);");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  async executeAction(action) {");
        sb.AppendLine("    // Execute the background sync action");
        sb.AppendLine("    console.log('Executing background sync:', action.tag);");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("export default BackgroundSyncManager;");
        
        return sb.ToString();
    }

    private string GenerateInstallPromptFeature(StandardizedApplicationLogic applicationLogic, WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("class InstallPromptManager {");
        sb.AppendLine("  constructor() {");
        sb.AppendLine("    this.deferredPrompt = null;");
        sb.AppendLine("    this.setupEventListeners();");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  setupEventListeners() {");
        sb.AppendLine("    window.addEventListener('beforeinstallprompt', (e) => {");
        sb.AppendLine("      e.preventDefault();");
        sb.AppendLine("      this.deferredPrompt = e;");
        sb.AppendLine("      this.showInstallButton();");
        sb.AppendLine("    });");
        sb.AppendLine();
        sb.AppendLine("    window.addEventListener('appinstalled', () => {");
        sb.AppendLine("      console.log('PWA was installed');");
        sb.AppendLine("      this.hideInstallButton();");
        sb.AppendLine("    });");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  async showInstallPrompt() {");
        sb.AppendLine("    if (this.deferredPrompt) {");
        sb.AppendLine("      this.deferredPrompt.prompt();");
        sb.AppendLine("      const { outcome } = await this.deferredPrompt.userChoice;");
        sb.AppendLine("      console.log(`User response to the install prompt: ${outcome}`);");
        sb.AppendLine("      this.deferredPrompt = null;");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  showInstallButton() {");
        sb.AppendLine("    // Show install button in UI");
        sb.AppendLine("    console.log('Show install button');");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  hideInstallButton() {");
        sb.AppendLine("    // Hide install button from UI");
        sb.AppendLine("    console.log('Hide install button');");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("export default InstallPromptManager;");
        
        return sb.ToString();
    }
}
