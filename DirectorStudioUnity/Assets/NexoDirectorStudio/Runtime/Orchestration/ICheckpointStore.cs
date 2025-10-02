using System.Threading.Tasks;

namespace NexoDirectorStudio.Orchestration
{
    public interface ICheckpointStore
    {
        Task SaveAsync(PhaseToken token, object output, RunContext ctx);
        Task<(bool found, object data)> TryLoadAsync(PhaseToken token, RunContext ctx);
    }
}
