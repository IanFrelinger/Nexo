# Surface Review — Phase R1 Sprint 3

Sprint 3 makes the supported package surface internal-by-default without renames, moves, or behavior changes. Counts below are source-level public top-level type counts after Sprint 2 and after this visibility-only sweep.

## Supported package public-type counts

| Package | Before | After | Notes |
| --- | ---: | ---: | --- |
| `Nexo.Contracts` | 24 | 24 | Kept public: API request/response records, middleware ingress DTOs/envelopes, and SMS ingress contract/options used across API/ingress packages. |
| `Nexo.Brick.Contracts` | 20 | 20 | Folded into the supported surface: all types are brick wire DTOs/enums/constants serialized by consumers and hosts. |
| `Nexo.Sdk` | 4 | 4 | Kept public: client SDK builder, DI extension entry point, and legacy aliases. |
| `Nexo.Client` | 4 | 4 | Kept public: `INexoClient`, `NexoClient`, options, and DI extensions are used directly in docs/demos and supported client integration. |
| `Nexo.Hosting` | 9 | 9 | Kept public: `AddNexo`/SDK/OpenTelemetry entry points, option bags, deployment profile, and legacy builder alias. |
| `Nexo.Runtime` | 32 | 4 | Runtime implementation classes, registries, monitors, sinks, resolvers, and option models were made internal. |
| `Nexo.Hosting.Bundle` | 0 | 0 | Bundle package has no source public types. |
| `Nexo.Runtime.Bundle` | 0 | 0 | Bundle package has no source public types. |

## `Nexo.Runtime` types made internal

- `AgentHost`
- `CapabilityRegistry`
- `InMemoryAgentMemory`
- `RoutingAgentTransport`
- `RoutingOptions`
- `EndpointDescriptorConfig`
- `RemoteCapabilitiesOptions`
- `InMemoryEndpointRegistry`
- `EndpointHealthMonitor`
- `StructuredBarrierAuditLog`
- `ScopedBarrierContextAccessor`
- `BarrierContextAmbient`
- `BarrierIdentityResolverOptions`
- `DefaultBarrierIdentityResolverPipeline`
- `HttpBarrierContextMiddleware`
- `ApiKeyResolverOptions`
- `ApiKeyBarrierResolver`
- `JwtClaimResolverOptions`
- `JwtClaimBarrierResolver`
- `PkiCertificateResolverOptions`
- `CertificateBarrierRule`
- `PkiCertificateBarrierResolver`
- `FileBarrierAuditSinkOptions`
- `FileBarrierAuditSink`
- `FileBarrierAuditSinkLifetime`
- `NoOpBarrierAuditSink`
- `StructuredLogBarrierAuditSinkOptions`
- `StructuredLogBarrierAuditSink`

Cross-assembly implementation access is granted through `InternalsVisibleTo` from `Nexo.Runtime` to Nexo assemblies/tests that already depended on these implementation details.

## Human-review appendix

These types remain public but should be explicitly reviewed as part of the supported runtime surface:

- `Nexo.Runtime.RuntimeServiceCollectionExtensions` — consumer/host DI entry point for runtime routing and barrier registration.
- `Nexo.Runtime.PolicyEngine` — currently appears in public non-supported background-agent method/factory signatures; changing it would require visibility changes outside the supported packages, which Sprint 3 forbids.
- `Nexo.Runtime.ChainRejectionCallback` — currently appears in public non-supported background-agent method signatures; changing it would require visibility changes outside the supported packages, which Sprint 3 forbids.
- `Nexo.Runtime.BarrierCeilingExceededException` — caught by the CLI host for user-facing barrier failures.

No supported-package type was renamed, moved, or behaviorally refactored.
