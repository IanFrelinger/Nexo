using Nexo.Shared.Interfaces;

namespace Nexo.Shared.Services
{
    public class SharedService : ISharedService
    {
        public async Task<string> GetSharedDataAsync()
        {
            await Task.Delay(50); // Simulate processing
            return "Shared data from core";
        }
    }
}
