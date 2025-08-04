using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Web.Interfaces;
using Nexo.Feature.Web.Models;
using Nexo.Feature.Web.Enums;
using System.Text;

namespace Nexo.Feature.Web.Services
{
    /// <summary>
    /// Service for generating web code with React/Vue support and WebAssembly optimization.
    /// </summary>
    public class WebCodeGenerator : IWebCodeGenerator
    {
        private readonly ILogger<WebCodeGenerator> _logger;
        private readonly IFrameworkTemplateProvider _templateProvider;
        private readonly IWebAssemblyOptimizer _wasmOptimizer;

        public WebCodeGenerator(
            ILogger<WebCodeGenerator> logger,
            IFrameworkTemplateProvider templateProvider,
            IWebAssemblyOptimizer wasmOptimizer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _templateProvider = templateProvider ?? throw new ArgumentNullException(nameof(templateProvider));
            _wasmOptimizer = wasmOptimizer ?? throw new ArgumentNullException(nameof(wasmOptimizer));
        }

        public async Task<WebCodeGenerationResult> GenerateCodeAsync(WebCodeGenerationRequest request)
        {
            _logger.LogInformation("Starting web code generation for {Framework} {ComponentType}", 
                request.Framework, request.ComponentType);

            try
            {
                // Validate request
                if (!ValidateRequest(request))
                {
                    return new WebCodeGenerationResult
                    {
                        Success = false,
                        Message = "Invalid request parameters",
                        Framework = request.Framework,
                        ComponentType = request.ComponentType,
                        Optimization = request.Optimization
                    };
                }

                var result = new WebCodeGenerationResult
                {
                    Framework = request.Framework,
                    ComponentType = request.ComponentType,
                    Optimization = request.Optimization
                };

                // Generate component code
                result.ComponentCode = await GenerateComponentCodeAsync(request);

                // Generate TypeScript types if requested
                if (request.IncludeTypeScript)
                {
                    result.TypeScriptTypes = await GenerateTypeScriptTypesAsync(request);
                }

                // Generate styling if requested
                if (request.IncludeStyling)
                {
                    result.StylingCode = await GenerateStylingCodeAsync(request);
                }

                // Generate tests if requested
                if (request.IncludeTests)
                {
                    result.TestCode = await GenerateTestCodeAsync(request);
                }

                // Generate documentation if requested
                if (request.IncludeDocumentation)
                {
                    result.Documentation = await GenerateDocumentationAsync(request);
                }

                // Apply WebAssembly optimization
                if (request.Optimization != WebAssemblyOptimization.None)
                {
                    var wasmConfig = CreateWebAssemblyConfig(request);
                    var wasmResult = await _wasmOptimizer.OptimizeAsync(result.ComponentCode, wasmConfig);
                    
                    if (wasmResult.Success)
                    {
                        result.ComponentCode = wasmResult.OptimizedCode;
                        result.WebAssemblyMetrics = wasmResult.Metrics;
                        result.Warnings.AddRange(wasmResult.Warnings);
                    }
                }

                // Analyze performance and bundle size
                var performanceAnalysis = await _wasmOptimizer.AnalyzePerformanceAsync(result.ComponentCode);
                result.PerformanceMetrics = performanceAnalysis.PerformanceMetrics;

                var bundleAnalysis = await _wasmOptimizer.EstimateBundleSizeAsync(result.ComponentCode, 
                    CreateWebAssemblyConfig(request));
                result.BundleSizes = bundleAnalysis.BundleSizes;

                // Generate file paths
                result.GeneratedFiles = GenerateFilePaths(request);

                result.Success = true;
                result.Message = "Code generation completed successfully";

                _logger.LogInformation("Web code generation completed successfully for {ComponentName}", 
                    request.ComponentName);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during web code generation for {ComponentName}", request.ComponentName);
                
                return new WebCodeGenerationResult
                {
                    Success = false,
                    Message = $"Code generation failed: {ex.Message}",
                    Framework = request.Framework,
                    ComponentType = request.ComponentType,
                    Optimization = request.Optimization
                };
            }
        }

        public bool ValidateRequest(WebCodeGenerationRequest request)
        {
            if (request == null)
                return false;

            if (string.IsNullOrWhiteSpace(request.ComponentName))
                return false;

            if (string.IsNullOrWhiteSpace(request.TargetPath))
                return false;

            if (!Enum.IsDefined(typeof(WebFrameworkType), request.Framework))
                return false;

            if (!Enum.IsDefined(typeof(WebComponentType), request.ComponentType))
                return false;

            if (!Enum.IsDefined(typeof(WebAssemblyOptimization), request.Optimization))
                return false;

            return true;
        }

