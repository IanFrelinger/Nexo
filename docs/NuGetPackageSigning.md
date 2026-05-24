# NuGet package signing (optional)

This repo’s default packages are **unsigned**; `dotnet nuget verify` will report **NU3004** until you adopt signing.

## Why sign

- Stronger **integrity** and **provenance** for consumers who enforce signed packages.
- Enables meaningful **`dotnet nuget verify`** in CI.

## Typical approach

1. Obtain a **code signing certificate** trusted by the NuGet client policy on your consumers’ machines (often a **public CA**-issued cert for broad trust).
2. Use **`dotnet nuget sign`** (or sign during pack with MSBuild targets) on each `.nupkg` **before** push.
3. Register the certificate with **nuget.org** account/org signing requirements per [NuGet signing docs](https://learn.microsoft.com/nuget/create-packages/sign-a-package).

## CI integration (not enabled by default)

- Store the certificate as a **protected secret** (or use a **hardware token** / cloud HSM where your policy allows).
- Add a dedicated workflow or extend **`reusable-release-nuget.yml`** with a guarded step that runs only when a repo variable like **`NUGET_SIGN_PACKAGES=true`** is set—**do not** commit private keys to the repository.

## Further reading

- [Sign a package](https://learn.microsoft.com/nuget/create-packages/sign-a-package) (Microsoft)
- [Signed packages and client trust policies](https://learn.microsoft.com/en-us/nuget/consume-packages/installing-signed-packages)
