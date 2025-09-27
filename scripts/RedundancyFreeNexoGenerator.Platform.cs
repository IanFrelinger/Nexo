using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace NexoDoomGame.ExternalGeneration
{
    /// <summary>
    /// Platform implementation generation functionality for redundancy-free Nexo script generator
    /// </summary>
    public partial class RedundancyFreeNexoGenerator
    {
        private async Task GeneratePlatformImplementations(ScriptGenerationConfigV2 config)
        {
            Console.WriteLine("🎮 Generating platform implementations using templates...");
            
            foreach (var platform in config.PlatformImplementations)
            {
                var platformDir = Path.Combine("GeneratedNexoScripts/PlatformImplementations", platform.Platform);
                Directory.CreateDirectory(platformDir);
                
                foreach (var domainComponent in config.DomainLogicComponents)
                {
                    var scriptContent = GeneratePlatformImplementationFromTemplate(platform, domainComponent);
                    var filePath = Path.Combine(platformDir, $"{domainComponent.Name}Implementation.cs");
                    await File.WriteAllTextAsync(filePath, scriptContent);
                    Console.WriteLine($"✅ Generated {platform.Platform} implementation for {domainComponent.Name}");
                }
            }
        }
        
        private string GeneratePlatformImplementationFromTemplate(PlatformImplementation platform, DomainLogicComponent domainComponent)
        {
            var constructorParams = domainComponent.Dependencies.Select(d => $"I{d.Replace("Provider", "")}Provider {d.ToLower()}").ToArray();
            var baseParams = domainComponent.Dependencies.Select(d => d.ToLower()).ToArray();
            
            return $@"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexoDoomGame.DomainLogic;
using NexoDoomGame.PlatformImplementations.Base;

namespace NexoDoomGame.PlatformImplementations.{platform.Platform}
{{
    /// <summary>
    /// {platform.Platform} implementation of {domainComponent.Name}
    /// Target Framework: {platform.TargetFramework}
    /// Implementation Style: {platform.ImplementationStyle}
    /// </summary>
    public class {domainComponent.Name}Implementation : {domainComponent.Name}
    {{
        public {domainComponent.Name}Implementation({string.Join(", ", constructorParams)})
            : base({string.Join(", ", baseParams)})
        {{
        }}
        
        /// <summary>
        /// {platform.Platform}-specific validation
        /// </summary>
        public override async Task<bool> ValidateAsync()
        {{
            // {platform.Platform}-specific validation logic
            await Task.CompletedTask;
            return true;
        }}
        
        /// <summary>
        /// {platform.Platform}-specific execution
        /// </summary>
        public override async Task<object> ExecuteAsync(object input)
        {{
            // {platform.Platform}-specific execution logic
            await Task.CompletedTask;
            return new {{ Platform = ""{platform.Platform}"", Component = ""{domainComponent.Name}"" }};
        }}
        
        /// <summary>
        /// {platform.Platform}-specific state management
        /// </summary>
        public override async Task<Dictionary<string, object>> GetStateAsync()
        {{
            await Task.CompletedTask;
            return new Dictionary<string, object>
            {{
                [""Platform""] = ""{platform.Platform}"",
                [""Component""] = ""{domainComponent.Name}"",
                [""Domain""] = ""{domainComponent.Domain}"",
                [""Framework""] = ""{platform.TargetFramework}""
            }};
        }}
    }}
}}";
        }
    }
}
