# build.ps1 — assembles the payload, compiles the Material Design 3 Expressive wizard
# installer with Windows inbox .NET Framework csc.exe, AND packages the unified release ZIP
# containing both the graphical .exe installer, the manual-install edition, and bilingual docs.
param(
    [string]$Configuration = "Release",
    [string]$Version = "1.5.0"
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

$shadow     = Join-Path $mos 'vfx\mos_阴影'
$shake      = Join-Path $mos '形变\mos_shake'
$warp       = Join-Path $mos '形变\mos_变形白点'
$blur       = Join-Path $mos '模糊\mos_模糊'
$ripple     = Join-Path $mos '形变\mos_液态\mos_涟漪'
$lightmix   = Join-Path $mos 'vfx\mos_灯光混合'
$rimlight   = Join-Path $mos 'vfx\mos_打光'
$lightsweep = Join-Path $mos 'vfx\mos_扫光'
$dissolve   = Join-Path $mos 'vfx\mos_消散'
$rain       = Join-Path $mos '形变\mos_液态\mos_雨滴'
$lens       = Join-Path $mos '形变\mos_液态\mos_透镜'

$variants = @('zh-zh', 'zh-en', 'en-zh', 'en-en')
$wasmEffects = @(
    @{ Root = $shadow; File = 'mos_shadow.wasm' },
    @{ Root = $shake;  File = 'mos_shake.wasm' },
    @{ Root = $warp;   File = 'ae_warp.wasm' },
    @{ Root = $blur;   File = 'mos_blur.wasm' },
    @{ Root = $ripple; File = 'mos_ripple.wasm' }
)

$helpers = @(
    @{ Src = (Join-Path $lightmix   'dist\effectcraft-helper-mos-lightmix.exe');   File = 'effectcraft-helper-mos-lightmix.exe' },
    @{ Src = (Join-Path $rimlight   'dist\effectcraft-helper-mos-rimlight.exe');   File = 'effectcraft-helper-mos-rimlight.exe' },
    @{ Src = (Join-Path $lightsweep 'dist\effectcraft-helper-mos-lightsweep.exe'); File = 'effectcraft-helper-mos-lightsweep.exe' },
    @{ Src = (Join-Path $dissolve   'dist\effectcraft-helper-mos-dissolve.exe');   File = 'effectcraft-helper-mos-dissolve.exe' },
    @{ Src = (Join-Path $rain       'dist\effectcraft-helper-mos-rain.exe');       File = 'effectcraft-helper-mos-rain.exe' },
    @{ Src = (Join-Path $lens       'dist\effectcraft-helper-mos-lens.exe');       File = 'effectcraft-helper-mos-lens.exe' }
)

foreach ($v in $variants) {
    foreach ($e in $wasmEffects) {
        $src = Join-Path $e.Root "dist\$v\$($e.File)"
        if (-not (Test-Path $src)) { Fail "missing plugin build: $src" }
    }
}
foreach ($h in $helpers) {
    if (-not (Test-Path $h.Src)) { Fail "missing helper build: $($h.Src)" }
}

$script = Join-Path $rain 'tools\mos-load-helpers.jsx'
if (-not (Test-Path $script)) { $script = Join-Path $lightmix 'tools\mos-load-helpers.jsx' }
if (-not (Test-Path $script)) { Fail "missing startup script: $script" }

$manualZh = Join-Path $suite 'docs\zh\安装说明.md'
$manualEn = Join-Path $suite 'docs\en\Install-Guide.md'
if (-not (Test-Path $manualZh)) { Fail "missing manual: $manualZh" }
if (-not (Test-Path $manualEn)) { Fail "missing manual: $manualEn" }

$logo = Join-Path $here 'assets\logo.png'
if (-not (Test-Path $logo)) { Fail "missing logo: $logo" }

Write-Host 'staging installer payload...'
foreach ($path in @($payload, $stage)) {
    if (Test-Path $path) { Remove-Item $path -Recurse -Force }
}
New-Item -ItemType Directory -Force -Path $payload | Out-Null

foreach ($v in $variants) {
    $dir = Join-Path $payload "plug-ins\$v"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    foreach ($e in $wasmEffects) {
        Copy-Item (Join-Path $e.Root "dist\$v\$($e.File)") (Join-Path $dir $e.File) -Force
    }
}

$helpersDir = Join-Path $payload 'helpers'
New-Item -ItemType Directory -Force -Path $helpersDir | Out-Null
foreach ($h in $helpers) {
    Copy-Item $h.Src (Join-Path $helpersDir $h.File) -Force
}

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
Write-Host ("payload {0:N0} bytes ({1} variants x {2} wasm + {3} helpers)" -f (Get-Item $zip).Length, $variants.Count, $wasmEffects.Count, $helpers.Count)

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

# ---------------- Unified Release ZIP (Installer .exe + Manual Edition + Bilingual Docs) ----------------

Write-Host 'packaging unified release archive (installer + manual edition + bilingual docs)...'
$bundleStage = Join-Path $stage "MOS-Suite-$version"
$bundleManual = Join-Path $bundleStage 'manual'
New-Item -ItemType Directory -Force -Path $bundleStage | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $bundleManual 'plug-ins') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $bundleManual 'helpers') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $bundleManual 'Scripts\Startup') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $bundleStage 'docs\zh') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $bundleStage 'docs\en') | Out-Null

