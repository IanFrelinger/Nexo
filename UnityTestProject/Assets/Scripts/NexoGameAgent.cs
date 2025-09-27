using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Embedded Nexo Agent for natural language game development
    /// </summary>
    public partial class NexoGameAgent : MonoBehaviour
    {
        [Header("Nexo Agent Configuration")]
        [SerializeField] private string gameSpecification = "";
        [SerializeField] private bool autoGenerateAssets = true;
        [SerializeField] private bool enableRealTimeGeneration = true;
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button generateButton;
        [SerializeField] private Button testButton;
        
        [Header("Generated Assets")]
        [SerializeField] private List<Texture2D> generatedTextures = new();
        [SerializeField] private List<GameObject> generatedModels = new();
        [SerializeField] private List<AudioClip> generatedAudio = new();
        
        private GameSpecificationParser _specParser;
        private AssetGenerator _assetGenerator;
        private GameBuilder _gameBuilder;
        private bool _isGenerating = false;
        
}
