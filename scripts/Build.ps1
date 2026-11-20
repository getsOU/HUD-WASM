[CmdletBinding()]
param([string]$OutputDirectory = '')

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $repo 'artifacts' }
$output = [IO.Path]::GetFullPath($OutputDirectory, $repo)
$manifest = Get-Content -LiteralPath (Join-Path $repo 'src/BasicHud/hud.json') -Raw | ConvertFrom-Json
$archive = Join-Path $output "$($manifest.id)-$($manifest.version).souhud"
$package = Join-Path $output 'package'
if ((Test-Path -LiteralPath $archive) -or (Test-Path -LiteralPath $package)) {
    throw 'Output already exists. Choose a fresh -OutputDirectory; packages are not overwritten.'
}
New-Item -ItemType Directory -Path $output -Force | Out-Null

function Invoke-Dotnet([string[]]$Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments[0]) failed ($LASTEXITCODE)." }
}

Push-Location $repo
try {
    & npm ci --ignore-scripts --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw 'Style dependency restore failed.' }
    & (Join-Path $PSScriptRoot 'Generate-IconLayout.ps1') -Check
    & npm run styles:check
    if ($LASTEXITCODE -ne 0) { throw 'Compiled styles do not match SCSS sources.' }
    Invoke-Dotnet @('restore', 'src/BasicHud/BasicHud.csproj', '--configfile', 'NuGet.Config', '--locked-mode')
    Invoke-Dotnet @('restore', 'tests/BasicHud.Tests/BasicHud.Tests.csproj', '--configfile', 'NuGet.Config', '--locked-mode')
    # A private preview used the same version. Avoid the per-user tool resolver,
    # which can retain that older executable even after restoring public packages.
    $sdkVersion = (Get-Content -LiteralPath '.config/dotnet-tools.json' -Raw | ConvertFrom-Json).tools.'sou.hud.tool'.version
    $toolDirectory = Join-Path $repo 'artifacts/published-sdk-tools'
    $sdkTool = Join-Path $toolDirectory ".store/sou.hud.tool/$sdkVersion/sou.hud.tool/$sdkVersion/tools/net10.0/any/SOU.Hud.Tool.dll"
    if (!(Test-Path -LiteralPath $sdkTool)) {
        Invoke-Dotnet @('tool', 'install', 'SOU.Hud.Tool', '--version', $sdkVersion, '--configfile', 'NuGet.Config', '--tool-path', $toolDirectory)
    }
    Invoke-Dotnet @('format', 'SOU.BasicHud.slnx', 'whitespace', '--no-restore', '--verify-no-changes', '--verbosity', 'minimal')
    Invoke-Dotnet @('run', '--project', 'tests/BasicHud.Tests/BasicHud.Tests.csproj', '-c', 'Release', '--no-restore')
    Invoke-Dotnet @('publish', 'src/BasicHud/BasicHud.csproj', '-c', 'Release', '--no-restore', '-o', $package)
    $runtimeVersion = (Get-Content -LiteralPath (Join-Path $package 'wwwroot/sou-hud-sdk.version') -Raw).Trim()
    if ($runtimeVersion -cne $sdkVersion -or $manifest.sdkVersion -cne $sdkVersion) { throw 'SDK tool, published runtime and HUD manifest versions must match.' }
    Invoke-Dotnet @($sdkTool, 'pack', $package, $archive)
    Invoke-Dotnet @($sdkTool, 'validate', $archive)
    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText("$archive.sha256", "$hash  $([IO.Path]::GetFileName($archive))`n", [Text.UTF8Encoding]::new($false))
    Write-Host "Package: $archive"
    Write-Host "SHA256: $hash"
}
finally { Pop-Location }
