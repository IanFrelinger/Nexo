using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Pipeline;
using Nexo.Core.Contracts;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Interfaces;
using YamlDotNet.Serialization;
            using var scope = _serviceProvider.CreateScope();
            using var writer = new StreamWriter(reportPath);

namespace Nexo.CLI.Commands
{
}