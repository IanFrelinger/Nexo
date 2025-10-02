using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace NexoDirectorStudio.Orchestration
{
    public sealed class FileCheckpointStore : ICheckpointStore
    {
        public async Task SaveAsync(PhaseToken token, object output, RunContext ctx)
        {
            var dir = ctx.PhaseFolder(token);
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "output.json");
            File.WriteAllText(path, JsonUtility.ToJson(new Wrapper(output), true));
            await Task.CompletedTask;
        }

        public async Task<(bool found, object data)> TryLoadAsync(PhaseToken token, RunContext ctx)
        {
            var dir = ctx.PhaseFolder(token);
            var path = Path.Combine(dir, "output.json");
            if (!File.Exists(path)) return (false, null);
            var json = File.ReadAllText(path);
            var wrap = JsonUtility.FromJson<Wrapper>(json);
            return (wrap != null && wrap.Payload != null, wrap?.Payload);
        }

        [System.Serializable]
        private sealed class Wrapper { public object Payload; public Wrapper(object o)=>Payload=o; }
    }
}
