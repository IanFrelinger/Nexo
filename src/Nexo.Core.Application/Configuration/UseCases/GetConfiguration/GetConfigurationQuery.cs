using MediatR;
using Nexo.Core.Application.Configuration.Models;

namespace Nexo.Core.Application.Configuration.UseCases.GetConfiguration;

/// <summary>
/// Query for getting current configuration.
/// </summary>
public record GetConfigurationQuery : IRequest<NexoConfiguration>;

