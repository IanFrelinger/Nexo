using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Shared.Models;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Interfaces
{
public partial interface ICommand<in TRequest, TResponse>
}