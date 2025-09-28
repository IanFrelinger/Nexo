namespace Nexo.Core.Application.Interfaces
{
    public interface ICoreService
    {
        Task<string> ProcessRequestAsync(string request);
    }
}
