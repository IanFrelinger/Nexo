using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace NexoDoomGame.ExternalGeneration
{
    /// <summary>
    /// Core functionality for redundancy-free Nexo script generator
    /// </summary>
    public partial class RedundancyFreeNexoGenerator
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🎯 Starting Redundancy-Free Nexo Script Generation...");
            
            var generator = new RedundancyFreeNexoGenerator();
            await generator.GenerateScriptsAsync();
            
            Console.WriteLine("✅ Redundancy-free Nexo script generation completed!");
        }
        
        public async Task GenerateScriptsAsync()
        {
            // Load configuration
            var config = LoadConfiguration();
            
            // Generate base classes and interfaces
            await GenerateBaseClasses();
            
            // Generate domain logic components using templates
            await GenerateDomainLogicComponents(config);
            
            // Generate platform implementations using templates
            await GeneratePlatformImplementations(config);
            
            // Generate composition components using templates
            await GenerateCompositionComponents(config);
            
            // Create analysis report
            CreateAnalysisReport(config);
        }
        
        private ScriptGenerationConfigV2 LoadConfiguration()
        {
            var configPath = "ScriptGenerationConfigV2.json";
            
            if (File.Exists(configPath))
            {
                var configJson = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<ScriptGenerationConfigV2>(configJson) ?? new ScriptGenerationConfigV2();
            }
            
            return new ScriptGenerationConfigV2();
        }
    }
}
