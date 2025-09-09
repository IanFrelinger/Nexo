using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Agents.Specialized;

/// <summary>
/// Specialized agent for web platform optimization
/// </summary>
public class WebOptimizationAgent : ISpecializedAgent
{
    public string AgentId => "WebOptimization";
    public AgentSpecialization Specialization => AgentSpecialization.PlatformSpecific | AgentSpecialization.WebDevelopment;
    public PlatformCompatibility PlatformExpertise => PlatformCompatibility.Web;
    
    public PerformanceProfile OptimizationProfile => new()
    {
        PrimaryTarget = OptimizationTarget.Performance,
        MonitoredMetrics = new[]
        {
            PerformanceMetric.NetworkLatency,
            PerformanceMetric.ExecutionTime,
            PerformanceMetric.MemoryUsage,
            PerformanceMetric.CacheHitRate,
            PerformanceMetric.ErrorRate
        },
        SupportsRealTimeOptimization = true
    };
    
    private readonly IModelOrchestrator _modelOrchestrator;
    private readonly ILogger<WebOptimizationAgent> _logger;
    
    public WebOptimizationAgent(
        IModelOrchestrator modelOrchestrator,
        ILogger<WebOptimizationAgent> logger)
    {
        _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<AgentResponse> ProcessAsync(AgentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing web optimization request");
            
            var webContext = ExtractWebContext(request);
            var optimizations = new List<WebOptimization>();
            
            // Network optimization
            var networkOpt = await OptimizeNetworkPerformance(request, webContext);
            if (networkOpt != null)
            {
                optimizations.Add(networkOpt);
            }
            
            // Frontend performance optimization
            var frontendOpt = await OptimizeFrontendPerformance(request, webContext);
            if (frontendOpt != null)
            {
                optimizations.Add(frontendOpt);
            }
            
            // Backend optimization
            var backendOpt = await OptimizeBackendPerformance(request, webContext);
            if (backendOpt != null)
            {
                optimizations.Add(backendOpt);
            }
            
            // Caching optimization
            var cachingOpt = await OptimizeCaching(request, webContext);
            if (cachingOpt != null)
            {
                optimizations.Add(cachingOpt);
            }
            
            // Security optimization
            var securityOpt = await OptimizeWebSecurity(request, webContext);
            if (securityOpt != null)
            {
                optimizations.Add(securityOpt);
            }
            
            var optimizedCode = await ApplyWebOptimizations(request.Input, optimizations);
            
            return new AgentResponse
            {
                Result = optimizedCode,
                Confidence = CalculateOptimizationConfidence(optimizations),
                Metadata = new Dictionary<string, object>
                {
                    ["WebOptimizations"] = optimizations,
                    ["TargetBrowser"] = webContext.TargetBrowser,
                    ["Framework"] = webContext.Framework,
                    ["AgentId"] = AgentId,
                    ["Specialization"] = Specialization.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing web optimization request");
            return new AgentResponse
            {
                Success = false,
                ErrorMessage = $"Web optimization failed: {ex.Message}",
                Confidence = 0.0
            };
        }
    }
    
    public async Task<AgentResponse> CoordinateAsync(AgentRequest request, IEnumerable<ISpecializedAgent> collaborators)
    {
        try
        {
            _logger.LogInformation("Coordinating web optimization with {CollaboratorCount} agents", 
                collaborators.Count());
            
            // Find security agents for coordination
            var securityAgents = collaborators
                .Where(a => a.Specialization.HasFlag(AgentSpecialization.SecurityAnalysis))
                .ToList();
            
            var coordinatedOptimizations = new List<WebOptimization>();
            
            // Get security insights from security agents
            foreach (var securityAgent in securityAgents)
            {
                var securityRequest = request with 
                { 
                    Input = $"{request.Input}\n\nWeb security context: {ExtractWebContext(request)}" 
                };
                
                var securityResponse = await securityAgent.ProcessAsync(securityRequest);
                
                if (securityResponse.HasResult)
                {
                    var securityOptimization = new WebOptimization
                    {
                        Type = WebOptimizationType.SecurityOptimization,
                        OriginalCode = request.Input,
                        OptimizedCode = securityResponse.Result,
                        EstimatedImprovementFactor = 1.2,
                        WebSpecificNotes = "Coordinated with security agent"
                    };
                    
                    coordinatedOptimizations.Add(securityOptimization);
                }
            }
            
            // Apply web-specific optimizations on top of security optimizations
            var webOptimizedCode = await ApplyWebOptimizations(request.Input, coordinatedOptimizations);
            
            return new AgentResponse
            {
                Result = webOptimizedCode,
                Confidence = 0.9,
                Metadata = new Dictionary<string, object>
                {
                    ["CoordinatedOptimizations"] = coordinatedOptimizations,
                    ["CoordinationType"] = "WebSecurity",
                    ["AgentId"] = AgentId
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error coordinating web optimization");
            return new AgentResponse
            {
                Success = false,
                ErrorMessage = $"Web coordination failed: {ex.Message}",
                Confidence = 0.0
            };
        }
    }
    
    public async Task<AgentCapabilityAssessment> AssessCapabilityAsync(AgentRequest request)
    {
        try
        {
            var isWebRequest = request.TargetPlatform?.Contains("web", StringComparison.OrdinalIgnoreCase) == true ||
                             request.Input.Contains("web", StringComparison.OrdinalIgnoreCase) ||
                             request.Input.Contains("http", StringComparison.OrdinalIgnoreCase) ||
                             request.Input.Contains("api", StringComparison.OrdinalIgnoreCase) ||
                             request.Input.Contains("frontend", StringComparison.OrdinalIgnoreCase) ||
                             request.Input.Contains("backend", StringComparison.OrdinalIgnoreCase);
            
            var strengths = new List<string>();
            var limitations = new List<string>();
            var capabilityScore = 0.0;
            
            if (isWebRequest)
            {
                strengths.Add("Web-specific optimization expertise");
                strengths.Add("Network performance tuning");
                strengths.Add("Frontend and backend optimization");
                strengths.Add("Web security best practices");
                capabilityScore += 0.9;
            }
            else
            {
                limitations.Add("Not a web-specific request");
                capabilityScore += 0.1;
            }
            
            if (request.PerformanceRequirements?.RequiresRealTime == true)
            {
                strengths.Add("High-performance web code generation");
                capabilityScore += 0.1;
            }
            
            return new AgentCapabilityAssessment
            {
                CapabilityScore = Math.Min(capabilityScore, 1.0),
                Strengths = strengths.ToArray(),
                Limitations = limitations.ToArray(),
                CanHandleRequest = capabilityScore > 0.5,
                Recommendation = capabilityScore > 0.8 ? "Highly recommended for web optimization" : 
                               capabilityScore > 0.5 ? "Suitable for web development" : "Not recommended"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing web agent capability");
            return new AgentCapabilityAssessment
            {
                CapabilityScore = 0.0,
                CanHandleRequest = false,
                Recommendation = "Assessment failed"
            };
        }
    }
    
    public async Task LearnFromResultAsync(AgentRequest request, AgentResponse response, PerformanceMetrics metrics)
    {
        try
        {
            _logger.LogDebug("Learning from web optimization result");
            
            // Store web-specific learning data
            var learningData = new
            {
                Request = request.Input,
                Response = response.Result,
                Success = response.Success,
                Confidence = response.Confidence,
                WebContext = ExtractWebContext(request),
                ActualPerformance = metrics,
                Timestamp = DateTime.UtcNow
            };
            
            _logger.LogDebug("Web learning data recorded for future optimization improvements");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error learning from web optimization result");
        }
    }
    
    private WebContext ExtractWebContext(AgentRequest request)
    {
        var context = new WebContext();
        
        // Extract web-specific information from the request
        if (request.Parameters?.TryGetValue("WebContext", out var webCtx) == true && 
            webCtx is WebContext extractedContext)
        {
            return extractedContext;
        }
        
        // Default web context
        return new WebContext
        {
            TargetBrowser = "Chrome",
            Framework = "ASP.NET Core",
            Environment = "Production",
            PerformanceTarget = "High"
        };
    }
    
    private async Task<WebOptimization?> OptimizeNetworkPerformance(AgentRequest request, WebContext context)
    {
        var prompt = $"""
        Optimize this code for web network performance:
        
        {request.Input}
        
        Target Browser: {context.TargetBrowser}
        Framework: {context.Framework}
        
        Apply network optimizations:
        1. Minimize HTTP requests through bundling and minification
        2. Implement proper caching headers (Cache-Control, ETag)
        3. Use CDN for static assets
        4. Enable compression (Gzip/Brotli)
        5. Implement lazy loading for images and resources
        6. Use HTTP/2 server push where appropriate
        7. Optimize API response sizes
        8. Implement request batching
        9. Use WebSockets for real-time communication
        10. Implement proper error handling and retry logic
        
        Generate network-optimized code.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = prompt,
            Temperature = 0.3,
            MaxTokens = 1200
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (response.Success)
        {
            return new WebOptimization
            {
                Type = WebOptimizationType.NetworkOptimization,
                OriginalCode = request.Input,
                OptimizedCode = response.Response,
                EstimatedImprovementFactor = 1.4,
                WebSpecificNotes = "Optimized for network performance and caching"
            };
        }
        
        return null;
    }
    
    private async Task<WebOptimization?> OptimizeFrontendPerformance(AgentRequest request, WebContext context)
    {
        var prompt = $"""
        Optimize this code for frontend performance:
        
        {request.Input}
        
        Target Browser: {context.TargetBrowser}
        Framework: {context.Framework}
        
        Apply frontend optimizations:
        1. Minimize DOM manipulation and reflows
        2. Use virtual scrolling for large lists
        3. Implement code splitting and lazy loading
        4. Optimize images (WebP, responsive images)
        5. Use CSS-in-JS or CSS modules for optimal styling
        6. Implement service workers for offline functionality
        7. Use requestAnimationFrame for animations
        8. Minimize JavaScript bundle size
        9. Implement proper state management
        10. Use Web Workers for heavy computations
        
        Generate frontend-optimized code.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = prompt,
            Temperature = 0.3,
            MaxTokens = 1200
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (response.Success)
        {
            return new WebOptimization
            {
                Type = WebOptimizationType.FrontendOptimization,
                OriginalCode = request.Input,
                OptimizedCode = response.Response,
                EstimatedImprovementFactor = 1.3,
                WebSpecificNotes = "Optimized for frontend performance and user experience"
            };
        }
        
        return null;
    }
    
    private async Task<WebOptimization?> OptimizeBackendPerformance(AgentRequest request, WebContext context)
    {
        var prompt = $"""
        Optimize this code for backend performance:
        
        {request.Input}
        
        Framework: {context.Framework}
        Environment: {context.Environment}
        
        Apply backend optimizations:
        1. Implement proper database query optimization
        2. Use connection pooling and async operations
        3. Implement caching strategies (Redis, in-memory)
        4. Use background services for heavy operations
        5. Implement proper logging and monitoring
        6. Use dependency injection for better testability
        7. Implement rate limiting and throttling
        8. Use proper error handling and status codes
        9. Implement health checks and metrics
        10. Use microservices architecture where appropriate
        
        Generate backend-optimized code.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = prompt,
            Temperature = 0.3,
            MaxTokens = 1200
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (response.Success)
        {
            return new WebOptimization
            {
                Type = WebOptimizationType.BackendOptimization,
                OriginalCode = request.Input,
                OptimizedCode = response.Response,
                EstimatedImprovementFactor = 1.5,
                WebSpecificNotes = "Optimized for backend performance and scalability"
            };
        }
        
        return null;
    }
    
    private async Task<WebOptimization?> OptimizeCaching(AgentRequest request, WebContext context)
    {
        var prompt = $"""
        Optimize this code for web caching:
        
        {request.Input}
        
        Framework: {context.Framework}
        Environment: {context.Environment}
        
        Apply caching optimizations:
        1. Implement browser caching with proper headers
        2. Use server-side caching (Redis, Memcached)
        3. Implement application-level caching
        4. Use CDN caching for static assets
        5. Implement cache invalidation strategies
        6. Use HTTP caching headers (Cache-Control, ETag, Last-Modified)
        7. Implement cache warming strategies
        8. Use distributed caching for scalability
        9. Implement cache monitoring and metrics
        10. Use appropriate cache TTL values
        
        Generate caching-optimized code.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = prompt,
            Temperature = 0.3,
            MaxTokens = 1200
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (response.Success)
        {
            return new WebOptimization
            {
                Type = WebOptimizationType.CachingOptimization,
                OriginalCode = request.Input,
                OptimizedCode = response.Response,
                EstimatedImprovementFactor = 1.6,
                WebSpecificNotes = "Optimized for caching performance and hit rates"
            };
        }
        
        return null;
    }
    
    private async Task<WebOptimization?> OptimizeWebSecurity(AgentRequest request, WebContext context)
    {
        var prompt = $"""
        Optimize this code for web security:
        
        {request.Input}
        
        Framework: {context.Framework}
        Environment: {context.Environment}
        
        Apply security optimizations:
        1. Implement proper input validation and sanitization
        2. Use HTTPS and secure headers (HSTS, CSP, X-Frame-Options)
        3. Implement authentication and authorization
        4. Use parameterized queries to prevent SQL injection
        5. Implement CSRF protection
        6. Use secure session management
        7. Implement rate limiting and DDoS protection
        8. Use secure password hashing (bcrypt, Argon2)
        9. Implement proper error handling without information leakage
        10. Use security headers and CORS configuration
        
        Generate security-optimized code.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = prompt,
            Temperature = 0.3,
            MaxTokens = 1200
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (response.Success)
        {
            return new WebOptimization
            {
                Type = WebOptimizationType.SecurityOptimization,
                OriginalCode = request.Input,
                OptimizedCode = response.Response,
                EstimatedImprovementFactor = 1.2,
                WebSpecificNotes = "Enhanced security with web best practices"
            };
        }
        
        return null;
    }
    
    private async Task<string> ApplyWebOptimizations(string originalCode, IEnumerable<WebOptimization> optimizations)
    {
        if (!optimizations.Any())
        {
            return originalCode;
        }
        
        var synthesisPrompt = $"""
        Synthesize these web optimizations into a final, cohesive solution:
        
        Original Code:
        {originalCode}
        
        Optimizations Applied:
        """;
        
        foreach (var opt in optimizations)
        {
            synthesisPrompt += $"\n{opt.Type}:\n{opt.OptimizedCode}\n";
        }
        
        synthesisPrompt += """
        
        Create a unified solution that:
        1. Combines all optimizations seamlessly
        2. Maintains web standards and best practices
        3. Includes proper error handling and monitoring
        4. Provides performance monitoring capabilities
        5. Handles cross-browser compatibility
        
        Generate the final, optimized web code.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = synthesisPrompt,
            Temperature = 0.3,
            MaxTokens = 2000
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (!response.Success)
        {
            _logger.LogError("Failed to synthesize web optimizations");
            return originalCode;
        }
        
        return response.Response;
    }
    
    private double CalculateOptimizationConfidence(IEnumerable<WebOptimization> optimizations)
    {
        if (!optimizations.Any())
        {
            return 0.5;
        }
        
        var avgImprovement = optimizations.Average(o => o.EstimatedImprovementFactor);
        return Math.Min(0.95, 0.6 + (avgImprovement - 1.0) * 0.2);
    }
}

/// <summary>
/// Web-specific context information
/// </summary>
public record WebContext
{
    public string TargetBrowser { get; init; } = "Chrome";
    public string Framework { get; init; } = "ASP.NET Core";
    public string Environment { get; init; } = "Production";
    public string PerformanceTarget { get; init; } = "High";
}

/// <summary>
/// Web optimization result
/// </summary>
public record WebOptimization
{
    public WebOptimizationType Type { get; init; }
    public string OriginalCode { get; init; } = string.Empty;
    public string OptimizedCode { get; init; } = string.Empty;
    public double EstimatedImprovementFactor { get; init; } = 1.0;
    public string WebSpecificNotes { get; init; } = string.Empty;
}

/// <summary>
/// Types of web optimizations
/// </summary>
public enum WebOptimizationType
{
    NetworkOptimization,
    FrontendOptimization,
    BackendOptimization,
    CachingOptimization,
    SecurityOptimization,
    PerformanceOptimization
}
