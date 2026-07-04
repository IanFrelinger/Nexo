using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification.Composition;

/// <summary>
/// Real-model composition proposer — delegates to <see cref="ICompositionGeneratorModel"/>.
/// </summary>
public sealed class ModelCompositionProposer : ICompositionProposer
{
    private readonly ICompositionGeneratorModel _model;

    /// <summary>Initializes a new model-backed composition proposer.</summary>
    public ModelCompositionProposer(ICompositionGeneratorModel model) =>
        _model = model ?? throw new ArgumentNullException(nameof(model));

    /// <summary>Propose asynchronously.</summary>
    public Task<ProposedComposition> ProposeAsync(
        CompositionProposerInput input,
        CancellationToken cancellationToken = default) =>
        _model.ProposeAsync(input, cancellationToken);
}
