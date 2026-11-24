// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 MOSSLS

// L10n.cs — bilingual strings (zh / en) for the MOS Suite 1.5.0 step-by-step M3 wizard.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MosSuite
{
    internal static class L10n
    {
        public static string UiLang = "zh";

        private static readonly Dictionary<string, string> Zh = new Dictionary<string, string>
        {
            { "app.title", "MOS 套件" },
            { "app.subtitle", "EffectCraft 效果合集" },
            { "close.tip", "关闭" },
            { "min.tip", "最小化" },

            // Navigation Rail steps
            { "step.welcome", "欢迎" },
            { "step.language", "语言设置" },
            { "step.effects", "选择效果" },
            { "step.location", "安装位置" },
            { "step.install", "安装" },
            { "step.counter", "步骤 {0} / 4" },

            // Step 1: Welcome
            { "welcome.title", "欢迎使用 MOS 效果套件" },
            { "welcome.body", "包含 11 款专为 EffectCraft 打造的开源效果插件，涵盖形变、液态光学、视效、效果与模糊五大分组。接下来只需 3 步即可完成配置。" },
            { "welcome.group.deform", "MOS 形变 · 抖动、收缩（变形·白点）" },
            { "welcome.group.liquid", "MOS 液态 · 涟漪、雨滴、透镜（支持自定义图层）" },
            { "welcome.group.vfx", "MOS 视效 · 阴影、灯光混合、打光" },
            { "welcome.group.effects", "MOS 效果 · 扫光、消散" },
            { "welcome.group.blur", "MOS 模糊 · 高斯与羽化模糊" },
            { "welcome.installed", "检测到已安装 v{0}（名称：{1} · 参数：{2}）" },
            { "welcome.fresh", "尚未安装 · 默认安装到当前用户的 EffectCraft 目录" },

            // Step 2: Language
            { "lang.title", "选择效果语言" },
            { "lang.body", "可以分开设置效果面板中的名称语言，以及右侧控件栏里的参数语言。" },
            { "lang.names", "效果名称" },
            { "lang.names.hint", "决定效果栏里的分组名与效果名；选「跟随系统」时跟随 Windows 显示语言。" },
            { "lang.params", "参数语言" },
            { "lang.params.hint", "决定效果控件里的参数名与下拉选项；非中文系统默认显示英文。" },
            { "lang.system", "跟随系统" },
            { "lang.chinese", "中文" },
            { "lang.english", "English" },
            { "lang.preview.label", "面板预览" },

            // Step 3: Effects
            { "effects.title", "选择要安装的效果" },
            { "effects.body", "点击卡片勾选或取消，稍后可随时重新运行安装程序增删组件。" },
            { "effects.selectAll", "全选" },
            { "effects.selectNone", "全不选" },
            { "effects.count", "已选 {0} / {1}" },
            { "effects.helperTag", "外部助手" },
            { "effects.wasmTag", "WASM" },
            { "effects.installedTag", "已装" },

            // Step 4: Location & Review
            { "loc.title", "确认安装位置与摘要" },
            { "loc.body", "文件会写入 Plug-ins、helpers 与 Scripts\\Startup，不会影响其他插件或工程文件。" },
            { "loc.folder", "目标文件夹" },
            { "loc.browse", "更改…" },
            { "loc.found", "已检测到 EffectCraft 配置目录" },
            { "loc.willCreate", "该目录尚无配置，安装时将自动创建所需子文件夹" },
            { "loc.summary", "安装摘要" },
            { "loc.summary.lang", "语言配置：名称 {0} · 参数 {1}" },
            { "loc.summary.count", "选中效果：{0} 个（含 {1} 个外部助手与自动加载脚本）" },
            { "loc.summary.countWasmOnly", "选中效果：{0} 个（全部为轻量 WASM 插件）" },

            // Step 5: Working / Done / Error
            { "work.title", "正在安装 MOS 套件" },
            { "work.sub", "正在写入插件文件与语言配置，请稍候…" },
            { "work.uninstalling", "正在卸载 MOS 套件" },
            { "done.title", "安装完成" },
            { "done.body", "已成功安装 {0} 个效果。请重启 EffectCraft 以加载全部插件。" },
            { "done.helperTip", "提示：如需在当前窗口立即加载外部助手（雨滴、透镜、灯光混合、打光、扫光、消散），可执行「效果 ▸ 扫描效果辅助程序…」，并确认设置中已允许脚本读写文件。" },
            { "err.title", "安装未完成" },
            { "err.helperRunning", "请先完全退出 EffectCraft（以及正在运行的 effectcraft-helper-mos-* 进程），然后点击重试。" },

            // Uninstall dialog & completion
            { "unin.title", "卸载 MOS 套件？" },
            { "unin.body", "将移除 MOS 套件的全部 11 个效果、自动加载脚本与语言配置文件，不会影响其他插件和工程。" },
            { "unin.done", "已卸载完成" },
            { "unin.doneBody", "已从目录中清理 MOS 套件的全部插件与配置文件。" },
            { "unin.failed", "卸载未完成" },

            // Buttons
            { "btn.start", "开始配置" },
            { "btn.next", "下一步" },
            { "btn.back", "上一步" },
            { "btn.install", "开始安装" },
            { "btn.apply", "应用并安装" },
            { "btn.cancel", "取消" },
            { "btn.close", "完成" },
            { "btn.open", "打开文件夹" },
            { "btn.retry", "重试" },
            { "btn.uninstall", "卸载已安装" },

            // Labels & Progress
            { "names.zh", "中文" },
            { "names.en", "英文" },
            { "params.zh", "中文" },
            { "params.en", "English" },
            { "progress.extract", "正在解包安装资源…" },
            { "progress.copy", "正在安装 {0}…" },
            { "progress.helper", "正在安装外部助手 {0}…" },
            { "progress.remove", "正在移除 {0}…" },
            { "progress.script", "正在写入助手自动加载脚本…" },
            { "progress.manual", "正在写入双语使用说明…" },
            { "progress.config", "正在写入语言配置文件…" },
            { "progress.verify", "正在校验文件完整性…" },
            { "progress.done", "全部完成" }
        };

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "app.title", "MOS Suite" },
            { "app.subtitle", "EffectCraft Effects Pack" },
            { "close.tip", "Close" },
            { "min.tip", "Minimize" },

            // Navigation Rail steps
            { "step.welcome", "Welcome" },
            { "step.language", "Language" },
            { "step.effects", "Effects" },
            { "step.location", "Location" },
            { "step.install", "Install" },
            { "step.counter", "Step {0} of 4" },

            // Step 1: Welcome
            { "welcome.title", "Welcome to MOS Effects Suite" },
            { "welcome.body", "Eleven open-source effects built for EffectCraft across Deform, Liquid, VFX, Effects and Blur. Configure everything in 3 quick steps." },
            { "welcome.group.deform", "MOS Deform · Shake, Warp (Shrink)" },
            { "welcome.group.liquid", "MOS Liquid · Ripple, Rain, Lens (custom layer maps)" },
            { "welcome.group.vfx", "MOS VFX · Shadow, Light Mix, Rim Light" },
            { "welcome.group.effects", "MOS Effects · Light Sweep, Dissolve" },
            { "welcome.group.blur", "MOS Blur · Gaussian & edge-feathered blur" },
            { "welcome.installed", "Installed v{0} detected (names: {1} · params: {2})" },
            { "welcome.fresh", "Not installed yet · Defaults to your EffectCraft user folder" },

            // Step 2: Language
            { "lang.title", "Choose effect languages" },
            { "lang.body", "Set the Effects panel display names and the parameter controls language independently." },
            { "lang.names", "Effect names" },
            { "lang.names.hint", "Controls group and effect names in the Effects panel; System follows Windows." },
            { "lang.params", "Parameter language" },
            { "lang.params.hint", "Controls parameter labels and dropdowns; defaults to English on non-Chinese systems." },
            { "lang.system", "System" },
            { "lang.chinese", "中文" },
            { "lang.english", "English" },
            { "lang.preview.label", "Panel preview" },

            // Step 3: Effects
            { "effects.title", "Select effects to install" },
            { "effects.body", "Toggle cards to pick what you need. You can re-run setup anytime to add or remove effects." },
            { "effects.selectAll", "Select all" },
            { "effects.selectNone", "Clear" },
            { "effects.count", "{0} / {1} selected" },
            { "effects.helperTag", "Helper" },
            { "effects.wasmTag", "WASM" },
            { "effects.installedTag", "Installed" },

            // Step 4: Location & Review
            { "loc.title", "Confirm location & summary" },
            { "loc.body", "Writes only to Plug-ins, helpers and Scripts\\Startup. Other plug-ins and projects remain untouched." },
            { "loc.folder", "Target folder" },
            { "loc.browse", "Change…" },
            { "loc.found", "Existing EffectCraft folder detected" },
            { "loc.willCreate", "No settings here yet — required subfolders will be created automatically" },
            { "loc.summary", "Install summary" },
            { "loc.summary.lang", "Languages: names {0} · params {1}" },
            { "loc.summary.count", "Effects: {0} selected (includes {1} external helper(s) + startup script)" },
            { "loc.summary.countWasmOnly", "Effects: {0} selected (all lightweight WASM plug-ins)" },

            // Step 5: Working / Done / Error
            { "work.title", "Installing MOS Suite" },
            { "work.sub", "Copying effect binaries and writing language settings…" },
            { "work.uninstalling", "Uninstalling MOS Suite" },
            { "done.title", "Installation complete" },
            { "done.body", "Successfully installed {0} effect(s). Restart EffectCraft to load everything." },
            { "done.helperTip", "Tip: To load external helpers (Rain, Lens, Light Mix, Rim Light, Light Sweep, Dissolve) immediately in an open window, run Effects ▸ Scan Effect Helpers… and ensure script file access is enabled in Settings." },
            { "err.title", "Installation did not finish" },
            { "err.helperRunning", "Quit EffectCraft completely (and end any running effectcraft-helper-mos-* processes), then press Retry." },

            // Uninstall dialog & completion
            { "unin.title", "Uninstall MOS Suite?" },
            { "unin.body", "Removes all 11 MOS effects, the helper startup script and language settings. Other plug-ins and projects are untouched." },
            { "unin.done", "Uninstall complete" },
            { "unin.doneBody", "All MOS Suite files and settings have been removed from the target folder." },
            { "unin.failed", "Uninstall did not finish" },

            // Buttons
            { "btn.start", "Get started" },
            { "btn.next", "Next" },
            { "btn.back", "Back" },
            { "btn.install", "Install" },
            { "btn.apply", "Apply & install" },
            { "btn.cancel", "Cancel" },
            { "btn.close", "Done" },
            { "btn.open", "Open folder" },
            { "btn.retry", "Retry" },
            { "btn.uninstall", "Uninstall" },

            // Labels & Progress
            { "names.zh", "Chinese" },
            { "names.en", "English" },
            { "params.zh", "Chinese" },
            { "params.en", "English" },
            { "progress.extract", "Unpacking installer payload…" },
            { "progress.copy", "Installing {0}…" },
            { "progress.helper", "Installing helper {0}…" },
            { "progress.remove", "Removing {0}…" },
            { "progress.script", "Installing helper startup script…" },
            { "progress.manual", "Writing bilingual manuals…" },
            { "progress.config", "Writing language configuration…" },
            { "progress.verify", "Verifying installed files…" },
            { "progress.done", "All done" }
        };

        public static string T(string key)
        {
            Dictionary<string, string> table = UiLang == "en" ? En : Zh;
            string value;
            return table.TryGetValue(key, out value) ? value : key;
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        public static string SystemLanguage()
        {
            string code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return code == "zh" ? "zh" : "en";
        }

        public static string ResolveNames(string raw)
        {
            return raw == "en" ? "en" : SystemLanguage();
        }

        public static string ResolveParams(string raw)
        {
            if (raw == "zh" || raw == "en") return raw;
            return SystemLanguage();
        }

        public static string NamesLabel(string lang)
        {
            return T(lang == "en" ? "names.en" : "names.zh");
        }

        public static string ParamsLabel(string lang)
        {
            return T(lang == "en" ? "params.en" : "params.zh");
        }

        public static string EffectName(string id, string lang)
        {
            bool en = lang == "en";
            switch (id)
            {
                case "shadow":     return en ? "mos_shadow" : "mos_阴影";
                case "shake":      return en ? "mos_shake" : "mos_抖动";
                case "shrink":     return "mos_收缩";
                case "blur":       return en ? "mos_blur" : "mos_模糊";
                case "lightmix":   return en ? "mos_lightmix" : "mos_灯光混合";
                case "rimlight":   return en ? "mos_rimlight" : "mos_打光";
                case "lightsweep": return en ? "mos_lightsweep" : "mos_扫光";
                case "dissolve":   return en ? "mos_dissolve" : "mos_消散";
                case "ripple":     return en ? "mos_ripple" : "mos_涟漪";
                case "rain":       return en ? "mos_rain" : "mos_雨滴";
                case "lens":       return en ? "mos_lens" : "mos_透镜";
            }
            return id;
        }

        public static string GroupName(string group, string lang)
        {
            bool en = lang == "en";
            if (group == "deform")  return en ? "MOS Deform" : "MOS 形变";
            if (group == "liquid")  return en ? "MOS Liquid" : "MOS 液态";
            if (group == "effects") return en ? "MOS Effects" : "MOS 效果";
            if (group == "blur")    return en ? "MOS Blur" : "MOS 模糊";
            return en ? "MOS VFX" : "MOS 视效";
        }
    }
}
