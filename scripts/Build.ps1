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
    & (Join-Path $PSScriptRoot 'Generate-IconLayout.ps1') -Check
    Invoke-Dotnet @('restore', 'src/BasicHud/BasicHud.csproj', '--configfile', 'NuGet.Config', '--locked-mode')
    Invoke-Dotnet @('restore', 'tests/BasicHud.Tests/BasicHud.Tests.csproj', '--configfile', 'NuGet.Config', '--locked-mode')
    Invoke-Dotnet @('tool', 'restore', '--configfile', 'NuGet.Config')
    Invoke-Dotnet @('format', 'SOU.BasicHud.slnx', 'whitespace', '--no-restore', '--verify-no-changes', '--verbosity', 'minimal')
    Invoke-Dotnet @('run', '--project', 'tests/BasicHud.Tests/BasicHud.Tests.csproj', '-c', 'Release', '--no-restore')
    Invoke-Dotnet @('publish', 'src/BasicHud/BasicHud.csproj', '-c', 'Release', '--no-restore', '-o', $package)
    Invoke-Dotnet @('tool', 'run', 'sou-hud', '--', 'pack', $package, $archive)
    Invoke-Dotnet @('tool', 'run', 'sou-hud', '--', 'validate', $archive)
    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText("$archive.sha256", "$hash  $([IO.Path]::GetFileName($archive))`n", [Text.UTF8Encoding]::new($false))
    Write-Host "Package: $archive"
    Write-Host "SHA256: $hash"
}
finally { Pop-Location }
