# Roadmap

This stub tracks items intentionally removed from the README until they are verifiably true.

## Versioned release artifacts

- Publish versioned NuGet packages for the supported package set.
- Publish versioned GHCR tags for `nexo-cli` and `nexo-api` (the `latest` images already exist; the release sprint makes version pins real).
- After publication, restore README install instructions for:
  - `docker pull ghcr.io/ianfrelinger/nexo-cli:<version>`
  - `docker pull ghcr.io/ianfrelinger/nexo-api:<version>`
  - `dotnet add package Nexo.Sdk`

These items are planned for the v0.1.0 release preparation sprint.
