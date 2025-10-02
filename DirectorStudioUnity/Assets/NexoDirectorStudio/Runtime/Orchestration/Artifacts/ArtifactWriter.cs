using System.IO;
using UnityEngine;

namespace NexoDirectorStudio.Orchestration
{
    public static class ArtifactWriter
    {
        public static void WriteJson(string folder, string fileName, object data)
        {
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, fileName);
            File.WriteAllText(path, JsonUtility.ToJson(new Wrap{Payload=data}, true));
        }
        [System.Serializable] private class Wrap { public object Payload; }
    }
}
