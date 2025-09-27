using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.Enums.Code;
using System;

namespace Nexo.Core.Application.Services.AI.Pipeline
{
    /// <summary>
    /// Utility methods and type conversions
    /// </summary>
    public partial class AITestingStep
    {
        private Nexo.Core.Domain.Enums.Code.CodeLanguage ParseLanguage(string language)
        {
            return language.ToLower() switch
            {
                "csharp" or "c#" => Nexo.Core.Domain.Enums.Code.CodeLanguage.CSharp,
                "javascript" or "js" => Nexo.Core.Domain.Enums.Code.CodeLanguage.JavaScript,
                "typescript" or "ts" => Nexo.Core.Domain.Enums.Code.CodeLanguage.TypeScript,
                "python" or "py" => Nexo.Core.Domain.Enums.Code.CodeLanguage.Python,
                "java" => Nexo.Core.Domain.Enums.Code.CodeLanguage.Java,
                "go" => Nexo.Core.Domain.Enums.Code.CodeLanguage.Go,
                "rust" => Nexo.Core.Domain.Enums.Code.CodeLanguage.Rust,
                _ => Nexo.Core.Domain.Enums.Code.CodeLanguage.CSharp
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
