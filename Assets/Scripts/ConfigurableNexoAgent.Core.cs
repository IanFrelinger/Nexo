using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Core configurable Nexo agent functionality
    /// </summary>
    public partial class ConfigurableNexoAgent
    {
        private void InitializeAgent()
        {
            Debug.Log("🎮 Initializing Configurable Nexo Agent...");
            
            // Initialize existing Nexo Agent system
            InitializeNexoAgentSystem();
            
            // Set up UI
            SetupUI();
            
            UpdateStatus("Configurable Nexo Agent Ready");
        }

        private void InitializeNexoAgentSystem()
        {
            try
            {
                // Create Nexo Agent instance
                _nexoAgent = new Nexo.Agent.Implementations.AtlasAgent(
                    new Nexo.Agent.Implementations.SimplePlanner(),
                    new Nexo.Agent.Implementations.ToolBroker(),
                    new Nexo.Agent.Implementations.PipelineToolFactory()
                );
                
                // Create tool registry
                _toolRegistry = new Nexo.Agent.Implementations.ToolRegistry();
                
                // Register built-in tools
                RegisterBuiltinTools();
                
                Debug.Log("✅ Nexo Agent system initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to initialize Nexo Agent system: {ex.Message}");
            }
        }

        private void RegisterBuiltinTools()
        {
            // Register built-in tools for code generation
            var fileReadTool = new Nexo.Agent.Tools.Builtin.FileReadTool();
            var reportWriteTool = new Nexo.Agent.Tools.Builtin.ReportWriteTool();
            var summarizeTool = new Nexo.Agent.Tools.Builtin.SummarizeTool();
            
            _toolRegistry.RegisterTool(fileReadTool);
            _toolRegistry.RegisterTool(reportWriteTool);
            _toolRegistry.RegisterTool(summarizeTool);
            
            Debug.Log("🔧 Built-in tools registered");
        }

        private void SetupUI()
        {
            if (generateButton != null)
                generateButton.onClick.AddListener(GenerateGameAsync);
            
            if (testButton != null)
                testButton.onClick.AddListener(TestGame);
            
            if (specInputField != null)
                specInputField.text = LoadDefaultSpecification();
        }

        public void UpdateConfiguration(AgentConfiguration newConfig)
        {
            agentConfig = newConfig;
            InitializeAgent();
        }

        public List<GeneratedAsset> GetGeneratedAssets()
        {
            return generatedAssets;
        }
    }
}
