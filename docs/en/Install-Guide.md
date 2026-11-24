# MOS Suite v1.5.0 · Installation & User Guide

Built for **EffectCraft**. The suite ships **11 effects** organized into five Effects-panel groups:

| Group | Effect | Effect ID | Type | Version |
| --- | --- | --- | --- | --- |
| MOS Deform | mos_shake | `mos.shake` | WASM | 1.2.0 |
| MOS Deform | mos_收缩 (Warp) | `mos.ae.warp` | WASM | 1.3.0 |
| MOS Liquid | mos_ripple | `mos.ripple` | WASM | 1.1.0 |
| MOS Liquid | mos_rain (custom layer maps) | `mos.rain` | Helper | 5.0.0 |
| MOS Liquid | mos_lens (6 shapes + custom layer) | `mos.lens` | Helper | 2.0.0 |
| MOS VFX | mos_shadow | `mos.shadow` | WASM | 0.6.2 |
| MOS VFX | mos_lightmix | `mos.lightwrap` | Helper | 1.5.0 |
| MOS VFX | mos_rimlight (2D offset rim light) | `mos.rimlight` | Helper | 1.0.0 |
| MOS Effects | mos_lightsweep (sweep + contour & glow) | `mos.lightsweep` | Helper | 1.2.0 |
| MOS Effects | mos_dissolve (particle & pixel dissolve) | `mos.dissolve` | Helper | 1.0.0 |
| MOS Blur | mos_blur | `mos.blur` | WASM | 0.1.0 |

Default install folder: `%APPDATA%\EffectCraft` (`C:\Users\<you>\AppData\Roaming\EffectCraft`).

> **Security Note**: This is an independent open-source project without a paid commercial code-signing certificate, so Windows SmartScreen or some antivirus engines may show false-positive warnings on the `.exe` installer. If you prefer not to run the `.exe`, **you can skip it completely and use the ready-to-copy `manual\` folder** (Option 2 below).

---

## Option 1 — Step-by-step Graphical Installer (Recommended)

Run `MOS-Suite-Setup-1.5.0.exe` and follow the 4 steps in the left navigation rail:

1. **Welcome**: Overview of the suite and current install status (switch installer UI between 中文 / EN anytime at the bottom-left).
2. **Language**:
   - **Effect names** (`System` / `English`): Controls group and effect names in the Effects panel.
   - **Parameter language** (`System` / `中文` / `English`): Controls parameter labels and dropdowns inside effect controls.
3. **Effects**: Toggle individual effect cards on or off (Select all / Clear supported).
4. **Location**: Confirm `%APPDATA%\EffectCraft` (or pick a custom folder), review the summary, and press **Install**.
5. Restart EffectCraft to load all effects.

---

## Option 2 — Manual Installation (No Installer `.exe` Needed)

Everything is pre-organized inside the `manual\` folder of the release archive.

### 1. Quit EffectCraft

Quit EffectCraft completely and end any running `effectcraft-helper-mos-*.exe` processes.

### 2. Copy files to `%APPDATA%\EffectCraft`

`manual\plug-ins\` contains all 5 WASM effects in four `<names>-<params>` language pairs: `zh-zh`, `zh-en`, `en-zh`, `en-en`. Pick the pair you want and copy:

| Destination (inside `%APPDATA%\EffectCraft\`) | Source (inside `manual\`) |
| --- | --- |
| `Plug-ins\mos_shake.wasm` | `plug-ins\<pair>\mos_shake.wasm` |
| `Plug-ins\ae_warp.wasm` | `plug-ins\<pair>\ae_warp.wasm` |
| `Plug-ins\mos_ripple.wasm` | `plug-ins\<pair>\mos_ripple.wasm` |
| `Plug-ins\mos_shadow.wasm` | `plug-ins\<pair>\mos_shadow.wasm` |
| `Plug-ins\mos_blur.wasm` | `plug-ins\<pair>\mos_blur.wasm` |
| `helpers\effectcraft-helper-mos-rain.exe` | `helpers\effectcraft-helper-mos-rain.exe` |
| `helpers\effectcraft-helper-mos-lens.exe` | `helpers\effectcraft-helper-mos-lens.exe` |
| `helpers\effectcraft-helper-mos-lightmix.exe` | `helpers\effectcraft-helper-mos-lightmix.exe` |
| `helpers\effectcraft-helper-mos-rimlight.exe` | `helpers\effectcraft-helper-mos-rimlight.exe` |
| `helpers\effectcraft-helper-mos-lightsweep.exe` | `helpers\effectcraft-helper-mos-lightsweep.exe` |
| `helpers\effectcraft-helper-mos-dissolve.exe` | `helpers\effectcraft-helper-mos-dissolve.exe` |
| `Scripts\Startup\mos-load-helpers.jsx` | `Scripts\Startup\mos-load-helpers.jsx` |

> - When upgrading from an older version, remove any legacy `Plug-ins\mos_rain.wasm`, `mos_lens.wasm`, or `mos_shrink.wasm` to avoid duplicate entries.
> - Create `Plug-ins`, `helpers`, and `Scripts\Startup` if they do not exist yet. **Partial installs are fully supported.**

### 3. Copy the language config

Copy `manual\mos-suite.en.json` (English) or `manual\mos-suite.zh.json` (Chinese) to `%APPDATA%\EffectCraft\mos-suite.json`.
The external helpers (`mos_rain`, `mos_lens`, `mos_lightmix`, `mos_rimlight`, `mos_lightsweep`, `mos_dissolve`) read `namesResolved` and `paramsResolved` from this file.

### 4. Enable script permission (required for external helpers)

In EffectCraft, go to **Settings ▸ Scripting & Expressions**, enable **Allow Scripts to Write Files and Access Network**, and restart EffectCraft so the startup script can scan external helpers automatically.

---

## Uninstall

- **Via Installer**: Re-open `MOS-Suite-Setup-1.5.0.exe` and click **Uninstall** on the Welcome step.
- **Manual**: Quit EffectCraft and remove the copied `.wasm` files, `helpers\effectcraft-helper-mos-*.exe`, `Scripts\Startup\mos-load-helpers.jsx`, `mos-suite.json`, and `mos-suite\`.

---

## Troubleshooting

- **External helpers (Rain / Lens / Light Mix / Rim Light / Light Sweep / Dissolve) do not appear**: Run **Effects ▸ Scan Effect Helpers…** and verify Step 4 script permissions are enabled.
- **Access denied when copying**: EffectCraft or a helper process is still running; quit completely and retry.
- **Warp (`mos.ae.warp`) stays Chinese**: That plug-in currently ships a Chinese-only manifest (`mos_收缩`).

## License

All effects in this suite are open source under **AGPL-3.0-or-later**.
