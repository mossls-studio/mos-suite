# build.ps1 — build all eleven effects, the M3 installer, and the unified release ZIP.
param(
    [switch]$SkipWasm,
    [switch]$SkipHelper
)

$ErrorActionPreference = 'Stop'
$suite = $PSScriptRoot
$mos = Split-Path -Parent $suite

$shake      = Join-Path $mos '形变\mos_shake'
$warp       = Join-Path $mos '形变\mos_变形白点'
$shadow     = Join-Path $mos 'vfx\mos_阴影'
$blur       = Join-Path $mos '模糊\mos_模糊'
$ripple     = Join-Path $mos '形变\mos_液态\mos_涟漪'
$lightmix   = Join-Path $mos 'vfx\mos_灯光混合'
$rimlight   = Join-Path $mos 'vfx\mos_打光'
$lightsweep = Join-Path $mos 'vfx\mos_扫光'
$dissolve   = Join-Path $mos 'vfx\mos_消散'
$rain       = Join-Path $mos '形变\mos_液态\mos_雨滴'
$lens       = Join-Path $mos '形变\mos_液态\mos_透镜'

function Require($path, $what) {
    if (-not (Test-Path $path)) { throw "$what not found: $path" }
}

if (-not $SkipWasm) {
    Require (Join-Path $shake 'build.js') 'mos_shake build.js'
    Write-Host 'building mos_shake (zh + en)...'
    Push-Location $shake; try { & node build.js; if ($LASTEXITCODE -ne 0) { throw 'mos_shake build failed' } } finally { Pop-Location }

    Write-Host 'building ae_warp (变形·白点)...'
    Push-Location $warp; try { & node build.js; if ($LASTEXITCODE -ne 0) { throw 'ae_warp build failed' } } finally { Pop-Location }

    Write-Host 'building mos_shadow (zh + en)...'
    Push-Location $shadow; try { & node build.js; if ($LASTEXITCODE -ne 0) { throw 'mos_shadow build failed' } } finally { Pop-Location }

    Write-Host 'building mos_blur (zh + en)...'
    Push-Location $blur; try { & node build.js; if ($LASTEXITCODE -ne 0) { throw 'mos_blur build failed' } } finally { Pop-Location }

    Write-Host 'building mos_ripple (zh + en)...'
    Push-Location $ripple; try { & node build.js; if ($LASTEXITCODE -ne 0) { throw 'mos_ripple build failed' } } finally { Pop-Location }
}

if (-not $SkipHelper) {
    $cargo = Get-Command cargo -ErrorAction SilentlyContinue
    if (-not $cargo -and (Test-Path 'D:\ruanjian\rust-env\activate.ps1')) {
        . 'D:\ruanjian\rust-env\activate.ps1'
        $cargo = Get-Command cargo -ErrorAction SilentlyContinue
    }
    if (-not $cargo) { throw 'cargo not found; install the Rust toolchain or pass -SkipHelper' }

    $helperDefs = @(
        @{ Name = 'Light Mix';   Root = $lightmix;   Exe = 'effectcraft-helper-mos-lightmix.exe';   Target = 'mos-lightmix-target' },
        @{ Name = 'Rim Light';   Root = $rimlight;   Exe = 'effectcraft-helper-mos-rimlight.exe';   Target = 'mos-rimlight-target' },
        @{ Name = 'Light Sweep'; Root = $lightsweep; Exe = 'effectcraft-helper-mos-lightsweep.exe'; Target = 'mos-lightsweep-target' },
        @{ Name = 'Dissolve';    Root = $dissolve;   Exe = 'effectcraft-helper-mos-dissolve.exe';   Target = 'mos-dissolve-target' },
        @{ Name = 'Rain';        Root = $rain;       Exe = 'effectcraft-helper-mos-rain.exe';       Target = 'mos-rain-target' },
        @{ Name = 'Lens';        Root = $lens;       Exe = 'effectcraft-helper-mos-lens.exe';       Target = 'mos-lens-target' }
    )
    foreach ($h in $helperDefs) {
        Write-Host "building the $($h.Name) helper..."
        $manifest = Join-Path $h.Root 'helper\Cargo.toml'
        Require $manifest "$($h.Name) Cargo.toml"
        $targetDir = Join-Path $env:TEMP $h.Target
        & cargo build --release --manifest-path $manifest --target-dir $targetDir
        if ($LASTEXITCODE -ne 0) { throw "$($h.Name) helper build failed" }
        $built = Join-Path $targetDir "release\$($h.Exe)"
        Require $built "built $($h.Name) helper"
        Copy-Item $built (Join-Path $h.Root "dist\$($h.Exe)") -Force
        Write-Host "helper -> $(Join-Path $h.Root "dist\$($h.Exe)")"
    }
}

Write-Host 'building the installer & unified release archive...'
& (Join-Path $suite 'installer\build.ps1')
if ($LASTEXITCODE -ne 0) { throw 'installer build failed' }

Write-Host ''
Get-ChildItem (Join-Path $suite 'release') | Where-Object { $_.Extension -in @('.exe', '.zip') } | ForEach-Object {
    Write-Host ("release: {0} ({1:N0} bytes)" -f $_.FullName, $_.Length)
}
