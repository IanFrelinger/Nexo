# gRPC transport host (`Nexo.Transport.Grpc.Server.Host`)

`src/Nexo.Transport.Grpc.Server.Host` is the standalone server for the Nexo gRPC agent transport: it boots the kernel (`AddNexo()`), maps `AgentTransportServiceImpl` (`MapNexoGrpcServer`) and answers `GET /` with a one-line banner. It is not co-hosted in `Nexo.API` (`IngressCatalog` lists it as a separate host); `Nexo.API` and the CLI are its **clients** through `GrpcAgentTransport` when a routing endpoint has no scheme prefix of its own (`SchemeDispatchingAgentTransport` fallback).

```bash
dotnet run --project src/Nexo.Transport.Grpc.Server.Host
# -> Now listening on: http://127.0.0.1:5001
```

## Listen address and protocol

| Knob | Where | Default | Notes |
|------|-------|---------|-------|
| `Urls` / `ASPNETCORE_URLS` | `appsettings.json` / env | `http://127.0.0.1:5001` | Loopback by default, like every other Nexo host. Widen deliberately: `ASPNETCORE_URLS=http://0.0.0.0:5001` inside a container. Env wins over `appsettings.json`. |
| HTTP/2 | `Program.cs` (`ConfigureEndpointDefaults(... Http2)`) and `Kestrel:EndpointDefaults:Protocols` | `Http2` on every endpoint | gRPC needs HTTP/2. On a plain `http://` URL that is **h2c** (prior-knowledge HTTP/2, no TLS): fine on loopback or inside a private compose/k8s network, not on a shared network. Browsers and `curl` without `--http2-prior-knowledge` will not get the banner. |
| TLS | `Kestrel:Certificates:Default:Path` + `KeyPath` (PEM) or `Path` + `Password` (PFX) | none | Give the URL an `https://` scheme and point Kestrel at the server certificate, e.g. `ASPNETCORE_URLS=https://0.0.0.0:5001 Kestrel__Certificates__Default__Path=/run/secrets/server.crt Kestrel__Certificates__Default__KeyPath=/run/secrets/server.key`. Kestrel then negotiates HTTP/2 over ALPN. |
| Client-cert requirement (mTLS) | `Kestrel:Endpoints:<name>:ClientCertificateMode` | not required | Set `RequireCertificate` on an explicit endpoint entry to demand the client certificate that `Nexo:GrpcTransport:ClientCertPath` presents (see below). |

Health: this host has no `/health` or `/ready` route; a TCP probe on the listen port (or a gRPC health service you add) is the check to use.

## Client side: `Nexo:GrpcTransport` and the `/run/secrets/*` defaults

Both hosts bind `GrpcTransportOptions` from the `Nexo:GrpcTransport` section in `Program.cs` (host configuration, so `appsettings.json` **and** `Nexo__GrpcTransport__*` env vars work; this is *not* one of the options that `AddNexo` binds from environment variables only). `DefaultGrpcChannelFactory` reads it when it opens a channel to an endpoint:

| Key | `Nexo.API` `appsettings.json` default | Effect |
|-----|-------------------------------------|--------|
| `AllowInsecure` | `false` | `true` permits unencrypted HTTP/2 and skips server-certificate validation. `GrpcTransportOptions.Validate` **throws outside `Development`**, so it cannot be enabled in a production host. |
| `MaxRetryAttempts` | `3` | Retry budget for transport callers. |
| `ClientCertPath` / `ClientCertKeyPath` | `/run/secrets/client.crt` / `/run/secrets/client.key` | PEM client certificate + key presented for mTLS. |
| `CaCertPath` | `/run/secrets/ca.crt` | Private CA used to validate the server certificate (custom root trust, no revocation check). |

The `Nexo.API` defaults name Docker/compose **secret** paths, but no shipped compose file mounts them: the values are placeholders for the private-PKI shape below. They are read lazily (first channel to a gRPC endpoint), so a portal or agent-server stack that never routes to a gRPC endpoint is unaffected. Set the three paths to `null` (or empty env values) when your gRPC hosts use publicly trusted certificates without client auth. The gRPC server host's own `appsettings.json` ships them as `null`.

## Compose shape (server + API client with private PKI)

```yaml
services:
  nexo-grpc:
    # No shipped Dockerfile publishes this host yet: copy .docker/Dockerfile.api and swap the
    # project + ENTRYPOINT for src/Nexo.Transport.Grpc.Server.Host (same aspnet:8.0 base, same
    # HEALTHCHECK-less shape; add a TCP check if you want compose --wait to gate on it).
    image: your-registry/nexo-grpc-host:<tag>
    environment:
      ASPNETCORE_URLS: https://0.0.0.0:5001
      Kestrel__Certificates__Default__Path: /run/secrets/server.crt
      Kestrel__Certificates__Default__KeyPath: /run/secrets/server.key
      NEXO_DEPLOYMENT_PROFILE: server
    secrets: [server.crt, server.key]
    expose: ["5001"]            # in-network only; no host port

  nexo-api:
    build:
      context: ../..
      dockerfile: .docker/Dockerfile.api
    environment:
      Nexo__Routing__Endpoints__0__Endpoint: https://nexo-grpc:5001
      Nexo__Routing__Endpoints__0__Name: grpc-worker
      Nexo__GrpcTransport__ClientCertPath: /run/secrets/client.crt
      Nexo__GrpcTransport__ClientCertKeyPath: /run/secrets/client.key
      Nexo__GrpcTransport__CaCertPath: /run/secrets/ca.crt
    secrets: [client.crt, client.key, ca.crt]
    ports:
      - "127.0.0.1:8080:8080"

secrets:
  server.crt: { file: ./pki/server.crt }
  server.key: { file: ./pki/server.key }
  client.crt: { file: ./pki/client.crt }
  client.key: { file: ./pki/client.key }
  ca.crt:     { file: ./pki/ca.crt }
```

Compose mounts file secrets at `/run/secrets/<name>` read-only, which is exactly what the `Nexo.API` defaults expect; the images run as the non-root `app` user, so give the key files mode `0644` (or `0640` with a matching group) on the host.

## Related

- `docs/Configuration.md` — `NEXO_DEPLOYMENT_PROFILE`, routing endpoints, `Nexo.API` exposure
- `docs/DEPLOYMENT.md` — container health/readiness/non-root notes and the runtime-state volume
- `docs/architecture/ProtocolIntegration-MCP-A2A.md` — how the A2A/MCP schemes dispatch next to bare gRPC endpoints
