using UnityEngine;
using UnityEditor;
using System.IO;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Static class that can be called from command line to update iteration assets
    /// </summary>
    public static class IterativeDemoAssetUpdater
    {
        /// <summary>
        /// Update assets for a specific iteration - called from CLI
        /// </summary>
        public static void UpdateIterationAssets()
        {
            // Get iteration number from command line args
            string[] args = System.Environment.GetCommandLineArgs();
            int iteration = 1;
            
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-iteration" && int.TryParse(args[i + 1], out int iter))
                {
                    iteration = iter;
                    break;
                }
            }
            
            IterativeDemoController.UpdateIterationAssets(iteration);
            
            // Force asset database refresh
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            
            UnityEngine.Debug.Log($"✅ Updated Unity assets for iteration {iteration}");
        }
    }
}

