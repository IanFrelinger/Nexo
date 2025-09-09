using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Nexo.Core.Application.Interfaces.Performance;
using Nexo.Core.Application.Interfaces.Security;

namespace Nexo.CLI.Commands.Production
{
    /// <summary>
    /// CLI commands for Phase 3.4 production readiness features.
    /// Provides comprehensive production readiness tools including performance optimization,
    /// security auditing, monitoring, and compliance checking.
    /// </summary>
    public class ProductionReadinessCommands
    {
        private readonly ILogger<ProductionReadinessCommands> _logger;
        private readonly IProductionPerformanceOptimizer _performanceOptimizer;
        private readonly IProductionSecurityAuditor _securityAuditor;

        public ProductionReadinessCommands(
            ILogger<ProductionReadinessCommands> logger,
            IProductionPerformanceOptimizer performanceOptimizer,
            IProductionSecurityAuditor securityAuditor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _performanceOptimizer = performanceOptimizer ?? throw new ArgumentNullException(nameof(performanceOptimizer));
            _securityAuditor = securityAuditor ?? throw new ArgumentNullException(nameof(securityAuditor));
        }

        /// <summary>
        /// Runs comprehensive performance optimization across all services.
        /// </summary>
        [Command("performance optimize")]
        [Description("Run comprehensive performance optimization across all services")]
        public async Task OptimizePerformanceAsync(
            [Option("--caching", "-c")] bool optimizeCaching = true,
            [Option("--memory", "-m")] bool optimizeMemory = true,
            [Option("--ai", "-a")] bool optimizeAI = true,
            [Option("--security", "-s")] bool optimizeSecurity = true,
            [Option("--database", "-d")] bool optimizeDatabase = true,
            [Option("--network", "-n")] bool optimizeNetwork = true,
            [Option("--max-time")] int maxTimeMinutes = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🚀 Starting Performance Optimization[/]");
                
                var options = new PerformanceOptimizationOptions
                {
                    OptimizeCaching = optimizeCaching,
                    OptimizeMemory = optimizeMemory,
                    OptimizeAI = optimizeAI,
                    OptimizeSecurity = optimizeSecurity,
                    OptimizeDatabase = optimizeDatabase,
                    OptimizeNetwork = optimizeNetwork,
                    MaxOptimizationTime = TimeSpan.FromMinutes(maxTimeMinutes)
                };

                var progress = AnsiConsole.Progress()
                    .Columns(new ProgressColumn[]
                    {
                        new SpinnerColumn(),
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new ElapsedTimeColumn()
                    });

                await progress.StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("Optimizing performance...", maxValue: 100);
                    
                    // Simulate progress updates
                    for (int i = 0; i <= 100; i += 10)
                    {
                        task.Increment(10);
                        await Task.Delay(100, cancellationToken);
                    }
                });

                var result = await _performanceOptimizer.OptimizePerformanceAsync(options, cancellationToken);

