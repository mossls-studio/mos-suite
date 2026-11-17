# build.ps1 — assembles the payload and compiles the Material 3 installer with the
# .NET Framework C# compiler that ships with Windows (no SDK, no runtime download).
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$suite = Split-Path -Parent $here
$mos = Split-Path -Parent $suite
$release = Join-Path $suite 'release'
$payload = Join-Path $here 'payload'
$stage = Join-Path $here 'stage'
$version = '1.0.0'

function Fail($message) {
    Write-Host "error: $message" -ForegroundColor Red
    exit 1
}

$shadow = Join-Path $mos 'vfx\mos_阴影'
$shake = Join-Path $mos '形变\mos_shake'
$shrink = Join-Path $mos '形变\mos_收缩'
$lightmix = Join-Path $mos 'vfx\mos_灯光混合'

$wasm = @(
    @{ Src = Join-Path $shadow 'dist\zh\mos_shadow.wasm'; Lang = 'zh'; Name = 'mos_shadow.wasm' },
    @{ Src = Join-Path $shadow 'dist\en\mos_shadow.wasm'; Lang = 'en'; Name = 'mos_shadow.wasm' },
    @{ Src = Join-Path $shake 'dist\zh\mos_shake.wasm'; Lang = 'zh'; Name = 'mos_shake.wasm' },
    @{ Src = Join-Path $shake 'dist\en\mos_shake.wasm'; Lang = 'en'; Name = 'mos_shake.wasm' },
    @{ Src = Join-Path $shrink 'dist\zh\mos_shrink.wasm'; Lang = 'zh'; Name = 'mos_shrink.wasm' },
    @{ Src = Join-Path $shrink 'dist\en\mos_shrink.wasm'; Lang = 'en'; Name = 'mos_shrink.wasm' }
)

foreach ($w in $wasm) {
    if (-not (Test-Path $w.Src)) { Fail "missing plugin build: $($w.Src) (run the plugin build first)" }
}

$helper = Join-Path $lightmix 'dist\effectcraft-helper-mos-lightmix.exe'
if (-not (Test-Path $helper)) { Fail "missing helper build: $helper" }
$script = Join-Path $lightmix 'tools\mos-load-helpers.jsx'
if (-not (Test-Path $script)) { Fail "missing startup script: $script" }

$manualZh = Join-Path $suite 'docs\zh\安装说明.md'
$manualEn = Join-Path $suite 'docs\en\Install-Guide.md'
if (-not (Test-Path $manualZh)) { Fail "missing manual: $manualZh" }
if (-not (Test-Path $manualEn)) { Fail "missing manual: $manualEn" }

Write-Host 'staging payload...'
foreach ($path in @($payload, $stage)) {
    if (Test-Path $path) { Remove-Item $path -Recurse -Force }
}
New-Item -ItemType Directory -Force -Path $payload | Out-Null
foreach ($w in $wasm) {
    $dir = Join-Path $payload "plug-ins\$($w.Lang)"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    Copy-Item $w.Src (Join-Path $dir $w.Name) -Force
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
