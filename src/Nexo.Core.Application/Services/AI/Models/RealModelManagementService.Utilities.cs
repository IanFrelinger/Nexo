using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Models
{
    /// <summary>
    /// Helper methods and utilities for RealModelManagementService
    /// </summary>
    public partial class RealModelManagementService
    {
        private Nexo.Core.Domain.Enums.PlatformType GetCurrentPlatformType()
        {
            if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
                return Nexo.Core.Domain.Enums.PlatformType.Windows;
            else if (System.Environment.OSVersion.Platform == PlatformID.MacOSX)
                return Nexo.Core.Domain.Enums.PlatformType.macOS;
            else if (System.Environment.OSVersion.Platform == PlatformID.Unix)
                return Nexo.Core.Domain.Enums.PlatformType.Linux;
            else
                return Nexo.Core.Domain.Enums.PlatformType.Unknown;
        }

        private Nexo.Core.Domain.Entities.Infrastructure.PlatformType ConvertToInfrastructurePlatformType(Nexo.Core.Domain.Enums.PlatformType platformType)
        {
            return platformType switch
            {
                Nexo.Core.Domain.Enums.PlatformType.Web => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Web,
                Nexo.Core.Domain.Enums.PlatformType.Desktop => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Desktop,
                Nexo.Core.Domain.Enums.PlatformType.Mobile => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Mobile,
                Nexo.Core.Domain.Enums.PlatformType.Windows => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Windows,
                Nexo.Core.Domain.Enums.PlatformType.Linux => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Linux,
                Nexo.Core.Domain.Enums.PlatformType.macOS => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.macOS,
                Nexo.Core.Domain.Enums.PlatformType.iOS => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.iOS,
                Nexo.Core.Domain.Enums.PlatformType.Android => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Android,
                Nexo.Core.Domain.Enums.PlatformType.Cloud => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Cloud,
                Nexo.Core.Domain.Enums.PlatformType.Container => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Docker,
                Nexo.Core.Domain.Enums.PlatformType.CrossPlatform => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Other,
                _ => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Unknown
            };
        }
    }
}
