using Nexo.Core.Application.Interfaces;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services
{
    public class CoreService : ICoreService
    {
        public async Task<string> ProcessRequestAsync(string request)
        {
            await Task.Delay(100); // Simulate processing
            return $"Processed: {request}";
        }
    }
}
