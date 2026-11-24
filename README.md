# MOS 套件 / MOS Suite

EffectCraft 的 **11 个效果**，合并成一个套件（v1.5.0），在效果栏里分成五个分组：

| 分组 Group | 效果 Effect | ID | 类型 Type | 版本 Version | 源码 Source |
| --- | --- | --- | --- | --- | --- |
| MOS 形变 / MOS Deform | 抖动 Shake | `mos.shake` | WASM | 1.2.0 | <https://github.com/mossls-studio/mos-shake> |
| MOS 形变 / MOS Deform | 变形·白点 Warp | `mos.ae.warp` | WASM | 1.3.0 | <https://github.com/mossls-studio/mos-shrink> |
| MOS 液态 / MOS Liquid | 涟漪 Ripple | `mos.ripple` | WASM | 1.1.0 | <https://github.com/mossls-studio/mos-ripple> |
| MOS 液态 / MOS Liquid | 雨滴 Rain | `mos.rain` | 外部助手 Helper | 5.0.0 | <https://github.com/mossls-studio/mos-rain> |
| MOS 液态 / MOS Liquid | 透镜 Lens | `mos.lens` | 外部助手 Helper | 2.0.0 | <https://github.com/mossls-studio/mos-lens> |
| MOS 视效 / MOS VFX | 阴影 Shadow | `mos.shadow` | WASM | 0.6.2 | <https://github.com/mossls-studio/mos-shadow> |
| MOS 视效 / MOS VFX | 灯光混合 Light Mix | `mos.lightwrap` | 外部助手 Helper | 1.5.0 | <https://github.com/mossls-studio/mos-lightmix> |
| MOS 视效 / MOS VFX | 打光 Rim Light | `mos.rimlight` | 外部助手 Helper | 1.0.0 | 本地仓库 `vfx/mos_打光` |
| MOS 效果 / MOS Effects | 扫光 Light Sweep | `mos.lightsweep` | 外部助手 Helper | 1.2.0 | 本地仓库 `vfx/mos_扫光` |
| MOS 效果 / MOS Effects | 消散 Dissolve | `mos.dissolve` | 外部助手 Helper | 1.0.0 | 本地仓库 `vfx/mos_消散` |
| MOS 模糊 / MOS Blur | 模糊 Blur | `mos.blur` | WASM | 0.1.0 | 本地仓库 `模糊/mos_模糊` |

开源地址（AGPL-3.0-or-later，各自独立仓库，安装器、合集发布包与双语说明在套件仓库）：

| 仓库 Repository | 内容 Contents |
| --- | --- |
| <https://github.com/mossls-studio/mos-suite> | 套件：Material Design 3 Expressive 分步安装器、手动安装包整合与双语说明 |
| <https://github.com/mossls-studio/mos-ripple> | 涟漪 `mos.ripple`（WASM，API 2，波峰侧面高光与折射） |
| <https://github.com/mossls-studio/mos-rain> | 雨滴 `mos.rain`（Rust 外部助手，API 2，重力水滴 + 色散 + 自定义贴图图层） |
| <https://github.com/mossls-studio/mos-lens> | 透镜 `mos.lens`（Rust 外部助手，API 2，6 种形状 + 中心放大 + 镜内过渡羽化 + 自定义图层） |
| <https://github.com/mossls-studio/mos-shadow> | 阴影 `mos.shadow`（WASM，API 2，含源码 `src/mos_shadow.wat`） |
| <https://github.com/mossls-studio/mos-shake> | 抖动 `mos.shake`（WASM，API 1） |
| <https://github.com/mossls-studio/mos-shrink> | 变形·白点 `mos.ae.warp`（WASM，API 1，源码 `ae_warp/`） |
| <https://github.com/mossls-studio/mos-lightmix> | 灯光混合 `mos.lightwrap`（Rust 外部助手，含 `helper/src/*.rs`） |
| 打光 `mos.rimlight` | 本地仓库 `vfx/mos_打光`（Rust 外部助手，2D 偏移边缘打光 + 拼贴 + 渐变叠加 + 独立发光） |
| 扫光 `mos.lightsweep` | 本地仓库 `vfx/mos_扫光`（Rust 外部助手，直线/径向扫光 + 独立轮廓高光 + 高速发光） |
| 消散 `mos.dissolve` | 本地仓库 `vfx/mos_消散`（Rust 外部助手，粒子生成/像素分离 + 线性/杂色/自定义遮罩 + 风场湍流） |
| 模糊 `mos.blur` | 本地仓库 `模糊/mos_模糊`（Rust → WASM） |