        public IEnumerable<string> GetSupportedFrameworks()
        {
            return Enum.GetNames(typeof(WebFrameworkType));
        }

        public IEnumerable<string> GetSupportedComponentTypes(string framework)
        {
            if (Enum.TryParse<WebFrameworkType>(framework, out var frameworkType))
            {
                return Enum.GetNames(typeof(WebComponentType));
            }
            return Enumerable.Empty<string>();
        }

        private async Task<string> GenerateComponentCodeAsync(WebCodeGenerationRequest request)
        {
            var template = _templateProvider.GetTemplate(request.Framework, request.ComponentType);
            
            // Replace placeholders in template
            var code = template
                .Replace("{{ComponentName}}", request.ComponentName)
                .Replace("{{SourceCode}}", request.SourceCode)
                .Replace("{{Framework}}", request.Framework.ToString());

            // Add framework-specific optimizations
            code = await ApplyFrameworkOptimizationsAsync(code, request);

            return code;
        }

        private async Task<string> GenerateTypeScriptTypesAsync(WebCodeGenerationRequest request)
        {
            var template = _templateProvider.GetTypeScriptTemplate(request.Framework, request.ComponentType);
            
            return template
                .Replace("{{ComponentName}}", request.ComponentName)
                .Replace("{{Framework}}", request.Framework.ToString());
        }

        private async Task<string> GenerateStylingCodeAsync(WebCodeGenerationRequest request)
        {
            var template = _templateProvider.GetStylingTemplate(request.Framework, request.ComponentType);
            
            return template
                .Replace("{{ComponentName}}", request.ComponentName)
                .Replace("{{Framework}}", request.Framework.ToString());
        }

        private async Task<string> GenerateTestCodeAsync(WebCodeGenerationRequest request)
        {
            var template = _templateProvider.GetTestTemplate(request.Framework, request.ComponentType);
            
            return template
                .Replace("{{ComponentName}}", request.ComponentName)
                .Replace("{{Framework}}", request.Framework.ToString());
        }

        private async Task<string> GenerateDocumentationAsync(WebCodeGenerationRequest request)
        {
            var template = _templateProvider.GetDocumentationTemplate(request.Framework, request.ComponentType);
            
            return template
                .Replace("{{ComponentName}}", request.ComponentName)
                .Replace("{{Framework}}", request.Framework.ToString());
        }

        private async Task<string> ApplyFrameworkOptimizationsAsync(string code, WebCodeGenerationRequest request)
        {
            switch (request.Framework)
            {
                case WebFrameworkType.React:
                    return ApplyReactOptimizations(code, request);
                case WebFrameworkType.Vue:
                    return ApplyVueOptimizations(code, request);
                case WebFrameworkType.NextJs:
                    return ApplyNextJsOptimizations(code, request);
                case WebFrameworkType.NuxtJs:
                    return ApplyNuxtJsOptimizations(code, request);
                default:
                    return code;
            }
        }

        private string ApplyReactOptimizations(string code, WebCodeGenerationRequest request)
        {
            var optimizedCode = new StringBuilder(code);

            // Add React.memo for pure components
            if (request.ComponentType == WebComponentType.Pure)
            {
                optimizedCode.Replace("export default function", "const Component = React.memo(function");
                optimizedCode.AppendLine("\nexport default Component;");
            }

            // Add useCallback and useMemo optimizations
            if (request.Optimization == WebAssemblyOptimization.Aggressive)
            {
                optimizedCode.Replace("function handleClick", "const handleClick = useCallback(function");
                optimizedCode.Replace("const expensiveValue", "const expensiveValue = useMemo(() =>");
            }

            return optimizedCode.ToString();
        }

        private string ApplyVueOptimizations(string code, WebCodeGenerationRequest request)
        {
            var optimizedCode = new StringBuilder(code);

            // Add defineAsyncComponent for lazy loading
            if (request.ComponentType == WebComponentType.Page)
            {
                optimizedCode.Replace("export default {", "export default defineAsyncComponent(() => ({");
                optimizedCode.AppendLine("}));");
            }

            // Add computed properties for performance
            if (request.Optimization == WebAssemblyOptimization.Aggressive)
            {
                optimizedCode.Replace("data() {", "data() {\n    // Optimized with computed properties");
            }

            return optimizedCode.ToString();
        }

