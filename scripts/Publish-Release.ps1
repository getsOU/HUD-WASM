[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('sou-basic-react', 'sou-basic-wasm')][string]$ExpectedId,
    [Parameter(Mandatory)][string]$ManifestPath,
    [string]$ArtifactDirectory = 'release-artifacts',
    [switch]$ValidateOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = Split-Path $PSScriptRoot -Parent
$manifest = Get-Content -LiteralPath (Join-Path $repo $ManifestPath) -Raw | ConvertFrom-Json
$expectedRepository = if ($ExpectedId -ceq 'sou-basic-react') { 'getsOU/HUD-React' } else { 'getsOU/HUD-WASM' }
$expectedRuntime = if ($ExpectedId -ceq 'sou-basic-react') { 'web' } else { 'blazor-wasm' }
$version = [string]$manifest.version

if ($env:GITHUB_REPOSITORY -cne $expectedRepository -or $env:GITHUB_EVENT_NAME -cne 'push' -or
    $version -cnotmatch '\A[0-9]+\.[0-9]+\.[0-9]+\z' -or $env:GITHUB_REF -cne "refs/tags/v$version") {
    throw 'Publication requires this repository and an existing version tag matching hud.json.'
}
if ($manifest.id -cne $ExpectedId -or $manifest.runtime -cne $expectedRuntime -or $manifest.apiVersion -ne 3) {
    throw 'Unexpected HUD identity, runtime or protocol.'
}

$directory = [IO.Path]::GetFullPath($ArtifactDirectory, $repo)
$filename = "$ExpectedId-$version.souhud"
$archive = Join-Path $directory $filename
$checksum = "$archive.sha256"
$files = @(Get-ChildItem -LiteralPath $directory -File -Recurse)
if ($files.Count -ne 2 -or !(Test-Path -LiteralPath $archive -PathType Leaf) -or
    !(Test-Path -LiteralPath $checksum -PathType Leaf)) {
    throw 'Expected exactly the built HUD package and its checksum.'
}
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
if ((Get-Content -LiteralPath $checksum -Raw).TrimEnd("`r", "`n") -cne "$hash  $filename") {
    throw 'Package checksum does not match the build artifact.'
}

Add-Type -AssemblyName System.IO.Compression
$zip = [IO.Compression.ZipFile]::OpenRead($archive)
try {
    $entry = @($zip.Entries | Where-Object FullName -CEQ 'hud.json')
    if ($entry.Count -ne 1) { throw 'Expected a single package manifest.' }
    $reader = [IO.StreamReader]::new($entry[0].Open())
    try { $packaged = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    if ($packaged.id -cne $ExpectedId -or $packaged.version -cne $version -or
        $packaged.runtime -cne $expectedRuntime -or $packaged.apiVersion -ne 3) {
        throw 'Packaged manifest does not match the tagged source.'
    }
}
finally { $zip.Dispose() }

Write-Host "Verified $filename ($hash)."
if ($ValidateOnly) { return }

# GITHUB_TOKEN is supplied only to the release step; never persist or print it.
$notes = Join-Path $PSScriptRoot 'release-notes.md'
$title = "sOU Default HUD $version"
& gh release create "v$version" $archive $checksum --repo $expectedRepository --verify-tag `
    --title $title --notes-file $notes
if ($LASTEXITCODE -ne 0) { throw 'GitHub release creation failed; existing releases are never overwritten.' }
