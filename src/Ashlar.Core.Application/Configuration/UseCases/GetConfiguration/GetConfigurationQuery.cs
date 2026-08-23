using MediatR;
using Ashlar.Core.Application.Configuration.Models;

namespace Ashlar.Core.Application.Configuration.UseCases.GetConfiguration;

/// <summary>
/// Query for getting current configuration.
/// </summary>
public record GetConfigurationQuery : IRequest<AshlarConfiguration>;

