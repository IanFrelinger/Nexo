using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Initialization and setup functionality
    /// </summary>
    public partial class MasterValidationController
    {
        private void InitializeMasterController()
        {
            Debug.Log("🎯 Initializing Master Validation Controller...");
            
            // Ensure all components are available
            if (selfValidator == null)
                selfValidator = FindObjectOfType<NexoSelfValidator>();
            
            if (improvementEngine == null)
                improvementEngine = FindObjectOfType<QualityImprovementEngine>();
            
            if (taskOrchestrator == null)
                taskOrchestrator = FindObjectOfType<NexoTaskOrchestrator>();
            
            if (debugger == null)
                debugger = FindObjectOfType<NexoDebugger>();
            
            UpdateMasterStatus("Master Validation Controller Ready");
            LogMaster("🎯 Master Controller initialized");
        }
        
        private void SetupUI()
        {
            if (startFullValidationButton != null)
                startFullValidationButton.onClick.AddListener(StartFullValidation);
            
            if (stopValidationButton != null)
                stopValidationButton.onClick.AddListener(StopValidation);
        }
    }
}
