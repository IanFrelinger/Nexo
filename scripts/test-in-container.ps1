<#
.SYNOPSIS
Runs a dotnet test suite inside the repo's devcontainer image instead of on the
Windows host.

.DESCRIPTION
Windows Smart App Control reputation-blocks freshly built unsigned DLLs
(FileLoadException 0x800711C7), which makes host-side `dotnet test` runs of this
repo unreliable: verdicts are per-file-hash, cached, and re-rolled on every
rebuild. SAC has no exclusion mechanism, so the sandboxed answer is to build and
test inside a Linux container, where SAC does not apply.

The container clones the repo from a read-only bind mount, so the Windows
working tree is never polluted with Linux bin/obj output. Only COMMITTED state
is tested — commit (or at least `git add` nothing and commit locally) before
running. NuGet packages persist in the same named volume the devcontainer uses
(nexo-nuget-packages), so repeat runs skip most of the restore.

.PARAMETER Filter
xUnit filter expression. Defaults to the cert-gate namespace filter.

.PARAMETER Framework
Target framework to test. Defaults to net9.0 (the image carries the 9.x SDK;
DOTNET_ROLL_FORWARD=LatestMajor covers net8.0 test hosts too).

.PARAMETER Project
Test project path, repo-relative. Defaults to Nexo.Tests.Infrastructure.

.PARAMETER Ref
Git ref to test. Defaults to HEAD of the current branch.

.EXAMPLE
pwsh scripts/test-in-container.ps1 -Filter "FullyQualifiedName~CertifiedBrickHotSwapHostTests"
#>
param(
    [string]$Filter = "FullyQualifiedName~Nexo.Tests.Infrastructure.Tests.Certification",
    [string]$Framework = "net9.0",
    [string]$Project = "src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj",
    [string]$Ref = "HEAD"
)

$ErrorActionPreference = "Stop"

$repoRoot = (git rev-parse --show-toplevel)
if (-not $repoRoot) { throw "Not inside a git repository." }
$sha = (git rev-parse $Ref)

# Same image + roll-forward env as .devcontainer/devcontainer.json.
$image = "mcr.microsoft.com/devcontainers/dotnet:9.0-bookworm"

Write-Host "== container test: $sha ($Framework) filter='$Filter' =="

docker run --rm --user root `
    -v "${repoRoot}:/src-mirror:ro" `
    -v nexo-nuget-packages:/root/.nuget/packages `
    -e DOTNET_ROLL_FORWARD=LatestMajor `
    $image `
    bash -lc "set -e; git config --global safe.directory '*'; git clone -q /src-mirror /repo; cd /repo; git checkout -q $sha; dotnet test '$Project' --framework '$Framework' --filter '$Filter' --nologo -v minimal"

exit $LASTEXITCODE