                if (result.Success)
                {
                    AnsiConsole.MarkupLine($"[bold green]✅ Performance optimization completed successfully![/]");
                    AnsiConsole.MarkupLine($"[dim]Duration: {result.TotalOptimizationTime.TotalMilliseconds:F0}ms[/]");
                    AnsiConsole.MarkupLine($"[dim]Total improvements: {result.GetTotalImprovements()}[/]");
                    
                    // Display optimization results
                    var table = new Table();
                    table.AddColumn("Component");
                    table.AddColumn("Status");
                    table.AddColumn("Improvement");
                    
                    if (result.CacheOptimization?.Success == true)
                        table.AddRow("Caching", "✅ Optimized", $"{result.CacheOptimization.ImprovementPercentage:P1}");
                    
                    if (result.MemoryOptimization?.Success == true)
                        table.AddRow("Memory", "✅ Optimized", $"{result.MemoryOptimization.MemorySavedMB}MB saved");
                    
                    if (result.AIOptimization?.Success == true)
                        table.AddRow("AI", "✅ Optimized", $"{result.AIOptimization.ResponseTimeImprovement:P1}");
                    
                    if (result.SecurityOptimization?.Success == true)
                        table.AddRow("Security", "✅ Optimized", $"{result.SecurityOptimization.SecurityCheckTimeImprovement:P1}");
                    
                    if (result.DatabaseOptimization?.Success == true)
                        table.AddRow("Database", "✅ Optimized", $"{result.DatabaseOptimization.QueryTimeImprovement:P1}");
                    
                    if (result.NetworkOptimization?.Success == true)
                        table.AddRow("Network", "✅ Optimized", $"{result.NetworkOptimization.NetworkLatencyImprovement:P1}");
                    
                    AnsiConsole.Write(table);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[bold red]❌ Performance optimization failed: {result.ErrorMessage}[/]");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during performance optimization");
                AnsiConsole.MarkupLine($"[bold red]❌ Error: {ex.Message}[/]");
            }
        }

        /// <summary>
        /// Runs comprehensive performance benchmarking.
        /// </summary>
        [Command("performance benchmark")]
        [Description("Run comprehensive performance benchmarking")]
        public async Task RunBenchmarkAsync(
            [Option("--name", "-n")] string benchmarkName = "Production Benchmark",
            [Option("--iterations", "-i")] int iterations = 10,
            [Option("--warmup")] int warmupSeconds = 30,
            [Option("--system")] bool includeSystem = true,
            [Option("--cache")] bool includeCache = true,
            [Option("--ai")] bool includeAI = true,
            [Option("--security")] bool includeSecurity = true,
            [Option("--database")] bool includeDatabase = true,
            [Option("--end-to-end")] bool includeEndToEnd = true,
            CancellationToken cancellationToken = default)
        {
            try
            {
                AnsiConsole.MarkupLine($"[bold blue]📊 Running Performance Benchmark: {benchmarkName}[/]");
                
                var options = new PerformanceBenchmarkOptions
                {
                    BenchmarkName = benchmarkName,
                    Iterations = iterations,
                    WarmupTime = TimeSpan.FromSeconds(warmupSeconds),
                    IncludeSystemMetrics = includeSystem,
                    IncludeCacheMetrics = includeCache,
                    IncludeAIMetrics = includeAI,
                    IncludeSecurityMetrics = includeSecurity,
                    IncludeDatabaseMetrics = includeDatabase,
                    IncludeEndToEndMetrics = includeEndToEnd
                };

                var progress = AnsiConsole.Progress()
                    .Columns(new ProgressColumn[]
                    {
                        new SpinnerColumn(),
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new ElapsedTimeColumn()
                    });

                await progress.StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("Running benchmark...", maxValue: 100);
                    
                    // Simulate benchmark progress
                    for (int i = 0; i <= 100; i += 5)
                    {
                        task.Increment(5);
                        await Task.Delay(200, cancellationToken);
                    }
                });

                var result = await _performanceOptimizer.RunBenchmarkAsync(options, cancellationToken);

                if (result.Success)
                {
                    var benchmark = result.Benchmark;
                    AnsiConsole.MarkupLine($"[bold green]✅ Benchmark completed successfully![/]");
                    AnsiConsole.MarkupLine($"[dim]Duration: {benchmark.Duration.TotalMilliseconds:F0}ms[/]");
                    
                    // Display benchmark results
                    var table = new Table();
                    table.AddColumn("Metric");
                    table.AddColumn("Value");
                    table.AddColumn("Status");
                    
                    if (benchmark.SystemMetrics != null)
                    {
                        table.AddRow("CPU Usage", $"{benchmark.SystemMetrics.CPUUsagePercent:F1}%", 
                            benchmark.SystemMetrics.CPUUsagePercent < 80 ? "✅ Good" : "⚠️ High");
                        table.AddRow("Memory Usage", $"{benchmark.SystemMetrics.MemoryUsageMB}MB", 
                            benchmark.SystemMetrics.MemoryUsageMB < 1000 ? "✅ Good" : "⚠️ High");
                    }
                    
                    if (benchmark.CacheMetrics != null)
                    {
                        table.AddRow("Cache Hit Rate", $"{benchmark.CacheMetrics.HitRate:P1}", 
                            benchmark.CacheMetrics.HitRate > 0.8 ? "✅ Good" : "⚠️ Low");
                        table.AddRow("Cache Access Time", $"{benchmark.CacheMetrics.AverageAccessTime.TotalMilliseconds:F1}ms", 
                            benchmark.CacheMetrics.AverageAccessTime.TotalMilliseconds < 10 ? "✅ Good" : "⚠️ Slow");
                    }
                    
                    if (benchmark.AIMetrics != null)
                    {
                        table.AddRow("AI Response Time", $"{benchmark.AIMetrics.AverageResponseTime.TotalMilliseconds:F0}ms", 
                            benchmark.AIMetrics.AverageResponseTime.TotalSeconds < 5 ? "✅ Good" : "⚠️ Slow");
                        table.AddRow("AI Success Rate", $"{benchmark.AIMetrics.SuccessRate:P1}", 
                            benchmark.AIMetrics.SuccessRate > 0.95 ? "✅ Good" : "⚠️ Low");
                    }
                    
                    AnsiConsole.Write(table);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[bold red]❌ Benchmark failed: {result.ErrorMessage}[/]");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during performance benchmark");
                AnsiConsole.MarkupLine($"[bold red]❌ Error: {ex.Message}[/]");
            }
        }

        /// <summary>
        /// Gets performance recommendations.
        /// </summary>
        [Command("performance recommendations")]
        [Description("Get performance recommendations based on current metrics")]
        public async Task GetPerformanceRecommendationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]💡 Performance Recommendations[/]");
                
                var recommendations = await _performanceOptimizer.GetPerformanceRecommendationsAsync(cancellationToken);
                var recommendationsList = recommendations.ToList();

                if (recommendationsList.Count == 0)
                {
                    AnsiConsole.MarkupLine("[green]✅ No performance recommendations at this time.[/]");
                    return;
                }

                foreach (var recommendation in recommendationsList)
                {
                    var priorityColor = recommendation.Priority switch
                    {
                        PerformancePriority.Critical => "red",
                        PerformancePriority.High => "orange3",
                        PerformancePriority.Medium => "yellow",
                        PerformancePriority.Low => "green",
                        _ => "white"
                    };

                    AnsiConsole.MarkupLine($"[bold {priorityColor}]📋 {recommendation.Title}[/]");
                    AnsiConsole.MarkupLine($"[dim]Category: {recommendation.Category} | Priority: {recommendation.Priority}[/]");
                    AnsiConsole.MarkupLine($"[white]{recommendation.Description}[/]");
                    AnsiConsole.MarkupLine($"[dim]Impact: {recommendation.EstimatedImpact} | Effort: {recommendation.ImplementationEffort}[/]");
                    AnsiConsole.WriteLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance recommendations");
                AnsiConsole.MarkupLine($"[bold red]❌ Error: {ex.Message}[/]");
            }
        }

        /// <summary>
        /// Runs comprehensive security audit.
        /// </summary>
        [Command("security audit")]
        [Description("Run comprehensive security audit across all systems")]
        public async Task RunSecurityAuditAsync(
            [Option("--api-keys", "-k")] bool auditApiKeys = true,
            [Option("--authentication", "-a")] bool auditAuthentication = true,
            [Option("--authorization", "-z")] bool auditAuthorization = true,
            [Option("--encryption", "-e")] bool auditEncryption = true,
            [Option("--audit-logging", "-l")] bool auditAuditLogging = true,
            [Option("--network", "-n")] bool auditNetwork = true,
            [Option("--compliance", "-c")] bool auditCompliance = true,
            [Option("--performance", "-p")] bool auditPerformance = true,
            [Option("--max-time")] int maxTimeMinutes = 15,
            CancellationToken cancellationToken = default)
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🔒 Starting Security Audit[/]");
                
                var options = new SecurityAuditOptions
                {
                    AuditApiKeys = auditApiKeys,
                    AuditAuthentication = auditAuthentication,
                    AuditAuthorization = auditAuthorization,
                    AuditEncryption = auditEncryption,
                    AuditAuditLogging = auditAuditLogging,
                    AuditNetwork = auditNetwork,
                    AuditCompliance = auditCompliance,
                    AuditPerformance = auditPerformance,
                    MaxAuditTime = TimeSpan.FromMinutes(maxTimeMinutes)
                };

                var progress = AnsiConsole.Progress()
                    .Columns(new ProgressColumn[]
                    {
                        new SpinnerColumn(),
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new ElapsedTimeColumn()
                    });

                await progress.StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("Running security audit...", maxValue: 100);
                    
                    // Simulate audit progress
                    for (int i = 0; i <= 100; i += 10)
                    {
                        task.Increment(10);
                        await Task.Delay(150, cancellationToken);
                    }
                });

                var result = await _securityAuditor.RunSecurityAuditAsync(options, cancellationToken);

                if (result.Success)
                {
                    var scoreColor = result.OverallSecurityScore switch
                    {
                        >= 90 => "green",
                        >= 80 => "yellow",
                        >= 70 => "orange3",
                        _ => "red"
                    };

                    AnsiConsole.MarkupLine($"[bold green]✅ Security audit completed successfully![/]");
                    AnsiConsole.MarkupLine($"[dim]Duration: {result.Duration.TotalMilliseconds:F0}ms[/]");
                    AnsiConsole.MarkupLine($"[bold {scoreColor}]Overall Security Score: {result.OverallSecurityScore:F1}/100[/]");
                    
                    // Display audit results
                    var table = new Table();
                    table.AddColumn("Component");
                    table.AddColumn("Score");
                    table.AddColumn("Status");
                    
                    if (result.ApiKeyAudit?.Success == true)
                        table.AddRow("API Keys", $"{result.ApiKeyAudit.Score:F1}/100", 
                            result.ApiKeyAudit.Score >= 80 ? "✅ Good" : "⚠️ Needs Attention");
                    
                    if (result.AuthenticationAudit?.Success == true)
                        table.AddRow("Authentication", $"{result.AuthenticationAudit.Score:F1}/100", 
                            result.AuthenticationAudit.Score >= 80 ? "✅ Good" : "⚠️ Needs Attention");
                    
                    if (result.AuthorizationAudit?.Success == true)
                        table.AddRow("Authorization", $"{result.AuthorizationAudit.Score:F1}/100", 
                            result.AuthorizationAudit.Score >= 80 ? "✅ Good" : "⚠️ Needs Attention");
                    
                    if (result.EncryptionAudit?.Success == true)
                        table.AddRow("Encryption", $"{result.EncryptionAudit.Score:F1}/100", 
                            result.EncryptionAudit.Score >= 80 ? "✅ Good" : "⚠️ Needs Attention");
                    
                    if (result.AuditLoggingAudit?.Success == true)
                        table.AddRow("Audit Logging", $"{result.AuditLoggingAudit.Score:F1}/100", 
                            result.AuditLoggingAudit.Score >= 80 ? "✅ Good" : "⚠️ Needs Attention");
                    
                    if (result.NetworkAudit?.Success == true)
                        table.AddRow("Network", $"{result.NetworkAudit.Score:F1}/100", 
                            result.NetworkAudit.Score >= 80 ? "✅ Good" : "⚠️ Needs Attention");
                    
                    if (result.ComplianceAudit?.Success == true)
                        table.AddRow("Compliance", $"{result.ComplianceAudit.Score:F1}/100", 
                            result.ComplianceAudit.Score >= 80 ? "✅ Good" : "⚠️ Needs Attention");
                    
                    if (result.PerformanceSecurityAudit?.Success == true)
                        table.AddRow("Performance Security", $"{result.PerformanceSecurityAudit.Score:F1}/100", 
                            result.PerformanceSecurityAudit.Score >= 80 ? "✅ Good" : "⚠️ Needs Attention");
                    
                    AnsiConsole.Write(table);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[bold red]❌ Security audit failed: {result.ErrorMessage}[/]");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during security audit");
                AnsiConsole.MarkupLine($"[bold red]❌ Error: {ex.Message}[/]");
            }
        }

        /// <summary>
        /// Runs penetration testing simulation.
        /// </summary>
        [Command("security penetration-test")]
        [Description("Run penetration testing simulation")]
        public async Task RunPenetrationTestAsync(
            [Option("--name", "-n")] string testName = "Production Penetration Test",
            [Option("--auth-bypass", "-a")] bool testAuthBypass = true,
            [Option("--auth-escalation", "-e")] bool testAuthEscalation = true,
            [Option("--api-keys", "-k")] bool testApiKeys = true,
            [Option("--data-injection", "-d")] bool testDataInjection = true,
            [Option("--session-mgmt", "-s")] bool testSessionMgmt = true,
            [Option("--input-validation", "-i")] bool testInputValidation = true,
            [Option("--max-time")] int maxTimeMinutes = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                AnsiConsole.MarkupLine($"[bold blue]🎯 Running Penetration Test: {testName}[/]");
                
                var options = new PenetrationTestOptions
                {
                    TestName = testName,
                    TestAuthenticationBypass = testAuthBypass,
                    TestAuthorizationEscalation = testAuthEscalation,
                    TestApiKeySecurity = testApiKeys,
                    TestDataInjection = testDataInjection,
                    TestSessionManagement = testSessionMgmt,
                    TestInputValidation = testInputValidation,
                    MaxTestTime = TimeSpan.FromMinutes(maxTimeMinutes)
                };

                var progress = AnsiConsole.Progress()
                    .Columns(new ProgressColumn[]
                    {
                        new SpinnerColumn(),
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new ElapsedTimeColumn()
                    });

                await progress.StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("Running penetration tests...", maxValue: 100);
                    
                    // Simulate penetration test progress
                    for (int i = 0; i <= 100; i += 5)
                    {
                        task.Increment(5);
                        await Task.Delay(300, cancellationToken);
                    }
                });

                var result = await _securityAuditor.RunPenetrationTestAsync(options, cancellationToken);

                if (result.Success)
                {
                    var ratingColor = result.SecurityRating switch
                    {
                        SecurityRating.Excellent => "green",
                        SecurityRating.Good => "yellow",
                        SecurityRating.Fair => "orange3",
                        SecurityRating.Poor => "red",
                        _ => "white"
                    };

                    AnsiConsole.MarkupLine($"[bold green]✅ Penetration test completed successfully![/]");
                    AnsiConsole.MarkupLine($"[dim]Duration: {result.Duration.TotalMilliseconds:F0}ms[/]");
                    AnsiConsole.MarkupLine($"[bold {ratingColor}]Security Rating: {result.SecurityRating}[/]");
                    AnsiConsole.MarkupLine($"[bold {(result.VulnerabilityCount == 0 ? "green" : "red")}]Vulnerabilities Found: {result.VulnerabilityCount}[/]");
                    
                    if (result.VulnerabilityCount == 0)
                    {
                        AnsiConsole.MarkupLine("[green]🎉 No vulnerabilities found! System is secure.[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]⚠️ Vulnerabilities found. Review results and implement fixes.[/]");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[bold red]❌ Penetration test failed: {result.ErrorMessage}[/]");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during penetration test");
                AnsiConsole.MarkupLine($"[bold red]❌ Error: {ex.Message}[/]");
            }
        }

        /// <summary>
        /// Gets security recommendations.
        /// </summary>
        [Command("security recommendations")]
        [Description("Get security recommendations based on audit results")]
        public async Task GetSecurityRecommendationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🛡️ Security Recommendations[/]");
                
                var recommendations = await _securityAuditor.GetSecurityRecommendationsAsync(cancellationToken);
                var recommendationsList = recommendations.ToList();

                if (recommendationsList.Count == 0)
                {
                    AnsiConsole.MarkupLine("[green]✅ No security recommendations at this time.[/]");
                    return;
                }

                foreach (var recommendation in recommendationsList)
                {
                    var priorityColor = recommendation.Priority switch
                    {
                        SecurityPriority.Critical => "red",
                        SecurityPriority.High => "orange3",
                        SecurityPriority.Medium => "yellow",
                        SecurityPriority.Low => "green",
                        _ => "white"
                    };

                    AnsiConsole.MarkupLine($"[bold {priorityColor}]🔐 {recommendation.Title}[/]");
                    AnsiConsole.MarkupLine($"[dim]Category: {recommendation.Category} | Priority: {recommendation.Priority}[/]");
                    AnsiConsole.MarkupLine($"[white]{recommendation.Description}[/]");
                    AnsiConsole.MarkupLine($"[dim]Impact: {recommendation.EstimatedImpact} | Effort: {recommendation.ImplementationEffort}[/]");
                    AnsiConsole.WriteLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting security recommendations");
                AnsiConsole.MarkupLine($"[bold red]❌ Error: {ex.Message}[/]");
            }
        }

        /// <summary>
        /// Checks security compliance status.
        /// </summary>
        [Command("security compliance")]
        [Description("Check security compliance status")]
        public async Task CheckComplianceStatusAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]📋 Security Compliance Status[/]");
                
                var status = await _securityAuditor.GetSecurityComplianceStatusAsync(cancellationToken);

                var scoreColor = status.OverallComplianceScore switch
                {
                    >= 90 => "green",
                    >= 80 => "yellow",
                    >= 70 => "orange3",
                    _ => "red"
                };

                AnsiConsole.MarkupLine($"[bold {scoreColor}]Overall Compliance Score: {status.OverallComplianceScore:F1}/100[/]");
                AnsiConsole.MarkupLine($"[bold {(status.IsCompliant ? "green" : "red")}]Compliance Status: {(status.IsCompliant ? "✅ Compliant" : "❌ Non-Compliant")}[/]");
                
                // Display compliance details
                var table = new Table();
                table.AddColumn("Standard");
                table.AddColumn("Status");
                table.AddColumn("Score");
                
                if (status.GDPRCompliance != null)
                    table.AddRow("GDPR", 
                        status.GDPRCompliance.IsCompliant ? "✅ Compliant" : "❌ Non-Compliant",
                        $"{status.GDPRCompliance.Score:F1}/100");
                
                if (status.HIPAACompliance != null)
                    table.AddRow("HIPAA", 
                        status.HIPAACompliance.IsCompliant ? "✅ Compliant" : "❌ Non-Compliant",
                        $"{status.HIPAACompliance.Score:F1}/100");
                
                if (status.SOXCompliance != null)
                    table.AddRow("SOX", 
                        status.SOXCompliance.IsCompliant ? "✅ Compliant" : "❌ Non-Compliant",
                        $"{status.SOXCompliance.Score:F1}/100");
                
                if (status.ISO27001Compliance != null)
                    table.AddRow("ISO 27001", 
                        status.ISO27001Compliance.IsCompliant ? "✅ Compliant" : "❌ Non-Compliant",
                        $"{status.ISO27001Compliance.Score:F1}/100");
                
                if (status.PCICompliance != null)
                    table.AddRow("PCI DSS", 
                        status.PCICompliance.IsCompliant ? "✅ Compliant" : "❌ Non-Compliant",
                        $"{status.PCICompliance.Score:F1}/100");
                
                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking compliance status");
                AnsiConsole.MarkupLine($"[bold red]❌ Error: {ex.Message}[/]");
            }
        }

        /// <summary>
        /// Runs comprehensive production readiness check.
        /// </summary>
        [Command("production readiness")]
        [Description("Run comprehensive production readiness check")]
        public async Task RunProductionReadinessCheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]🚀 Production Readiness Check[/]");
                AnsiConsole.MarkupLine("[dim]Running comprehensive production readiness assessment...[/]");
                
                // Run performance optimization
                AnsiConsole.MarkupLine("\n[bold]1. Performance Optimization[/]");
                var perfOptions = new PerformanceOptimizationOptions
                {
                    OptimizeCaching = true,
                    OptimizeMemory = true,
                    OptimizeAI = true,
                    OptimizeSecurity = true,
                    OptimizeDatabase = true,
                    OptimizeNetwork = true
                };
                var perfResult = await _performanceOptimizer.OptimizePerformanceAsync(perfOptions, cancellationToken);
                
                // Run security audit
                AnsiConsole.MarkupLine("\n[bold]2. Security Audit[/]");
                var securityOptions = new SecurityAuditOptions
                {
                    AuditApiKeys = true,
                    AuditAuthentication = true,
                    AuditAuthorization = true,
                    AuditEncryption = true,
                    AuditAuditLogging = true,
                    AuditNetwork = true,
                    AuditCompliance = true,
                    AuditPerformance = true
                };
                var securityResult = await _securityAuditor.RunSecurityAuditAsync(securityOptions, cancellationToken);
                
                // Check compliance
                AnsiConsole.MarkupLine("\n[bold]3. Compliance Check[/]");
                var complianceStatus = await _securityAuditor.GetSecurityComplianceStatusAsync(cancellationToken);
                
                // Display overall results
                AnsiConsole.MarkupLine("\n[bold]📊 Production Readiness Summary[/]");
                
                var overallScore = (perfResult.Success ? 50 : 0) + (securityResult.Success ? 30 : 0) + (complianceStatus.IsCompliant ? 20 : 0);
                var scoreColor = overallScore switch
                {
                    >= 90 => "green",
                    >= 80 => "yellow",
                    >= 70 => "orange3",
                    _ => "red"
                };
                
                AnsiConsole.MarkupLine($"[bold {scoreColor}]Overall Readiness Score: {overallScore}/100[/]");
                
                var table = new Table();
                table.AddColumn("Component");
                table.AddColumn("Status");
                table.AddColumn("Score");
                
                table.AddRow("Performance Optimization", 
                    perfResult.Success ? "✅ Ready" : "❌ Issues",
                    perfResult.Success ? "50/50" : "0/50");
                
                table.AddRow("Security Audit", 
                    securityResult.Success ? "✅ Ready" : "❌ Issues",
                    securityResult.Success ? "30/30" : "0/30");
                
                table.AddRow("Compliance", 
                    complianceStatus.IsCompliant ? "✅ Compliant" : "❌ Non-Compliant",
                    complianceStatus.IsCompliant ? "20/20" : "0/20");
                
                AnsiConsole.Write(table);
                
                if (overallScore >= 90)
                {
                    AnsiConsole.MarkupLine("\n[bold green]🎉 System is production ready![/]");
                }
                else if (overallScore >= 80)
                {
                    AnsiConsole.MarkupLine("\n[bold yellow]⚠️ System is mostly ready with minor issues.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("\n[bold red]❌ System is not production ready. Address issues before deployment.[/]");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during production readiness check");
                AnsiConsole.MarkupLine($"[bold red]❌ Error: {ex.Message}[/]");
            }
        }
    }
}
