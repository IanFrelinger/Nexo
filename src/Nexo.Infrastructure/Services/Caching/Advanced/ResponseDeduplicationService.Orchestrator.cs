using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Interfaces.Caching;
            using var sha256 = SHA256.Create();

namespace Nexo.Infrastructure.Services.Caching.Advanced
{
    public interface IResponseDeduplicationService
{
    // Orchestration methods will be added here
}
}