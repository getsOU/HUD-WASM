[CmdletBinding()]
param([switch]$Check)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$document = Get-Content -LiteralPath (Join-Path $repo 'docs/cs2-icon-geometry.json') -Raw | ConvertFrom-Json
if ($document.schemaVersion -ne 1) { throw 'Unsupported icon geometry schema.' }
$target = if (Test-Path -LiteralPath (Join-Path $repo 'src/BasicHud')) { 'src/BasicHud/wwwroot/icon-layout.css' } else { 'src/ui/icon-layout.css' }
$lines = [Collections.Generic.List[string]]::new()
$lines.Add('/* Derived from docs/cs2-icon-geometry.json; sizing preserves shipped SVG files. */')
function Number([double]$Value) { $Value.ToString('0.#######', [Globalization.CultureInfo]::InvariantCulture) }
foreach ($icon in $document.icons) {
    if ($icon.PSObject.Properties['empty'] -and $icon.empty) { continue }
    if ($icon.key -cnotmatch '^(weapons|ui|killfeed)/[a-z0-9_]+$') { throw 'Invalid icon key.' }
    $left = [Math]::Max(0, $icon.x - .02 * $icon.sourceHeight / $icon.sourceWidth)
    $top = [Math]::Max(0, $icon.y - .02)
    $width = [Math]::Min(1, $icon.x + $icon.width + .02 * $icon.sourceHeight / $icon.sourceWidth) - $left
    $height = [Math]::Min(1, $icon.y + $icon.height + .02) - $top
    if ($width -le 0 -or $height -le 0) { throw "Empty bounds: $($icon.key)" }
    $lines.Add(('.cs2-icon[data-icon="{0}"] {{ --icon-aspect: {1}; --icon-image-width: {2}%; --icon-image-height: {3}%; --icon-image-left: {4}%; --icon-image-top: {5}%; }}' -f $icon.key, (Number ($icon.sourceWidth * $width / ($icon.sourceHeight * $height))), (Number (100 / $width)), (Number (100 / $height)), (Number (-$left / $width * 100)), (Number (-$top / $height * 100))))
}
$expected = ($lines -join "`n") + "`n"
$path = Join-Path $repo $target
if ($Check) {
    if ([IO.File]::ReadAllText($path).Replace("`r`n", "`n") -cne $expected) { throw 'Icon layout is stale; run scripts/Generate-IconLayout.ps1.' }
    Write-Host 'Original-icon optical layout is current.'
} else {
    [IO.File]::WriteAllText($path, $expected, [Text.UTF8Encoding]::new($false))
}