        private string ApplyNextJsOptimizations(string code, WebCodeGenerationRequest request)
        {
            var optimizedCode = new StringBuilder(code);

            // Add Next.js specific optimizations
            if (request.ComponentType == WebComponentType.Page)
            {
                optimizedCode.Replace("export default function", "export default function Page");
                optimizedCode.AppendLine("\nPage.getInitialProps = async (ctx) => {");
                optimizedCode.AppendLine("  return { props: {} };");
                optimizedCode.AppendLine("};");
            }

            return optimizedCode.ToString();
        }

        private string ApplyNuxtJsOptimizations(string code, WebCodeGenerationRequest request)
        {
            var optimizedCode = new StringBuilder(code);

            // Add Nuxt.js specific optimizations
            if (request.ComponentType == WebComponentType.Page)
            {
                optimizedCode.Replace("export default {", "export default definePageMeta({");
                optimizedCode.AppendLine("});");
            }

            return optimizedCode.ToString();
        }

        private WebAssemblyConfig CreateWebAssemblyConfig(WebCodeGenerationRequest request)
        {
            return new WebAssemblyConfig
            {
                Optimization = request.Optimization,
                EnableTreeShaking = request.Optimization != WebAssemblyOptimization.None,
                EnableCodeSplitting = request.Optimization == WebAssemblyOptimization.Balanced || 
                                    request.Optimization == WebAssemblyOptimization.Aggressive,
                EnableMinification = request.Optimization != WebAssemblyOptimization.None,
                EnableSourceMaps = request.Optimization == WebAssemblyOptimization.None,
                TargetBrowsers = new List<string> { "chrome >= 80", "firefox >= 78", "safari >= 13" },
                CustomFlags = request.WebAssemblySettings,
                Memory = new WebAssemblyMemoryConfig
                {
                    InitialPages = 256,
                    MaxPages = 2048,
                    EnableSharedMemory = request.Optimization == WebAssemblyOptimization.Aggressive
                },
                Threading = new WebAssemblyThreadingConfig
                {
                    EnableThreading = request.Optimization == WebAssemblyOptimization.Aggressive,
                    MaxThreads = 4,
                    UseWebWorkers = true
                },
                Simd = new WebAssemblySimdConfig
                {
                    EnableSimd = request.Optimization != WebAssemblyOptimization.None,
                    InstructionSet = "wasm_simd128"
                }
            };
        }

        private List<string> GenerateFilePaths(WebCodeGenerationRequest request)
        {
            var files = new List<string>();
            var basePath = request.TargetPath;

            files.Add($"{basePath}/{request.ComponentName}.{GetFileExtension(request.Framework)}");

            if (request.IncludeTypeScript)
            {
                files.Add($"{basePath}/{request.ComponentName}.d.ts");
            }

            if (request.IncludeStyling)
            {
                files.Add($"{basePath}/{request.ComponentName}.{GetStylingExtension(request.Framework)}");
            }

            if (request.IncludeTests)
            {
                files.Add($"{basePath}/{request.ComponentName}.test.{GetFileExtension(request.Framework)}");
            }

            if (request.IncludeDocumentation)
            {
                files.Add($"{basePath}/{request.ComponentName}.md");
            }

            return files;
        }

        private string GetFileExtension(WebFrameworkType framework)
        {
            switch (framework)
            {
                case WebFrameworkType.React:
                    return "tsx";
                case WebFrameworkType.Vue:
                    return "vue";
                case WebFrameworkType.Angular:
                    return "ts";
                case WebFrameworkType.Svelte:
                    return "svelte";
                case WebFrameworkType.NextJs:
                    return "tsx";
                case WebFrameworkType.NuxtJs:
                    return "vue";
                default:
                    return "tsx";
            }
        }

        private string GetStylingExtension(WebFrameworkType framework)
        {
            switch (framework)
            {
                case WebFrameworkType.React:
                    return "css";
                case WebFrameworkType.Vue:
                    return "scss";
                case WebFrameworkType.Angular:
                    return "scss";
                case WebFrameworkType.Svelte:
                    return "css";
                case WebFrameworkType.NextJs:
                    return "css";
                case WebFrameworkType.NuxtJs:
                    return "scss";
                default:
                    return "css";
            }
        }
    }
} 