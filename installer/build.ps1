# build.ps1 — assembles the payload and compiles the Material 3 installer with the
# .NET Framework C# compiler that ships with Windows (no SDK, no runtime download).
#
# The payload carries every language combination of the four wasm effects
# (plug-ins/<names>-<params>/<effect>.wasm); the installer copies the one that
# matches the chosen "effect names" + "parameter language" settings.
param(
    [string]$Configuration = "Release",
    [string]$Version = "1.3.0"
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$suite = Split-Path -Parent $here
$mos = Split-Path -Parent $suite
$release = Join-Path $suite 'release'
$payload = Join-Path $here 'payload'
$stage = Join-Path $here 'stage'
$version = $Version

function Fail($message) {
    Write-Host "error: $message" -ForegroundColor Red
    exit 1
}

$shadow = Join-Path $mos 'vfx\mos_阴影'
$shake = Join-Path $mos '形变\mos_shake'
$shrink = Join-Path $mos '形变\mos_收缩'
$blur = Join-Path $mos '模糊\mos_模糊'
$lightmix = Join-Path $mos 'vfx\mos_灯光混合'

# language combinations produced by the four plugin build.js files
$variants = @('zh-zh', 'zh-en', 'en-zh', 'en-en')
$effects = @(
    @{ Root = $shadow; File = 'mos_shadow.wasm' },
    @{ Root = $shake;  File = 'mos_shake.wasm' },
    @{ Root = $shrink; File = 'ae_warp.wasm' },
    @{ Root = $blur;   File = 'mos_blur.wasm' }
)

foreach ($v in $variants) {
    foreach ($e in $effects) {
        $src = Join-Path $e.Root "dist\$v\$($e.File)"
        if (-not (Test-Path $src)) { Fail "missing plugin build: $src (run the plugin build first)" }
    }
}

$helper = Join-Path $lightmix 'dist\effectcraft-helper-mos-lightmix.exe'
if (-not (Test-Path $helper)) { Fail "missing helper build: $helper" }
$script = Join-Path $lightmix 'tools\mos-load-helpers.jsx'
if (-not (Test-Path $script)) { Fail "missing startup script: $script" }

$manualZh = Join-Path $suite 'docs\zh\安装说明.md'
$manualEn = Join-Path $suite 'docs\en\Install-Guide.md'
if (-not (Test-Path $manualZh)) { Fail "missing manual: $manualZh" }
if (-not (Test-Path $manualEn)) { Fail "missing manual: $manualEn" }

$logo = Join-Path $here 'assets\logo.png'
if (-not (Test-Path $logo)) { Fail "missing logo: $logo" }

Write-Host 'staging payload...'
foreach ($path in @($payload, $stage)) {
    if (Test-Path $path) { Remove-Item $path -Recurse -Force }
}
New-Item -ItemType Directory -Force -Path $payload | Out-Null
foreach ($v in $variants) {
    $dir = Join-Path $payload "plug-ins\$v"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    foreach ($e in $effects) {
        Copy-Item (Join-Path $e.Root "dist\$v\$($e.File)") (Join-Path $dir $e.File) -Force
    }
}
New-Item -ItemType Directory -Force -Path (Join-Path $payload 'helper') | Out-Null
Copy-Item $helper (Join-Path $payload 'helper\effectcraft-helper-mos-lightmix.exe') -Force
New-Item -ItemType Directory -Force -Path (Join-Path $payload 'scripts') | Out-Null
Copy-Item $script (Join-Path $payload 'scripts\mos-load-helpers.jsx') -Force
New-Item -ItemType Directory -Force -Path (Join-Path $payload 'manual\zh') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $payload 'manual\en') | Out-Null
Copy-Item $manualZh (Join-Path $payload 'manual\zh\安装说明.md') -Force
Copy-Item $manualEn (Join-Path $payload 'manual\en\Install-Guide.md') -Force

$zip = Join-Path $stage 'payload.zip'
New-Item -ItemType Directory -Force -Path $stage | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($payload, $zip, [System.IO.Compression.CompressionLevel]::Optimal, $false)
Write-Host ("payload {0:N0} bytes, {1} variants x {2} effects" -f (Get-Item $zip).Length, $variants.Count, $effects.Count)

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path $csc)) { Fail 'csc.exe (.NET Framework 4) not found' }

New-Item -ItemType Directory -Force -Path $release | Out-Null
$exe = Join-Path $release "MOS-Suite-Setup-$version.exe"
$sources = Get-ChildItem (Join-Path $here 'src') -Filter *.cs | ForEach-Object { $_.FullName }

$cscArgs = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/codepage:65001',
    "/win32manifest:$here\app.manifest",
    "/resource:$zip,Payload",
    "/resource:$logo,Logo"
)
$icon = Join-Path $here 'assets\logo.ico'
if (Test-Path $icon) { $cscArgs += "/win32icon:$icon" }
$cscArgs += @(
    '/r:System.dll',
    '/r:System.Drawing.dll',
    '/r:System.Windows.Forms.dll',
    '/r:System.IO.Compression.dll',
    '/r:System.IO.Compression.FileSystem.dll',
    "/out:$exe"
)
$cscArgs += $sources

Write-Host "compiling $exe ..."
& $csc @cscArgs
if ($LASTEXITCODE -ne 0) { Fail "csc failed with exit code $LASTEXITCODE" }
if (-not (Test-Path $exe)) { Fail 'installer was not produced' }

$info = Get-Item $exe
Write-Host ("built {0} ({1:N0} bytes)" -f $info.Name, $info.Length) -ForegroundColor Green
