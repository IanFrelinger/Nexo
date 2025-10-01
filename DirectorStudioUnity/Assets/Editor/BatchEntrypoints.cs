using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;
using NexoDirectorStudio.Agents;

public static class BatchEntrypoints
{
    [MenuItem("Tools/DirectorStudio/BatchGenerate Plan+Build")]
    public static async void GenerateDoomSlicePlanBuild()
    {
        var go = new GameObject("AgentDirector (Batch)");
        var director = go.AddComponent<AgentDirector>();
        director.prompt = "Doom-style FPS. 15 min. Intense combat, key gates. seed=666.";
        director.autoLaunch = false;
        director.attachAutoplayer = false;
        await director.RunAsync(System.Threading.CancellationToken.None);
        Debug.Log("Batch generation complete.");
        EditorApplication.Exit(0);
    }
}
