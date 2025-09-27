using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Enums;

namespace Nexo.Core.Application.Services.AI.Pipeline
{
    /// <summary>
    /// AI runtime selection and engine creation functionality
    /// </summary>
    public partial class AIOptimizationStep
    {
        private string GetModelPathForOptimization(AIEngineType engineType)
        {
            return engineType switch
            {
                AIEngineType.LlamaWebAssembly => "models/codellama-7b-instruct.gguf",
                AIEngineType.LlamaNative => "models/codellama-13b-instruct.gguf",
                _ => "models/codellama-7b-instruct.gguf"
            };
        }

        private Nexo.Core.Domain.Enums.PlatformType ConvertToEnumsPlatformType(Nexo.Core.Domain.Entities.Infrastructure.PlatformType platformType)
        {
            return platformType switch
            {
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Web => Nexo.Core.Domain.Enums.PlatformType.Web,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Desktop => Nexo.Core.Domain.Enums.PlatformType.Desktop,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Mobile => Nexo.Core.Domain.Enums.PlatformType.Mobile,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Console => Nexo.Core.Domain.Enums.PlatformType.Desktop, // Map Console to Desktop
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Windows => Nexo.Core.Domain.Enums.PlatformType.Windows,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Linux => Nexo.Core.Domain.Enums.PlatformType.Linux,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.macOS => Nexo.Core.Domain.Enums.PlatformType.macOS,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.WebAssembly => Nexo.Core.Domain.Enums.PlatformType.Web, // Map WebAssembly to Web
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.iOS => Nexo.Core.Domain.Enums.PlatformType.iOS,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Android => Nexo.Core.Domain.Enums.PlatformType.Android,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Cloud => Nexo.Core.Domain.Enums.PlatformType.Cloud,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Docker => Nexo.Core.Domain.Enums.PlatformType.Container,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Other => Nexo.Core.Domain.Enums.PlatformType.CrossPlatform,
                _ => Nexo.Core.Domain.Enums.PlatformType.Unknown
            };
        }
    }
}