5 个 WASM 插件各带 4 份语言构建（名称语言 × 参数语言：`zh-zh`、`zh-en`、`en-zh`、`en-en`），其中变形·白点只有中文清单；6 个外部助手（雨滴、透镜、灯光混合、打光、扫光、消散）为原生可执行文件，运行时读取 `mos-suite.json` 中的设置分别本地化分组/效果名与参数控件语言。

## 安装器界面截图（Material Design 3 Expressive 分步向导）

| 1. 欢迎 Welcome | 2. 语言设置 Language |
| --- | --- |
| ![欢迎](docs/screenshots/setup-welcome.png) | ![语言设置](docs/screenshots/setup-language.png) |
| **3. 选择效果 Effects** | **4. 安装位置与摘要 Location** |
| ![选择效果](docs/screenshots/setup-effects.png) | ![安装位置](docs/screenshots/setup-location.png) |

| English UI (switchable in-app via bottom-left `中文 / EN`) |
| --- |
| ![English UI](docs/screenshots/setup-en.png) |

## 目录

```text
mos/
  suite/                    本目录：Material 3 分步安装器、双语文档与统一发布包
    build.ps1               一键编译全部 11 个效果、安装器与 release ZIP
    installer/              安装器源码（C# / .NET Framework 4，60fps 弹簧动效引擎，无 SDK 依赖）
      assets/               顶栏 logo（png + ico）
      src/                  Motion.cs、Theme.cs、Widgets.cs、InstallerForm.cs、InstallerCore.cs 等
    docs/zh, docs/en        中英双语手动安装说明与界面截图
    release/                发布产物：MOS-Suite-1.5.0.zip（含 Setup .exe + manual/ 手动版 + 文档）与独立 .exe
  vfx/mos_阴影/             mos.shadow（WASM，API 2）
  vfx/mos_灯光混合/         mos.lightwrap（Rust 外部助手）
  vfx/mos_打光/             mos.rimlight（Rust 外部助手）
  vfx/mos_扫光/             mos.lightsweep（Rust 外部助手）
  vfx/mos_消散/             mos.dissolve（Rust 外部助手）
  形变/mos_shake/           mos.shake（WASM，API 1）
  形变/mos_变形白点/         mos.ae.warp（WASM，API 1）
  形变/mos_液态/            mos_涟漪（WASM）、mos_雨滴（Rust 外部助手）、mos_透镜（Rust 外部助手）
  模糊/mos_模糊/            mos.blur（WASM，API 2）
```

## 构建

前提：Windows、Node.js（各 WASM 插件自带 `wabt` 依赖）、Rust 工具链（6 个外部助手与模糊需要）、.NET Framework 4（Windows 自带 `csc.exe`，不需要 Visual Studio 或 .NET SDK）。

```powershell
# 推荐：从 suite 根目录一键编译 11 个效果、安装器 exe 与含手动安装版的 ZIP 发布包
powershell -ExecutionPolicy Bypass -File suite\build.ps1

# 如插件产物已就绪，仅重新编译安装器与打包发布 ZIP
powershell -ExecutionPolicy Bypass -File suite\installer\build.ps1
```