# 1. Graphical installer .exe
Copy-Item $exe (Join-Path $bundleStage "MOS-Suite-Setup-$version.exe") -Force

# 2. Manual installation files
foreach ($v in $variants) {
    $vDir = Join-Path $bundleManual "plug-ins\$v"
    New-Item -ItemType Directory -Force -Path $vDir | Out-Null
    foreach ($e in $wasmEffects) {
        Copy-Item (Join-Path $e.Root "dist\$v\$($e.File)") (Join-Path $vDir $e.File) -Force
    }
}
foreach ($h in $helpers) {
    Copy-Item $h.Src (Join-Path $bundleManual "helpers\$($h.File)") -Force
}
Copy-Item $script (Join-Path $bundleManual 'Scripts\Startup\mos-load-helpers.jsx') -Force

# Language config templates for manual install
$cfgZh = @"
{
  "schema": 2,
  "suiteVersion": "$version",
  "namesLanguage": "system",
  "paramsLanguage": "zh",
  "namesResolved": "zh",
  "paramsResolved": "zh",
  "effectsLanguage": "zh"
}
"@
$cfgEn = @"
{
  "schema": 2,
  "suiteVersion": "$version",
  "namesLanguage": "en",
  "paramsLanguage": "en",
  "namesResolved": "en",
  "paramsResolved": "en",
  "effectsLanguage": "en"
}
"@
[System.IO.File]::WriteAllText((Join-Path $bundleManual 'mos-suite.zh.json'), $cfgZh, (New-Object System.Text.UTF8Encoding($false)))
[System.IO.File]::WriteAllText((Join-Path $bundleManual 'mos-suite.en.json'), $cfgEn, (New-Object System.Text.UTF8Encoding($false)))

# 3. Bilingual docs & top-level README
Copy-Item $manualZh (Join-Path $bundleStage 'docs\zh\安装说明.md') -Force
Copy-Item $manualEn (Join-Path $bundleStage 'docs\en\Install-Guide.md') -Force
$license = Join-Path $suite 'LICENSE'
if (Test-Path $license) { Copy-Item $license (Join-Path $bundleStage 'LICENSE') -Force }

$topReadme = @"
# MOS Suite v$version for EffectCraft (中文 / English)

---

## 中文说明

本压缩包同时提供**两种安装方式**，任选其一即可：

1. **图形化分步安装程序（推荐）**：直接双击运行 ``MOS-Suite-Setup-$version.exe``，按步骤选择语言、勾选效果并安装。
2. **手动安装版（无需运行安装程序）**：
   - 本项目为个人开源插件，未购买商业代码签名证书，部分杀毒软件或 Windows SmartScreen 可能对 ``.exe`` 安装程序产生误报。
   - **如不信任 ``.exe``，请直接打开 ``manual\`` 文件夹**，参照 ``docs\zh\安装说明.md`` 将文件手动复制到 ``%APPDATA%\EffectCraft\`` 即可。

### 包含的 11 款效果
- **MOS 形变**：``mos_抖动``、``mos_收缩（变形·白点）``
- **MOS 液态**：``mos_涟漪``、``mos_雨滴``、``mos_透镜``
- **MOS 视效**：``mos_阴影``、``mos_灯光混合``、``mos_打光``
- **MOS 效果**：``mos_扫光``、``mos_消散``
- **MOS 模糊**：``mos_模糊``

详细安装步骤与排错请查看：``docs\zh\安装说明.md``

---

## English Guide

This archive includes **two installation options** — use whichever you prefer:

1. **Graphical Step-by-Step Installer (Recommended)**: Run ``MOS-Suite-Setup-$version.exe`` to pick languages, choose effects, and install automatically.
2. **Manual Installation (No Installer ``.exe`` Required)**:
   - Because this is an independent open-source project without a paid commercial code-signing certificate, Windows SmartScreen or antivirus tools may flag the ``.exe`` installer as a false positive.
   - **If you prefer not to run the ``.exe``**, simply use the files inside ``manual\`` and follow ``docs\en\Install-Guide.md`` to copy them into ``%APPDATA%\EffectCraft\``.

### Included 11 Effects
- **MOS Deform**: ``mos_shake``, ``mos_收缩 (Warp)``
- **MOS Liquid**: ``mos_ripple``, ``mos_rain``, ``mos_lens``
- **MOS VFX**: ``mos_shadow``, ``mos_lightmix``, ``mos_rimlight``
- **MOS Effects**: ``mos_lightsweep``, ``mos_dissolve``
- **MOS Blur**: ``mos_blur``

Full installation & troubleshooting guide: ``docs\en\Install-Guide.md``
"@
[System.IO.File]::WriteAllText((Join-Path $bundleStage 'README_请先阅读.md'), $topReadme, (New-Object System.Text.UTF8Encoding($false)))

$releaseZip = Join-Path $release "MOS-Suite-$version.zip"
if (Test-Path $releaseZip) { Remove-Item $releaseZip -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory($bundleStage, $releaseZip, [System.IO.Compression.CompressionLevel]::Optimal, $true)
$zipInfo = Get-Item $releaseZip
Write-Host ("packaged {0} ({1:N0} bytes)" -f $zipInfo.Name, $zipInfo.Length) -ForegroundColor Green
