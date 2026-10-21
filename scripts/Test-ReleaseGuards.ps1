[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ExpectedId,
    [Parameter(Mandatory)][string]$ManifestPath,
    [Parameter(Mandatory)][string]$PackagePath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = Split-Path $PSScriptRoot -Parent
$manifest = Get-Content -LiteralPath (Join-Path $repo $ManifestPath) -Raw | ConvertFrom-Json
$scratch = Join-Path $repo ('artifacts/release-guard-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratch | Out-Null
$package = [IO.Path]::GetFullPath($PackagePath, $repo)
Copy-Item -LiteralPath $package, "$package.sha256" -Destination $scratch
$publication = Join-Path $PSScriptRoot 'Publish-Release.ps1'
$environmentNames = @('GITHUB_REPOSITORY', 'GITHUB_EVENT_NAME', 'GITHUB_REF')
$previous = @{}
foreach ($name in $environmentNames) { $previous[$name] = [Environment]::GetEnvironmentVariable($name) }

function Verify { & $publication -ExpectedId $ExpectedId -ManifestPath $ManifestPath -ArtifactDirectory $scratch -ValidateOnly }
function Reject([string]$Message) {
    $rejected = $false
    try { Verify } catch { $rejected = $true }
    if (!$rejected) { throw "Expected rejection: $Message" }
    Write-Host "PASS release guard: $Message"
}

try {
    $env:GITHUB_REPOSITORY = if ($ExpectedId -ceq 'sou-basic-react') { 'getsOU/HUD-React' } else { 'getsOU/HUD-WASM' }
    $env:GITHUB_EVENT_NAME = 'push'
    $env:GITHUB_REF = "refs/tags/v$($manifest.version)"
    Verify
    Write-Host 'PASS matching source, tag, manifest and package hash (no publication)'
    $expectedRepository = $env:GITHUB_REPOSITORY
    $env:GITHUB_REPOSITORY = 'example/untrusted-fork'
    Reject 'foreign repository'
    $env:GITHUB_REPOSITORY = $expectedRepository
    $env:GITHUB_EVENT_NAME = 'pull_request'
    Reject 'pull request'
    $env:GITHUB_EVENT_NAME = 'push'
    $env:GITHUB_REF = 'refs/heads/main'
    Reject 'branch instead of version tag'
    $env:GITHUB_REF = 'refs/tags/v999.0.0'
    Reject 'tag differs from source version'
    $env:GITHUB_REF = "refs/tags/v$($manifest.version)"
    $checksum = Join-Path $scratch ([IO.Path]::GetFileName($package) + '.sha256')
    [IO.File]::WriteAllText($checksum, 'tampered-checksum')
    Reject 'changed package checksum'
    Copy-Item -LiteralPath "$package.sha256" -Destination $checksum
    [IO.File]::WriteAllText((Join-Path $scratch 'unexpected.txt'), 'test-only extra artifact')
    Reject 'unexpected release file'
}
finally {
    foreach ($name in $environmentNames) { [Environment]::SetEnvironmentVariable($name, $previous[$name]) }
}
