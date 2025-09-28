namespace Nexo.Shared.Interfaces
{
    public interface ISharedService
    {
        Task<string> GetSharedDataAsync();
    }
}
