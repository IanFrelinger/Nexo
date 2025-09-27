using System;
using UnityEngine;

namespace NexoDoomGame
{
    /// <summary>
    /// Configuration classes and data structures
    /// </summary>
    public partial class NexoTaskOrchestrator
    {
        // This partial class contains all the data model classes
    }
    
    /// <summary>
    /// Configuration classes for JSON serialization
    /// </summary>
    [System.Serializable]
    public class UnityGenerationConfig
    {
        public NexoAgentConfig nexoAgent;
        public GameSpecificationConfig gameSpecification;
        public GenerationSettings generationSettings;
        public OutputSettings outputSettings;
        public TestingSettings testingSettings;
        public DebuggingSettings debuggingSettings;
    }
    
    [System.Serializable]
    public class NexoAgentConfig
    {
        public string mode;
        public bool enableImageGeneration;
        public bool enableCodeGeneration;
        public bool enableAssetGeneration;
        public bool enableRealTimeGeneration;
    }
    
    [System.Serializable]
    public class GameSpecificationConfig
    {
        public string gameType;
        public string artStyle;
        public string[] colorPalette;
        public string[] enemyTypes;
        public string[] weaponTypes;
        public int targetFPS;
        public string platform;
    }
    
    [System.Serializable]
    public class GenerationSettings
    {
        public ScriptGenerationSettings scriptGeneration;
        public AssetGenerationSettings assetGeneration;
        public LevelGenerationSettings levelGeneration;
    }
    
    [System.Serializable]
    public class ScriptGenerationSettings
    {
        public bool includeComments;
        public bool includeErrorHandling;
        public bool includeLogging;
        public string codeStyle;
        public bool useInputSystem;
        public bool useNavMesh;
        public bool useAudioSystem;
    }
    
    [System.Serializable]
    public class AssetGenerationSettings
    {
        public int textureResolution;
        public string textureFormat;
        public bool generateNormalMaps;
        public bool generateSpecularMaps;
        public string audioQuality;
        public string modelDetail;
    }
    
    [System.Serializable]
    public class LevelGenerationSettings
    {
        public int roomCount;
        public int corridorCount;
        public int enemyCount;
        public int pickupCount;
        public string lightingQuality;
        public bool fogEnabled;
    }
    
    [System.Serializable]
    public class OutputSettings
    {
        public string scriptsPath;
        public string prefabsPath;
        public string scenesPath;
        public string texturesPath;
        public string modelsPath;
        public string audioPath;
        public string documentationPath;
    }
    
    [System.Serializable]
    public class TestingSettings
    {
        public bool enablePerformanceTesting;
        public bool enableGameplayTesting;
        public bool enableAudioTesting;
        public bool enableAITesting;
        public int targetFrameRate;
        public string memoryLimit;
        public string loadingTimeLimit;
    }
    
    [System.Serializable]
    public class DebuggingSettings
    {
        public bool enableDebugConsole;
        public bool enablePerformanceOverlay;
        public bool enableErrorLogging;
        public bool enableAssetValidation;
        public bool enableAIStateVisualization;
        public string logLevel;
    }
}
