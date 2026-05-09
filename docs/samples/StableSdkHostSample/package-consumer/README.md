# Stable SDK host sample (package consumption)

This project references **`Nexo.Hosting.Bundle`** from NuGet only (metapackage: full `Nexo.*` graph at one version; no project references to the Nexo repo). It shares `Program.cs` with the sibling sample.

Verify locally (from repo root):

```bash
NEXO_SDK_PACKAGE_VERSION=1.0.0-local bash scripts/verify-stable-sdk-host-sample-packages.sh
```

Override package version at restore time:

```bash
dotnet restore -p:NexoSdkPackageVersion=1.2.3
dotnet build --no-restore
```
