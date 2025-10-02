#if UNITY_EDITOR
using System;
using UnityEngine;

namespace NexoDirectorStudio.Config
{
    [Serializable] public class PipelinePhaseConfig { public string token; public string type; public bool cache; public int timeoutMs; }
    [Serializable] public class PipelineConfig
    {
        public string name = "MyGameSlice";
        public string version = "0.1.0";
        public int seed = 1337;
        public string prompt = "micro room with one switch and one door";
        public string artifacts = "Artifacts";
        public PipelinePhaseConfig[] phases = Array.Empty<PipelinePhaseConfig>();
        public string adapters = "nexo.adapters.json";
        public string acceptance = "Assets/NexoDirectorStudio/Acceptance/Default.asset";
        public string[] scenarios = Array.Empty<string>();
    }

    public static class PipelineConfigLoader
    {
        public static PipelineConfig LoadFromPath(string path, PipelineConfig @default = null)
        {
            try
            {
                var json = System.IO.File.ReadAllText(path);
                var cfg = JsonUtility.FromJson<PipelineConfig>(json);
                return cfg ?? (@default ?? new PipelineConfig());
            }
            catch { return @default ?? new PipelineConfig(); }
        }
    }
}
#endif
