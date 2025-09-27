using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Core functionality for NexoTaskOrchestrator.
    /// </summary>
    public partial class NexoTaskOrchestrator : MonoBehaviour
    {
        [Header("Nexo Agent Configuration")]
        [SerializeField] private string configFilePath = "Assets/NexoConfig/UnityGenerationConfig.json";
        [SerializeField] private string promptsFilePath = "Assets/NexoPrompts/GameGenerationPrompts.md";
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button startGenerationButton;
        [SerializeField] private Button stopGenerationButton;
        [SerializeField] private TMP_InputField customPromptInput;
        [SerializeField] private Toggle enableDebugMode;
        
        [Header("Debug Console")]
        [SerializeField] private TextMeshProUGUI debugConsole;
        [SerializeField] private ScrollRect debugScrollRect;
        
        private Nexo.Agent.Contracts.ITaskExecutionAgent _nexoAgent;
        private UnityGenerationConfig _config;
        private List<string> _generationTasks;
        private bool _isGenerating = false;
        private int _currentTaskIndex = 0;
        
        private void Start()
        {
            InitializeOrchestrator();
            SetupUI();
        }
    }
}
