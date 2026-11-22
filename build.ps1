# build.ps1 — build all five effects and the installer.
param(
    [switch]$SkipWasm,
    [switch]$SkipHelper
)

$ErrorActionPreference = 'Stop'
$suite = $PSScriptRoot
$mos = Split-Path -Parent $suite

$shake = Join-Path $mos '形变\mos_shake'
$shrink = Join-Path $mos '形变\mos_收缩'
$shadow = Join-Path $mos 'vfx\mos_阴影'
$blur = Join-Path $mos '模糊\mos_模糊'
$lightmix = Join-Path $mos 'vfx\mos_灯光混合'

function Require($path, $what) {
    if (-not (Test-Path $path)) { throw "$what not found: $path" }
}

if (-not $SkipWasm) {
    Require (Join-Path $shake 'build.js') 'mos_shake build.js'
    Write-Host 'building mos_shake (zh + en)...'
    Push-Location $shake; try { & node build.js; if ($LASTEXITCODE -ne 0) { throw 'mos_shake build failed' } } finally { Pop-Location }

    Write-Host 'building ae_warp (变形·白点)...'
    Push-Location $shrink; try { & node build_warp.js; if ($LASTEXITCODE -ne 0) { throw 'ae_warp build failed' } } finally { Pop-Location }

    Write-Host 'building mos_shadow (zh + en)...'
    Push-Location $shadow; try { & node build.js; if ($LASTEXITCODE -ne 0) { throw 'mos_shadow build failed' } } finally { Pop-Location }

    Write-Host 'building mos_blur (zh + en)...'
    Push-Location $blur; try { & node build.js; if ($LASTEXITCODE -ne 0) { throw 'mos_blur build failed' } } finally { Pop-Location }
}

if (-not $SkipHelper) {
    $cargo = Get-Command cargo -ErrorAction SilentlyContinue
    if (-not $cargo -and (Test-Path 'D:\ruanjian\rust-env\activate.ps1')) {
        . 'D:\ruanjian\rust-env\activate.ps1'
        $cargo = Get-Command cargo -ErrorAction SilentlyContinue
    }
    if (-not $cargo) { throw 'cargo not found; install the Rust toolchain or pass -SkipHelper' }

    Write-Host 'building the Light Mix helper...'
    $manifest = Join-Path $lightmix 'helper\Cargo.toml'
    Require $manifest 'Light Mix Cargo.toml'
    # MinGW's ld cannot read dependency paths containing non-ASCII characters,
    # so the helper builds into a temp target directory.
    $targetDir = Join-Path $env:TEMP 'mos-lightmix-target'
    & cargo build --release --manifest-path $manifest --target-dir $targetDir
    if ($LASTEXITCODE -ne 0) { throw 'Light Mix helper build failed' }
    $built = Join-Path $targetDir 'release\effectcraft-helper-mos-lightmix.exe'
    Require $built 'built helper'
    Copy-Item $built (Join-Path $lightmix 'dist\effectcraft-helper-mos-lightmix.exe') -Force
    Write-Host "helper -> $(Join-Path $lightmix 'dist\effectcraft-helper-mos-lightmix.exe')"
}

Write-Host 'building the installer...'
& (Join-Path $suite 'installer\build.ps1')
if ($LASTEXITCODE -ne 0) { throw 'installer build failed' }

Write-Host ''
Write-Host (Get-ChildItem (Join-Path $suite 'release') -Filter *.exe | ForEach-Object { "release: $($_.FullName) ($($_.Length) bytes)" })
