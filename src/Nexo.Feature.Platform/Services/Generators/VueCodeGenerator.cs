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
/// Generates Vue.js code for web applications
/// </summary>
public partial class VueCodeGenerator
{
    private readonly ILogger<VueCodeGenerator> _logger;

    public VueCodeGenerator(ILogger<VueCodeGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WebCodeGenerationResult> GenerateVueCodeAsync(
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
        _logger.LogInformation("Starting Vue.js code generation");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = new WebCodeGenerationResult
            {
                GeneratedCode = new WebGeneratedCode()
            };

            // Generate Vue components
            var vueFiles = await GenerateVueFilesAsync(applicationLogic, webOptions, cancellationToken);
            result.GeneratedCode.VueFiles = vueFiles;

            // Generate TypeScript files
            var typescriptFiles = await GenerateVueTypeScriptFilesAsync(applicationLogic, webOptions, cancellationToken);
            result.GeneratedCode.TypeScriptFiles = typescriptFiles;

            // Generate package.json
            result.GeneratedCode.PackageJson = GeneratePackageJson(webOptions);

            // Generate Vite config
            result.GeneratedCode.ViteConfig = GenerateViteConfig(webOptions);

            // Generate ESLint config
            result.GeneratedCode.ESLintConfig = GenerateESLintConfig(webOptions);

            stopwatch.Stop();
            result.Success = true;
            result.Message = $"Vue.js code generation completed in {stopwatch.ElapsedMilliseconds}ms";
            result.GenerationTime = stopwatch.Elapsed;

            _logger.LogInformation("Vue.js code generation completed successfully");
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Vue.js code generation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Vue.js code generation");
            return new WebCodeGenerationResult
            {
                Success = false,
                Message = $"Vue.js code generation failed: {ex.Message}",
                GenerationTime = stopwatch.Elapsed
            };
        }
    }

    private async Task<List<JavaScriptFile>> GenerateVueFilesAsync(
        StandardizedApplicationLogic applicationLogic,
        WebGenerationOptions webOptions,
        CancellationToken cancellationToken)
    {
        var files = new List<JavaScriptFile>();

        try
        {
            _logger.LogDebug("Generating Vue files for {ComponentCount} components", 
                applicationLogic.Components.Count);

            foreach (var component in applicationLogic.Components)
            {
                var vueFile = new JavaScriptFile
                {
                    FileName = $"{component.Name}.vue",
                    Content = GenerateVueComponent(component, webOptions),
                    FileType = JavaScriptFileType.VueComponent,
                    GeneratedAt = DateTime.UtcNow
                };

                files.Add(vueFile);
            }

            // Generate main App component
            var appComponent = new JavaScriptFile
            {
                FileName = "App.vue",
                Content = GenerateAppComponent(applicationLogic, webOptions),
                FileType = JavaScriptFileType.VueComponent,
                GeneratedAt = DateTime.UtcNow
            };
            files.Add(appComponent);

            // Generate main.js
            var mainFile = new JavaScriptFile
            {
                FileName = "main.js",
                Content = GenerateMainFile(webOptions),
                FileType = JavaScriptFileType.EntryPoint,
                GeneratedAt = DateTime.UtcNow
            };
            files.Add(mainFile);

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Vue files");
            return files;
        }
    }

    private async Task<List<TypeScriptFile>> GenerateVueTypeScriptFilesAsync(
        StandardizedApplicationLogic applicationLogic,
        WebGenerationOptions webOptions,
        CancellationToken cancellationToken)
    {
        var files = new List<TypeScriptFile>();

        try
        {
            _logger.LogDebug("Generating TypeScript files for Vue application");

            // Generate type definitions
            foreach (var model in applicationLogic.Models)
            {
                var typeFile = new TypeScriptFile
                {
                    FileName = $"{model.Name}.types.ts",
                    Content = GenerateTypeDefinitions(model),
                    FileType = TypeScriptFileType.TypeDefinition,
                    GeneratedAt = DateTime.UtcNow
                };
                files.Add(typeFile);
            }

            // Generate composables
            var composableFile = new TypeScriptFile
            {
                FileName = "composables.ts",
                Content = GenerateComposables(applicationLogic),
                FileType = TypeScriptFileType.Composable,
                GeneratedAt = DateTime.UtcNow
            };
            files.Add(composableFile);

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating TypeScript files");
            return files;
        }
    }

    private string GenerateVueComponent(ApplicationComponent component, WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<template>");
        sb.AppendLine("  <div class=\"component-container\">");
        sb.AppendLine($"    <h2>{component.Name}</h2>");
        sb.AppendLine("    <!-- Component content -->");
        sb.AppendLine("  </div>");
        sb.AppendLine("</template>");
        sb.AppendLine();
        
        sb.AppendLine("<script setup lang=\"ts\">");
        sb.AppendLine("import { ref, computed } from 'vue';");
        sb.AppendLine();
        
        foreach (var prop in component.Properties)
        {
            sb.AppendLine($"const {prop.Name} = ref<{GetTypeScriptType(prop.Type)}>();");
        }
        
        sb.AppendLine("</script>");
        sb.AppendLine();
        
        sb.AppendLine("<style scoped>");
        sb.AppendLine(".component-container {");
        sb.AppendLine("  padding: 1rem;");
        sb.AppendLine("}");
        sb.AppendLine("</style>");
        
        return sb.ToString();
    }

