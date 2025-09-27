using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Infrastructure.Tests.Commands;
using System;
using System.Threading.Tasks;
        using var command = new MemoryCacheCommand(NullLogger<MemoryCacheCommand>.Instance);
        using var command = new MemoryCacheCommand(NullLogger<MemoryCacheCommand>.Instance);
        using var command = new MemoryCacheCommand(NullLogger<MemoryCacheCommand>.Instance);
        using var command = new MemoryCacheCommand(NullLogger<MemoryCacheCommand>.Instance);

namespace Nexo.Infrastructure.Tests;
{
}