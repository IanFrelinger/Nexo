using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace NexoDoomGame.ExternalGeneration
{
    /// <summary>
    /// Composition component generation functionality for redundancy-free Nexo script generator
    /// </summary>
    public partial class RedundancyFreeNexoGenerator
    {
        private async Task GenerateCompositionComponents(ScriptGenerationConfigV2 config)
        {
            Console.WriteLine("🔗 Generating composition components using templates...");
            
            var outputDir = "GeneratedNexoScripts/Composition";
            Directory.CreateDirectory(outputDir);
            
            foreach (var component in config.CompositionComponents)
            {
                var scriptContent = GenerateCompositionFromTemplate(component, config);
                var filePath = Path.Combine(outputDir, $"{component.Name}.cs");
                await File.WriteAllTextAsync(filePath, scriptContent);
                Console.WriteLine($"✅ Generated {component.Name}.cs");
            }
        }
        
        private string GenerateCompositionFromTemplate(CompositionComponent component, ScriptGenerationConfigV2 config)
        {
            var responsibilities = component.Responsibilities?.Length > 0 
                ? component.Responsibilities 
                : config.Defaults.CommonResponsibilities;
                
            return $@"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexoDoomGame.DomainLogic;
using NexoDoomGame.Composition.Base;

namespace NexoDoomGame.Composition
{{
    /// <summary>
    /// {component.Description}
    /// Responsibilities: {string.Join(", ", responsibilities)}
    /// </summary>
    public class {component.Name} : BaseCompositionComponent
    {{
        /// <summary>
        /// Orchestrate all registered components
        /// </summary>
        public async Task<Dictionary<string, object>> OrchestrateAsync()
        {{
            var results = new Dictionary<string, object>();
            
            foreach (var component in Components)
            {{
                if (component.Value is {string.Join(" or ", config.DomainLogicComponents.Select(c => c.Name))})
                {{
                    results[component.Key] = await ExecuteComponentAsync(component.Value);
                }}
            }}
            
            return results;
        }}
        
        /// <summary>
        /// Initialize all registered components
        /// </summary>
        public async Task InitializeAllAsync()
        {{
            foreach (var component in Components.Values)
            {{
                if (component is BaseDomainLogic domainLogic)
                {{
                    await domainLogic.InitializeAsync();
                }}
            }}
        }}
        
        /// <summary>
        /// Cleanup all registered components
        /// </summary>
        public async Task CleanupAllAsync()
        {{
            foreach (var component in Components.Values)
            {{
                if (component is BaseDomainLogic domainLogic)
                {{
                    await domainLogic.CleanupAsync();
                }}
            }}
        }}
    }}
}}";
        }
    }
}
