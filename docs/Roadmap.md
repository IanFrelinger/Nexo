# Roadmap

This stub tracks release-artifact items that require a post-merge tag or external account/secret setup before they are verifiably true.

## Versioned release artifacts

- Publish versioned NuGet packages for the supported package set.
- Publish versioned GHCR tags for `nexo-cli` and `nexo-api` (the `latest` images already exist; the release sprint makes version pins real).
- After publication, verify the README install instructions work from a clean machine:
  - `docker pull ghcr.io/ianfrelinger/nexo-cli:<version>`
  - `docker pull ghcr.io/ianfrelinger/nexo-api:<version>`
  - `dotnet add package Nexo.Sdk`

These items close after the `v0.1.0` tag workflow publishes artifacts and the manual acid test passes.
