using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace NexoDoomGame.ExternalGeneration
{
    /// <summary>
    /// Domain logic generation functionality for redundancy-free Nexo script generator
    /// </summary>
    public partial class RedundancyFreeNexoGenerator
    {
        private async Task GenerateDomainLogicComponents(ScriptGenerationConfigV2 config)
        {
            Console.WriteLine("🏗️ Generating domain logic components using templates...");
            
            var outputDir = "GeneratedNexoScripts/DomainLogic";
            Directory.CreateDirectory(outputDir);
            
            foreach (var component in config.DomainLogicComponents)
            {
                var scriptContent = GenerateDomainLogicFromTemplate(component, config.Defaults);
                var filePath = Path.Combine(outputDir, $"{component.Name}.cs");
                await File.WriteAllTextAsync(filePath, scriptContent);
                Console.WriteLine($"✅ Generated {component.Name}.cs");
            }
        }
        
        private string GenerateDomainLogicFromTemplate(DomainLogicComponent component, ConfigurationDefaults defaults)
        {
            var crossDomainUsages = component.CrossDomainUsages?.Length > 0 
                ? component.CrossDomainUsages 
                : defaults.CrossDomainUsages;
            
            var dependencies = component.Dependencies.Select(d => $"I{d.Replace("Provider", "")}Provider").ToArray();
            var constructorParams = component.Dependencies.Select(d => $"I{d.Replace("Provider", "")}Provider {d.ToLower()}").ToArray();
            var constructorAssignments = component.Dependencies.Select(d => $"this.{d.ToLower()} = {d.ToLower()};").ToArray();
                
            return $@"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexoDoomGame.DomainLogic.Base;

namespace NexoDoomGame.DomainLogic
{{
    /// <summary>
    /// Abstract domain logic for {component.Name}
    /// Domain: {component.Domain}
    /// Cross-domain usage: {string.Join(", ", crossDomainUsages)}
    /// </summary>
    public abstract class {component.Name} : BaseDomainLogic
    {{
        {string.Join("\n        ", dependencies.Select(d => $"protected readonly {d};"))}
        
        protected {component.Name}({string.Join(", ", constructorParams)})
        {{
            {string.Join("\n            ", constructorAssignments)}
        }}
        
        /// <summary>
        /// {component.Description}
        /// </summary>
        public override abstract Task<bool> ValidateAsync();
        
        /// <summary>
        /// Execute the core domain logic
        /// </summary>
        public override abstract Task<object> ExecuteAsync(object input);
        
        /// <summary>
        /// Get the current state of the component
        /// </summary>
        public override abstract Task<Dictionary<string, object>> GetStateAsync();
    }}
    
    /// <summary>
    /// Interface for {component.Name} providers
    /// </summary>
    public interface I{component.Name}Provider : IBaseDomainLogicProvider
    {{
        new Task<{component.Name}> CreateAsync();
    }}
    
    /// <summary>
    /// Interface for {component.Name} validation
    /// </summary>
    public interface I{component.Name}Validator : IBaseDomainLogicValidator
    {{
        Task<bool> ValidateAsync({component.Name} component);
    }}
}}";
        }
    }
}
