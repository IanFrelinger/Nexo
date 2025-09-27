using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Feature.AI.Agents.Specialized;
using Nexo.Feature.AI.Learning;

namespace Nexo.Feature.AI.Monitoring;
{
public interface IPerformanceMetricsCollector
}