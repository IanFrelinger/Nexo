using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Embedded Nexo instance responsible for composing all generated pieces together
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class NexoCompositionSystem : MonoBehaviour
    {
        [Header("Composition Configuration")]
        [SerializeField] private string generatedScriptsPath = "GeneratedScripts";
        [SerializeField] private string generatedAssetsPath = "GeneratedAssets";
        [SerializeField] private bool autoComposeOnStart = true;
        [SerializeField] private bool enableValidation = true;
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI compositionStatusText;
        [SerializeField] private Slider compositionProgressBar;
        [SerializeField] private Button startCompositionButton;
        [SerializeField] private Button runTestsButton;
        [SerializeField] private TextMeshProUGUI testResultsText;
        
        [Header("Generated Components")]
        [SerializeField] private List<GameObject> composedObjects = new List<GameObject>();
        
        private Nexo.Agent.Contracts.ITaskExecutionAgent _nexoAgent;
        private bool _isComposing = false;
        private List<CompositionResult> _compositionResults = new List<CompositionResult>();
        
        private void Start()
        {
            InitializeCompositionSystem();
            
            if (autoComposeOnStart)
            {
                StartCoroutine(ComposeAllComponents());
            }
        }
        // This class acts as an orchestrator for various composition system functionalities,
        // with specific categories defined in partial classes.
    }
}