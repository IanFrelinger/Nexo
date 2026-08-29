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
(ashlar-nuget-packages), so repeat runs skip most of the restore.

.PARAMETER Filter
xUnit filter expression. Defaults to the cert-gate namespace filter.

.PARAMETER Framework
Target framework to test. Defaults to net10.0. net8.0 works too: the image built by
ensure-devtest-image.ps1 carries the real ASP.NET Core 8 runtime, so net8.0 runs as
net8.0 rather than rolling forward onto ASP.NET Core 10 — which silently breaks every
HTTP-hosting test. See .docker/Dockerfile.devtest.

.PARAMETER Project
Test project path, repo-relative. Defaults to Ashlar.Tests.Infrastructure.

.PARAMETER Ref
Git ref to test. Defaults to HEAD of the current branch.

.EXAMPLE
pwsh scripts/test-in-container.ps1 -Filter "FullyQualifiedName~CertifiedBrickHotSwapHostTests"
#>
param(
    [string]$Filter = "FullyQualifiedName~Ashlar.Tests.Infrastructure.Tests.Certification",
    [string]$Framework = "net10.0",
    [string]$Project = "src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj",
    [string]$Ref = "HEAD"
)

$ErrorActionPreference = "Stop"

$repoRoot = (git rev-parse --show-toplevel)
if (-not $repoRoot) { throw "Not inside a git repository." }
$sha = (git rev-parse $Ref)

# The devcontainer base plus the real ASP.NET Core 8 runtime. This used to be the stock
# image with DOTNET_ROLL_FORWARD=LatestMajor; that rolls net8.0 onto ASP.NET Core 10 even
# when 8.0 is installed, and every HTTP-hosting test then fails in a way that reads as a
# product bug. Docker caches the build, so only the first call pays for it.
$image = & (Join-Path $PSScriptRoot "ensure-devtest-image.ps1")

Write-Host "== container test: $sha ($Framework) filter='$Filter' =="

docker run --rm --user root `
    -v "${repoRoot}:/src-mirror:ro" `
    -v ashlar-nuget-packages:/root/.nuget/packages `
    $image `
    bash -lc "set -e; git config --global safe.directory '*'; git clone -q /src-mirror /repo; cd /repo; git checkout -q $sha; dotnet test '$Project' --framework '$Framework' --filter '$Filter' --nologo -v minimal"

exit $LASTEXITCODE
