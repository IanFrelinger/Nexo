using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Feature.Agent.Models;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Agent.Services
{
                codeResult += $"public partial interface I{SanitizeForClassName(featureDescription)}\n";
}