# MOS Suite · Manual Installation Guide

For EffectCraft. The suite ships five effects in three Effects-panel groups:

| Group | Effect | Effect ID | Version |
| --- | --- | --- | --- |
| MOS Deform | mos_shake | mos.shake | 1.2.0 |
| MOS Deform | mos_收缩 (Warp) | mos.ae.warp | 1.3.0 |
| MOS VFX | mos_shadow | mos.shadow | 0.6.2 |
| MOS VFX | mos_lightmix | mos.lightwrap | 1.5.0 |
| MOS Blur | mos_blur | mos.blur | 0.1.0 |

Default install folder: `%APPDATA%\EffectCraft` (`C:\Users\<you>\AppData\Roaming\EffectCraft`).

## Option 1 — the installer (recommended)

Run `MOS-Suite-Setup-1.3.0.exe`. The installer follows the system language on its own; there is no installer-language picker. It offers two language settings:

1. **Effect names** (System / English) — the language of the **group and effect names** in the Effects panel. "System" follows the Windows display language and falls back to English on non-Chinese systems.
2. **Parameter language** (System / 中文 / English) — the language of **parameter names and popup options** inside the effect controls. "System" follows the system language and falls back to English when the suite has no build for it (Japanese, for example).

Untick any effect you do not want, confirm the install folder, press **Install**, then restart EffectCraft.

Both settings can be changed later by re-running the installer or by editing `mos-suite.json` (below).

## Option 2 — manual installation

### 1. Quit EffectCraft

Quit EffectCraft completely, and end `effectcraft-helper-mos-lightmix.exe` if Light Mix is already installed.

### 2. Copy the files

The installer payload carries the four wasm effects in four "names × parameters" combinations. Folder names are `<names>-<params>`: `zh-zh`, `zh-en`, `en-zh`, `en-en`. Pick your combination first (for example "names follow system = Chinese, parameters = English" is `zh-en`), then copy (the warp effect has a single Chinese manifest, so all four folders hold the same file):

| Destination | Source |
| --- | --- |
| `Plug-ins\mos_shadow.wasm` | `plug-ins\<pair>\mos_shadow.wasm` |
| `Plug-ins\mos_shake.wasm` | `plug-ins\<pair>\mos_shake.wasm` |
| `Plug-ins\ae_warp.wasm` | `plug-ins\<pair>\ae_warp.wasm` |
| `Plug-ins\mos_blur.wasm` | `plug-ins\<pair>\mos_blur.wasm` |
| `helpers\effectcraft-helper-mos-lightmix.exe` | `helper\effectcraft-helper-mos-lightmix.exe` |
| `Scripts\Startup\mos-load-helpers.jsx` | `scripts\mos-load-helpers.jsx` |
| `mos-suite\manual\zh\安装说明.md` | `manual\zh\安装说明.md` |
| `mos-suite\manual\en\Install-Guide.md` | `manual\en\Install-Guide.md` |

Create `Plug-ins`, `helpers` and `Scripts\Startup` if they do not exist. **Partial installs are fine**: an effect simply does not appear if its `.wasm` is missing.

### 3. Write the language settings

Create `%APPDATA%\EffectCraft\mos-suite.json` (UTF-8, no BOM):

```json
{
  "schema": 2,
  "suiteVersion": "1.3.0",
  "namesLanguage": "system",
  "paramsLanguage": "system",
  "namesResolved": "en",
  "paramsResolved": "en"
}
```

- `namesLanguage` is `system` or `en` (`system` follows the OS; non-Chinese systems get English).
- `paramsLanguage` is `system`, `zh` or `en` (`system` falls back to English when the suite has no build for that language).
- `namesResolved` / `paramsResolved` are the languages actually in effect and **the Light Mix helper reads these**; without them it falls back to `namesLanguage` / `paramsLanguage`, then to English.
- Suite 1.0 used a single `effectsLanguage` (`zh` / `en`) for everything; it is still honoured, but the two new keys win.
- With no language settings at all the suite runs in English.

### 4. Allow the startup script

EffectCraft ▸ Settings ▸ Scripting & Expressions ▸ enable **Allow Scripts to Write Files and Access Network**, then restart EffectCraft. The startup script needs this to scan for effect helpers.

### 5. Verify

Open the Effects panel: you should see **MOS Deform**, **MOS VFX** and **MOS Blur** (or **MOS 形变 / MOS 视效 / MOS 模糊** in Chinese). Drag one onto a layer to try it.

## Uninstall

Quit EffectCraft, then delete:

- `Plug-ins\mos_shadow.wasm`, `mos_shake.wasm`, `ae_warp.wasm`, `mos_blur.wasm` (when upgrading from an older suite, also remove the leftover `mos_shrink.wasm`)
- `helpers\effectcraft-helper-mos-lightmix.exe`
- `Scripts\Startup\mos-load-helpers.jsx`
- the whole `mos-suite\` folder
- `mos-suite.json`

If you used the installer, just reopen it and press **Uninstall**.

## Troubleshooting

**Light Mix does not appear.** Run Effects ▸ Scan Effect Helpers…, confirm `helpers\effectcraft-helper-mos-lightmix.exe` and `Scripts\Startup\mos-load-helpers.jsx` exist and that the script permission is on. `effect.helpers.list` should show `running: true`.

**Light Mix picks the wrong background position.** The helper cannot read background-layer transforms — a known limitation. Pre-compose the background and use the comp as the background layer.

**Blur on an adjustment layer shows no edge feathering.** An adjustment layer's mask is applied *after* the effect, so the wasm only sees a fully opaque buffer and cannot locate the mask edge: `Size` blurs evenly and `Edge Feather` produces no gradient. Put the effect on the normal layer below instead — that layer's alpha is the mask coverage. Also leave **Keep original opacity** off on adjustment layers, or the effect disappears entirely.

**Replacing a wasm fails with access denied.** EffectCraft is still running; quit it fully and retry.

**Names and parameters use different languages, or a language change does nothing.** Each wasm carries exactly one names+parameters combination, so either re-run the installer (it swaps the wasm) or replace the wasm with the matching `<pair>` build by hand; keep `namesResolved` / `paramsResolved` in `mos-suite.json` in sync and restart. The warp effect (`mos.ae.warp`) only ships a Chinese manifest: its display name stays `mos_收缩` in both name languages and its parameters do not follow the parameter language.

**Non-Chinese system still shows Chinese effect names.** A `zh-*` build is installed; set **Effect names** to English (or System) in the installer and reinstall.

## License

The five effects are distributed under AGPL-3.0-or-later (see the suite repository `LICENSE`); if you run a modified version as a network service you must offer its source. Light Mix is an independent implementation of a light-wrap style effect and contains no code or assets from any third-party product.
