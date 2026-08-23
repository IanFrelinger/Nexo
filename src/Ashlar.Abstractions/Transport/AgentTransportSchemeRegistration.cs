namespace Ashlar.Abstractions.Transport;

/// <summary>
/// Registers an additional remote agent transport selected by a target-endpoint prefix.
///
/// The runtime's routing composition supports exactly one remote transport; protocol adapters
/// (e.g. A2A) contribute themselves by adding one of these registrations to DI. Endpoints like
/// <c>a2a+https://host</c> are dispatched to the matching transport; unmatched endpoints fall
/// back to the default remote transport (gRPC today). The prefix is left ON the endpoint —
/// each transport owns stripping its own scheme, keeping the dispatcher protocol-agnostic.
/// </summary>
/// <param name="EndpointPrefix">Case-insensitive prefix matched against <see cref="AgentInvocationOptions.TargetEndpoint"/> (e.g. <c>a2a+</c>).</param>
/// <param name="TransportFactory">Factory resolving the transport from the service provider.</param>
public sealed record AgentTransportSchemeRegistration(
    string EndpointPrefix,
    Func<IServiceProvider, IAgentTransport> TransportFactory);
