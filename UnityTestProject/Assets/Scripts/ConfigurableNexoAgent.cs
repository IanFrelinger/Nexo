using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Configurable Nexo Agent that uses existing Nexo Agent system for code generation.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class ConfigurableNexoAgent : MonoBehaviour
    {
        [Header("Agent Configuration")]
        [SerializeField] private AgentConfiguration agentConfig;
        [SerializeField] private bool autoGenerateOnStart = false;
        [SerializeField] private bool enableRealTimeGeneration = true;
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button generateButton;
        [SerializeField] private Button testButton;
        [SerializeField] private TMP_InputField specInputField;
        
        [Header("Generated Assets")]
        [SerializeField] private List<GeneratedAsset> generatedAssets = new();
        
        // Use existing Nexo Agent system
        private Nexo.Agent.Contracts.ITaskExecutionAgent _nexoAgent;
        private Nexo.Agent.Contracts.IToolRegistry _toolRegistry;
        private bool _isGenerating = false;
        
        private void Start()
        {
            InitializeAgent();
            if (autoGenerateOnStart)
            {
                GenerateGameAsync();
            }
        }
        // This class acts as an orchestrator for various configurable Nexo agent functionalities,
        // with specific categories defined in partial classes.
    }
}