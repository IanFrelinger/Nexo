using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
            using (var fileStream = File.Create(filePath))
            using var md5 = System.Security.Cryptography.MD5.Create();
            using var stream = File.OpenRead(filePath);

namespace Nexo.Core.Application.Services.AI.Models
{
}