发布产物输出到 `suite\release\`：
- `MOS-Suite-1.5.0.zip`（包含 `MOS-Suite-Setup-1.5.0.exe` + `manual/` 手动安装目录 + 双语 `docs/` 说明）
- `MOS-Suite-Setup-1.5.0.exe`（独立单文件安装程序）

## 安装与卸载

提供两种安装方式：
1. **图形化安装器**：双击 `MOS-Suite-Setup-1.5.0.exe`，按左侧步骤完成语言配置、效果勾选与位置确认（界面左下角可随时切换 `中文 / EN`）。
2. **手动安装（无需运行 `.exe`）**：打开压缩包内 `manual/` 目录，参照 [docs/zh/安装说明.md](docs/zh/安装说明.md) 或 [docs/en/Install-Guide.md](docs/en/Install-Guide.md) 将文件复制到 `%APPDATA%\EffectCraft\`。

静默安装 / 卸载参数：

```powershell
MOS-Suite-Setup-1.5.0.exe /S /names=en /params=zh /dir="%APPDATA%\EffectCraft"
MOS-Suite-Setup-1.5.0.exe /S /uninstall
```

- `/names=` `system` | `en`，`/params=` `system` | `zh` | `en`；旧参数 `/effects=` 仍可用（同时设置两项）。
- 日志写到 `%TEMP%\mos-suite-setup.log`。

## 语言设置格式（`mos-suite.json`）

- 效果名称：分组名、效果名、描述由 `namesLanguage`（`system` / `en`）决定；选 `system` 时非中文系统回退英文。
- 参数语言：参数名与下拉选项由 `paramsLanguage`（`system` / `zh` / `en`）决定；没有对应构建时回退英文。
- 安装器会把设置与解析后的实际语言（`namesResolved` / `paramsResolved`）写入 `%APPDATA%\EffectCraft\mos-suite.json`（`schema: 2`），六个外部助手读取这两个字段完成双语本地化。

## 许可

AGPL-3.0-or-later（GNU Affero 通用公共许可证第 3 版或更高版本），见 `LICENSE`。

---

# MOS Suite (English)

Eleven EffectCraft effects bundled into one suite (`v1.5.0`) across five Effects-panel groups (see tables above). Seven effects are open-source under **AGPL-3.0-or-later** in their own repositories ([mos-shake](https://github.com/mossls-studio/mos-shake), [mos-shrink](https://github.com/mossls-studio/mos-shrink), [mos-ripple](https://github.com/mossls-studio/mos-ripple), [mos-rain](https://github.com/mossls-studio/mos-rain), [mos-lens](https://github.com/mossls-studio/mos-lens), [mos-shadow](https://github.com/mossls-studio/mos-shadow), and [mos-lightmix](https://github.com/mossls-studio/mos-lightmix)), while installer sources, unified release bundles, and bilingual manuals live in [mos-suite](https://github.com/mossls-studio/mos-suite).

The five WASM plug-ins ship in four `names × parameters` combinations (`zh-zh`, `zh-en`, `en-zh`, `en-en`; Warp ships a single Chinese manifest). The six native External Effect Helpers (`mos_rain`, `mos_lens`, `mos_lightmix`, `mos_rimlight`, `mos_lightsweep`, `mos_dissolve`) read `namesResolved` / `paramsResolved` from `%APPDATA%\EffectCraft\mos-suite.json` (`schema: 2`) to localize effect names and parameter controls independently.

Build everything from the workspace root with `powershell -ExecutionPolicy Bypass -File suite\build.ps1`, or repackage only the installer and unified release ZIP (`MOS-Suite-1.5.0.zip`) with `suite\installer\build.ps1`. The Material Design 3 Expressive step-by-step installer embeds all binaries cleanly, supports instant in-app `中文 / EN` UI switching, and the release ZIP additionally includes a ready-to-copy `manual/` folder + bilingual guides for users who prefer manual installation without running `.exe`.

Silent install/uninstall: `/S /names=en /params=zh /dir="..."` and `/S /uninstall` (log at `%TEMP%\mos-suite-setup.log`). Distributed under **AGPL-3.0-or-later**; see `LICENSE`.
