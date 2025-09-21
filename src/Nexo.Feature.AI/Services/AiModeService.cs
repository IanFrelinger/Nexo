using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Service for managing AI modes and their behavior
    /// </summary>
    public class AiModeService : IAiModeService
    {
        private readonly ILogger<AiModeService> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IAiConfigurationService _configurationService;
        private readonly Dictionary<AiMode, IAiModeHandler> _modeHandlers;

        public AiModeService(
            ILogger<AiModeService> logger,
            ILoggerFactory loggerFactory,
            IAiConfigurationService configurationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _modeHandlers = new Dictionary<AiMode, IAiModeHandler>
            {
                { AiMode.Off, new OffModeHandler(_loggerFactory.CreateLogger<OffModeHandler>()) },
                { AiMode.Assist, new AssistModeHandler(_loggerFactory.CreateLogger<AssistModeHandler>()) },
                { AiMode.Hybrid, new HybridModeHandler(_loggerFactory.CreateLogger<HybridModeHandler>()) },
                { AiMode.Embedded, new EmbeddedModeHandler(_loggerFactory.CreateLogger<EmbeddedModeHandler>()) }
            };
        }

        public async Task<AiMode> GetCurrentModeAsync(CancellationToken cancellationToken = default)
        {
            var config = await _configurationService.GetConfigurationAsync(cancellationToken);
            return config.Mode;
        }

        public async Task<bool> SetModeAsync(AiMode mode, CancellationToken cancellationToken = default)
        {
            try
            {
                var config = await _configurationService.GetConfigurationAsync(cancellationToken);
                var updatedConfig = config with { Mode = mode };
                await _configurationService.UpdateConfigurationAsync(updatedConfig, cancellationToken);
                
                _logger.LogInformation("AI mode changed to: {Mode}", mode);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set AI mode to: {Mode}", mode);
                return false;
            }
        }

        public async Task<bool> CanMakeNetworkCallsAsync(CancellationToken cancellationToken = default)
        {
            var mode = await GetCurrentModeAsync(cancellationToken);
            return mode is AiMode.Hybrid or AiMode.Embedded;
        }

        public async Task<bool> RequiresAiProcessingAsync(CancellationToken cancellationToken = default)
        {
            var mode = await GetCurrentModeAsync(cancellationToken);
            return mode is AiMode.Hybrid or AiMode.Embedded;
        }

        public async Task<bool> SupportsScaffoldGenerationAsync(CancellationToken cancellationToken = default)
        {
            var mode = await GetCurrentModeAsync(cancellationToken);
            return mode is AiMode.Assist or AiMode.Hybrid or AiMode.Embedded;
        }

        public async Task<AiModeResult> ProcessRequestAsync(
            string request,
            Dictionary<string, object> context,
            CancellationToken cancellationToken = default)
        {
            var mode = await GetCurrentModeAsync(cancellationToken);
            
            if (!_modeHandlers.TryGetValue(mode, out var handler))
            {
                throw new NotSupportedException($"AI mode {mode} is not supported");
            }

            return await handler.ProcessRequestAsync(request, context, cancellationToken);
        }
    }

    /// <summary>
    /// Interface for AI mode handlers
    /// </summary>
    public interface IAiModeHandler
    {
        Task<AiModeResult> ProcessRequestAsync(
            string request,
            Dictionary<string, object> context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Off mode handler - deterministic, no network calls
    /// </summary>
    public class OffModeHandler : IAiModeHandler
    {
        private readonly ILogger<OffModeHandler> _logger;

        public OffModeHandler(ILogger<OffModeHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AiModeResult> ProcessRequestAsync(
            string request,
            Dictionary<string, object> context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Processing request in Off mode (deterministic)");
            
            // Generate deterministic output based on request hash
            var requestHash = request.GetHashCode();
            var deterministicOutput = GenerateDeterministicOutput(request, requestHash);
            
            return new AiModeResult
            {
                Success = true,
                Output = deterministicOutput,
                Mode = AiMode.Off,
                NetworkCallsMade = 0,
                ProcessingTimeMs = 10, // Simulate quick processing
                IsDeterministic = true
            };
        }

        private string GenerateDeterministicOutput(string request, int hash)
        {
            // Generate consistent output based on request content
            var seed = Math.Abs(hash) % 1000;
            var random = new Random(seed);
            
            return request.ToLowerInvariant().Contains("triage") 
                ? "id,label,confidence\n1,urgent,0.95\n2,normal,0.87\n3,low,0.72"
                : request.ToLowerInvariant().Contains("summary")
                ? "This is a deterministic summary generated offline."
                : $"Deterministic output for: {request}";
        }
    }

    /// <summary>
    /// Assist mode handler - scaffold generation, no runtime AI
    /// </summary>
    public class AssistModeHandler : IAiModeHandler
    {
        private readonly ILogger<AssistModeHandler> _logger;

        public AssistModeHandler(ILogger<AssistModeHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AiModeResult> ProcessRequestAsync(
            string request,
            Dictionary<string, object> context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Processing request in Assist mode (scaffold generation)");
            
            // Generate scaffold outputs
            var scaffoldOutput = GenerateScaffoldOutput(request);
            
            return new AiModeResult
            {
                Success = true,
                Output = scaffoldOutput,
                Mode = AiMode.Assist,
                NetworkCallsMade = 0,
                ProcessingTimeMs = 50,
                IsDeterministic = false,
                ScaffoldGenerated = true
            };
        }

        private string GenerateScaffoldOutput(string request)
        {
            if (request.ToLowerInvariant().Contains("recipe"))
            {
                return "recipe: generated\nblocks: [triage, summarize]";
            }
            
            if (request.ToLowerInvariant().Contains("test"))
            {
                return "// Generated test code\n[Test]\npublic void GeneratedTest() { }";
            }
            
            return "// Generated scaffold code";
        }
    }

    /// <summary>
    /// Hybrid mode handler - optional local AI processing
    /// </summary>
    public class HybridModeHandler : IAiModeHandler
    {
        private readonly ILogger<HybridModeHandler> _logger;

        public HybridModeHandler(ILogger<HybridModeHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AiModeResult> ProcessRequestAsync(
            string request,
            Dictionary<string, object> context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Processing request in Hybrid mode (local AI)");
            
            // Simulate local AI processing
            var localOutput = await SimulateLocalAiProcessing(request, cancellationToken);
            
            return new AiModeResult
            {
                Success = true,
                Output = localOutput,
                Mode = AiMode.Hybrid,
                NetworkCallsMade = 0, // Local processing
                ProcessingTimeMs = 150,
                IsDeterministic = false,
                LocalAiUsed = true
            };
        }

        private async Task<string> SimulateLocalAiProcessing(string request, CancellationToken cancellationToken)
        {
            // Simulate local AI processing time
            await Task.Delay(100, cancellationToken);
            
            return request.ToLowerInvariant().Contains("triage")
                ? "id,label,confidence\n1,urgent,0.94\n2,normal,0.88\n3,low,0.71"
                : "This is a summary generated by local AI.";
        }
    }

    /// <summary>
    /// Embedded mode handler - required cloud AI processing
    /// </summary>
    public class EmbeddedModeHandler : IAiModeHandler
    {
        private readonly ILogger<EmbeddedModeHandler> _logger;

        public EmbeddedModeHandler(ILogger<EmbeddedModeHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AiModeResult> ProcessRequestAsync(
            string request,
            Dictionary<string, object> context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Processing request in Embedded mode (cloud AI)");
            
            // Simulate cloud AI processing
            var cloudOutput = await SimulateCloudAiProcessing(request, cancellationToken);
            
            return new AiModeResult
            {
                Success = true,
                Output = cloudOutput,
                Mode = AiMode.Embedded,
                NetworkCallsMade = 1, // Cloud processing
                ProcessingTimeMs = 200,
                IsDeterministic = false,
                CloudAiUsed = true
            };
        }

        private async Task<string> SimulateCloudAiProcessing(string request, CancellationToken cancellationToken)
        {
            // Simulate cloud AI processing time
            await Task.Delay(150, cancellationToken);
            
            return request.ToLowerInvariant().Contains("triage")
                ? "id,label,confidence\n1,urgent,0.93\n2,normal,0.89\n3,low,0.70"
                : "This is a summary generated by cloud AI.";
        }
    }

    /// <summary>
    /// Result of AI mode processing
    /// </summary>
    public class AiModeResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public AiMode Mode { get; set; }
        public int NetworkCallsMade { get; set; }
        public double ProcessingTimeMs { get; set; }
        public bool IsDeterministic { get; set; }
        public bool ScaffoldGenerated { get; set; }
        public bool LocalAiUsed { get; set; }
        public bool CloudAiUsed { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Interface for AI mode service
    /// </summary>
    public interface IAiModeService
    {
        Task<AiMode> GetCurrentModeAsync(CancellationToken cancellationToken = default);
        Task<bool> SetModeAsync(AiMode mode, CancellationToken cancellationToken = default);
        Task<bool> CanMakeNetworkCallsAsync(CancellationToken cancellationToken = default);
        Task<bool> RequiresAiProcessingAsync(CancellationToken cancellationToken = default);
        Task<bool> SupportsScaffoldGenerationAsync(CancellationToken cancellationToken = default);
        Task<AiModeResult> ProcessRequestAsync(
            string request,
            Dictionary<string, object> context,
            CancellationToken cancellationToken = default);
    }
}
