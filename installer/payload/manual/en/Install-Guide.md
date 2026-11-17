# MOS Suite · Manual install guide

For EffectCraft. The suite ships four effects in two Effects-panel groups:

| Group | Effect | Effect ID | Version |
| --- | --- | --- | --- |
| MOS Deform | mos_shake | mos.shake | 1.2.0 |
| MOS Deform | mos_shrink | mos.shrink | 1.3.7 |
| MOS VFX | mos_shadow | mos.shadow | 0.6.2 |
| MOS VFX | mos_lightmix | mos.lightwrap | 1.4.0 |

Default install root: `%APPDATA%\EffectCraft` (i.e. `C:\Users\<you>\AppData\Roaming\EffectCraft`).

## Option 1 — the installer (recommended)

Run `MOS-Suite-Setup-1.0.0.exe`:

1. Pick the installer language (System / 中文 / English).
2. Pick the effects-panel language (System / 中文 / English) — this sets the group, effect and parameter names in the Effects panel.
3. Untick any component you do not want.
4. Press Install, then restart EffectCraft.

You can re-run the installer any time to change the effects language, or edit `mos-suite.json` directly (below).

## Option 2 — manual install

### 1. Quit EffectCraft

Quit EffectCraft completely. If Light Mix was installed before, also end `effectcraft-helper-mos-lightmix.exe`.

### 2. Copy the files

Copy the files for your language (`<lang>` is `zh` or `en`) into place:

| Destination | Source |
| --- | --- |
| `Plug-ins\mos_shadow.wasm` | `plug-ins\<lang>\mos_shadow.wasm` |
| `Plug-ins\mos_shake.wasm` | `plug-ins\<lang>\mos_shake.wasm` |
| `Plug-ins\mos_shrink.wasm` | `plug-ins\<lang>\mos_shrink.wasm` |
| `helpers\effectcraft-helper-mos-lightmix.exe` | `helper\effectcraft-helper-mos-lightmix.exe` |
| `Scripts\Startup\mos-load-helpers.jsx` | `scripts\mos-load-helpers.jsx` |
| `mos-suite\manual\zh\安装说明.md` | `manual\zh\安装说明.md` |
| `mos-suite\manual\en\Install-Guide.md` | `manual\en\Install-Guide.md` |

Create `Plug-ins`, `helpers` and `Scripts\Startup` if they do not exist. Partial installs are fine: an effect only appears when its `.wasm` is present.

### 3. Write the language settings

Create `%APPDATA%\EffectCraft\mos-suite.json` (UTF-8, no BOM):

```json
{
  "schema": 1,
  "suiteVersion": "1.0.0",
  "effectsLanguage": "en"
}
```

- `effectsLanguage` is `zh` or `en`.
- The four effects' names and parameter names come from the wasm build itself, so **the wasm files must match the language**; the Light Mix helper is a single exe that reads this file at startup.
- Without this file the suite defaults to Chinese.

### 4. Allow scripts

EffectCraft ▸ Settings ▸ Scripting & Expressions ▸ enable "Allow Scripts to Write Files and Access Network", then restart EffectCraft. The startup script needs it to scan effect helpers.

### 5. Check

Open the Effects panel: you should see the groups **MOS Deform** and **MOS VFX**. Add an effect to a layer to try it.

## Uninstall

Quit EffectCraft and delete:

- `Plug-ins\mos_shadow.wasm`, `mos_shake.wasm`, `mos_shrink.wasm`
- `helpers\effectcraft-helper-mos-lightmix.exe`
- `Scripts\Startup\mos-load-helpers.jsx`
- the whole `mos-suite\` folder
- `mos-suite.json`

If you used the installer, just run it and press Uninstall.

## Troubleshooting

**Light Mix does not show up.** Run Effects ▸ Scan Effect Helpers…; make sure `helpers\effectcraft-helper-mos-lightmix.exe` and `Scripts\Startup\mos-load-helpers.jsx` exist and script access is allowed. From the console, `effect.helpers.list` should report `running: true`.

**Light Mix uses the wrong background position.** The helper cannot read the background layer's transform — a known limitation. Pre-compose the background first and use the pre-comp as the background layer.

**Overwriting a wasm fails with access denied.** EffectCraft is still running; quit it completely and retry.

**The language does not switch.** Check `effectsLanguage` in `mos-suite.json` and that the wasm files in `Plug-ins` are the matching language build; restart afterwards.

## License

The four effects are distributed under the MIT license. Light Mix is an independent re-implementation of Digital Anarchy Light Wrap Fantastic; it contains none of their code or assets. The name `mos_lightmix` only describes what the effect does.
