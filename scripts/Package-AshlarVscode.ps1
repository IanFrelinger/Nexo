# Package Ashlar VS Code extension as a .vsix (no npm/vsce required — plain JS zip).
# Output: extensions/ashlar-vscode/ashlar-<version>.vsix
param(
    [string]$OutDir = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$extDir = Join-Path $repoRoot "extensions\ashlar-vscode"
if (-not (Test-Path (Join-Path $extDir "package.json"))) {
    throw "Extension not found at $extDir"
}

$pkg = Get-Content (Join-Path $extDir "package.json") -Raw | ConvertFrom-Json
$name = $pkg.name
$version = $pkg.version
$publisher = if ($pkg.publisher) { $pkg.publisher } else { "ashlar" }
$displayName = if ($pkg.displayName) { $pkg.displayName } else { $name }
$description = if ($pkg.description) { $pkg.description } else { "" }
$engines = $pkg.engines.vscode
if (-not $engines) { $engines = "^1.85.0" }

$staging = Join-Path $env:TEMP ("ashlar-vsix-" + [guid]::NewGuid().ToString("N"))
$extensionRoot = Join-Path $staging "extension"
New-Item -ItemType Directory -Force -Path $extensionRoot | Out-Null

# Copy extension files (exclude prior VSIX artifacts)
$exclude = @("*.vsix", ".vscode", "node_modules")
Get-ChildItem -Path $extDir -Force | Where-Object {
    $n = $_.Name
    -not ($n -like "*.vsix") -and $n -ne ".vscode" -and $n -ne "node_modules"
} | ForEach-Object {
    $dest = Join-Path $extensionRoot $_.Name
    if ($_.PSIsContainer) {
        Copy-Item -Recurse -Force $_.FullName $dest
    } else {
        Copy-Item -Force $_.FullName $dest
    }
}

$manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<PackageManifest Version="2.0.0" xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011" xmlns:d="http://schemas.microsoft.com/developer/vsx-schema-design/2011">
  <Metadata>
    <Identity Language="en-US" Id="$name" Version="$version" Publisher="$publisher" />
    <DisplayName>$displayName</DisplayName>
    <Description xml:space="preserve">$([System.Security.SecurityElement]::Escape($description))</Description>
    <Tags>ai,chat,ashlar</Tags>
    <Categories>Other</Categories>
    <Properties>
      <Property Id="Microsoft.VisualStudio.Code.Engine" Value="$engines" />
      <Property Id="Microsoft.VisualStudio.Code.ExtensionDependencies" Value="" />
      <Property Id="Microsoft.VisualStudio.Code.ExtensionPack" Value="" />
      <Property Id="Microsoft.VisualStudio.Code.ExtensionKind" Value="workspace" />
      <Property Id="Microsoft.VisualStudio.Code.LocalizedLanguages" Value="" />
    </Properties>
  </Metadata>
  <Installation>
    <InstallationTarget Id="Microsoft.VisualStudio.Code"/>
  </Installation>
  <Dependencies/>
  <Assets>
    <Asset Type="Microsoft.VisualStudio.Code.Manifest" Path="extension/package.json" Addressable="true" />
  </Assets>
</PackageManifest>
"@

$contentTypes = @"
<?xml version="1.0" encoding="utf-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension=".json" ContentType="application/json"/>
  <Default Extension=".vsixmanifest" ContentType="text/xml"/>
  <Default Extension=".js" ContentType="application/javascript"/>
  <Default Extension=".md" ContentType="text/markdown"/>
  <Default Extension=".svg" ContentType="image/svg+xml"/>
  <Default Extension=".txt" ContentType="text/plain"/>
</Types>
"@

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText((Join-Path $staging "extension.vsixmanifest"), $manifest, $utf8NoBom)
[System.IO.File]::WriteAllText((Join-Path $staging "[Content_Types].xml"), $contentTypes, $utf8NoBom)

$vsixName = "$name-$version.vsix"
$vsixPath = if ($OutDir) {
    New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
    Join-Path $OutDir $vsixName
} else {
    Join-Path $extDir $vsixName
}

if (Test-Path $vsixPath) { Remove-Item -Force $vsixPath }

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($staging, $vsixPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)

Remove-Item -Recurse -Force $staging
Write-Host "Wrote $vsixPath"
