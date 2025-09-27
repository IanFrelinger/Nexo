using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Models;
using Nexo.Feature.Factory.Testing.Progress;
using Nexo.Feature.Factory.Testing.Coverage;
using Nexo.Feature.Factory.Testing.Timeout;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Simple testing commands that don't depend on complex AI features to prevent hanging.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public static partial class SimpleTestingCommands
    {
}