    private string GenerateAppComponent(StandardizedApplicationLogic applicationLogic, WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<template>");
        sb.AppendLine("  <div id=\"app\">");
        sb.AppendLine("    <nav>");
        sb.AppendLine("      <router-link to=\"/\">Home</router-link>");
        foreach (var component in applicationLogic.Components)
        {
            sb.AppendLine($"      <router-link to=\"/{component.Name.ToLower()}\">{component.Name}</router-link>");
        }
        sb.AppendLine("    </nav>");
        sb.AppendLine("    <router-view />");
        sb.AppendLine("  </div>");
        sb.AppendLine("</template>");
        sb.AppendLine();
        
        sb.AppendLine("<script setup lang=\"ts\">");
        sb.AppendLine("import { onMounted } from 'vue';");
        sb.AppendLine();
        sb.AppendLine("onMounted(() => {");
        sb.AppendLine("  console.log('App mounted');");
        sb.AppendLine("});");
        sb.AppendLine("</script>");
        sb.AppendLine();
        
        sb.AppendLine("<style>");
        sb.AppendLine("#app {");
        sb.AppendLine("  font-family: Avenir, Helvetica, Arial, sans-serif;");
        sb.AppendLine("}");
        sb.AppendLine("</style>");
        
        return sb.ToString();
    }

    private string GenerateMainFile(WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("import { createApp } from 'vue';");
        sb.AppendLine("import { createRouter, createWebHistory } from 'vue-router';");
        sb.AppendLine("import App from './App.vue';");
        sb.AppendLine("import './style.css';");
        sb.AppendLine();
        sb.AppendLine("const router = createRouter({");
        sb.AppendLine("  history: createWebHistory(),");
        sb.AppendLine("  routes: [");
        sb.AppendLine("    { path: '/', component: () => import('./components/Home.vue') }");
        sb.AppendLine("  ]");
        sb.AppendLine("});");
        sb.AppendLine();
        sb.AppendLine("const app = createApp(App);");
        sb.AppendLine("app.use(router);");
        sb.AppendLine("app.mount('#app');");
        
        return sb.ToString();
    }

    private string GenerateTypeDefinitions(ApplicationModel model)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"export interface {model.Name} {{");
        foreach (var property in model.Properties)
        {
            sb.AppendLine($"  {property.Name}: {GetTypeScriptType(property.Type)};");
        }
        sb.AppendLine("}");
        sb.AppendLine();
        
        sb.AppendLine($"export type {model.Name}Create = Omit<{model.Name}, 'id'>;");
        sb.AppendLine($"export type {model.Name}Update = Partial<{model.Name}>;");
        
        return sb.ToString();
    }

    private string GenerateComposables(StandardizedApplicationLogic applicationLogic)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("import { ref, computed } from 'vue';");
        sb.AppendLine();
        
        sb.AppendLine("export function useApi() {");
        sb.AppendLine("  const loading = ref(false);");
        sb.AppendLine("  const error = ref<string | null>(null);");
        sb.AppendLine();
        sb.AppendLine("  const execute = async <T>(apiCall: () => Promise<T>): Promise<T | null> => {");
        sb.AppendLine("    try {");
        sb.AppendLine("      loading.value = true;");
        sb.AppendLine("      error.value = null;");
        sb.AppendLine("      return await apiCall();");
        sb.AppendLine("    } catch (err) {");
        sb.AppendLine("      error.value = err instanceof Error ? err.message : 'Unknown error';");
        sb.AppendLine("      return null;");
        sb.AppendLine("    } finally {");
        sb.AppendLine("      loading.value = false;");
        sb.AppendLine("    }");
        sb.AppendLine("  };");
        sb.AppendLine();
        sb.AppendLine("  return { loading, error, execute };");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private string GeneratePackageJson(WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("{");
        sb.AppendLine("  \"name\": \"vue-app\",");
        sb.AppendLine("  \"version\": \"1.0.0\",");
        sb.AppendLine("  \"dependencies\": {");
        sb.AppendLine("    \"vue\": \"^3.2.0\",");
        sb.AppendLine("    \"vue-router\": \"^4.1.0\"");
        sb.AppendLine("  },");
        sb.AppendLine("  \"devDependencies\": {");
        sb.AppendLine("    \"@vitejs/plugin-vue\": \"^4.0.0\",");
        sb.AppendLine("    \"typescript\": \"^4.9.0\",");
        sb.AppendLine("    \"vite\": \"^4.0.0\"");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private string GenerateViteConfig(WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("import { defineConfig } from 'vite';");
        sb.AppendLine("import vue from '@vitejs/plugin-vue';");
        sb.AppendLine();
        sb.AppendLine("export default defineConfig({");
        sb.AppendLine("  plugins: [vue()],");
        sb.AppendLine("  server: {");
        sb.AppendLine("    port: 3000");
        sb.AppendLine("  }");
        sb.AppendLine("});");
        
        return sb.ToString();
    }

    private string GenerateESLintConfig(WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("{");
        sb.AppendLine("  \"extends\": [\"eslint:recommended\", \"plugin:vue/vue3-recommended\"],");
        sb.AppendLine("  \"parserOptions\": {");
        sb.AppendLine("    \"ecmaVersion\": 2021,");
        sb.AppendLine("    \"sourceType\": \"module\"");
        sb.AppendLine("  },");
        sb.AppendLine("  \"rules\": {");
        sb.AppendLine("    \"vue/multi-word-component-names\": \"off\"");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private string GetTypeScriptType(string type)
    {
        return type.ToLower() switch
        {
            "string" => "string",
            "number" => "number",
            "boolean" => "boolean",
            "date" => "Date",
            _ => "any"
        };
    }
}
