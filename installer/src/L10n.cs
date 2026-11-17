// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 MOSSLS

// L10n.cs — installer strings (zh / en) and effect-naming tables.
// The installer UI itself always follows the system language; only the effect
// naming and the effect-control parameters are configurable.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MosSuite
{
    internal static class L10n
    {
        // "zh" or "en" — the language the installer UI is drawn in (always the system one).
        public static string UiLang = "zh";

        private static readonly Dictionary<string, string> Zh = new Dictionary<string, string>
        {
            { "app.title", "MOS 套件" },
            { "close.tip", "关闭" },
            { "hero.title", "安装 MOS 效果套件" },
            { "hero.body", "阴影、抖动、收缩、灯光混合，4 个 EffectCraft 效果。" },
            { "lang.names", "效果名称" },
            { "lang.names.hint", "英文时效果栏里的分组名、效果名显示英文；跟随系统则使用系统语言。" },
            { "lang.params", "参数语言" },
            { "lang.params.hint", "效果控件里的参数名、选项与说明。系统语言不是中文时默认英文。" },
            { "lang.system", "跟随系统" },
            { "lang.chinese", "中文" },
            { "lang.english", "English" },
            { "comp.installed", "已安装 {0}" },
            { "comp.notInstalled", "未安装" },
            { "comp.update", "有新版本" },
            { "dest.title", "安装位置" },
            { "dest.change", "更改…" },
            { "dest.hint", "写入 Plug-ins、helpers 和 Scripts\\Startup，其他插件与工程不受影响。" },
            { "dest.newFolder", "这个目录还没有 EffectCraft 设置，需要的文件夹会自动创建。" },
            { "btn.install", "安装" },
            { "btn.apply", "应用更改" },
            { "btn.cancel", "取消" },
            { "btn.close", "完成" },
            { "btn.open", "打开文件夹" },
            { "btn.retry", "重试" },
            { "btn.uninstall", "卸载" },
            { "work.title", "正在安装" },
            { "err.title", "安装没有完成" },
            { "err.helperRunning", "请先完全退出 EffectCraft（或结束 effectcraft-helper-mos-lightmix 进程），再点重试。" },
            { "done.title", "安装完成" },
            { "done.body", "重启 EffectCraft 后生效。想马上看到灯光混合，可在当前窗口执行 效果 ▸ 扫描效果辅助程序…" },
            { "unin.title", "卸载 MOS 套件？" },
            { "unin.body", "会删除这 4 个效果和语言设置，不影响其他插件和工程文件。" },
            { "unin.done", "已卸载" },
            { "unin.failed", "卸载没有完成" },
            { "state.installed", "已安装 {0}" },
            { "state.detail", "名称 {0} · 参数 {1}" },
            { "names.zh", "中文" },
            { "names.en", "英文" },
            { "params.zh", "中文" },
            { "params.en", "English" },
            { "progress.extract", "正在解包安装文件…" },
            { "progress.copy", "正在复制 {0}…" },
            { "progress.helper", "正在安装灯光混合助手…" },
            { "progress.script", "正在安装启动脚本…" },
            { "progress.manual", "正在写入安装说明…" },
            { "progress.config", "正在写入语言设置…" },
            { "progress.verify", "正在校验…" },
            { "progress.done", "完成" }
        };

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "app.title", "MOS Suite" },
            { "close.tip", "Close" },
            { "hero.title", "Install the MOS effects suite" },
            { "hero.body", "Shadow, Shake, Shrink and Light Mix — four EffectCraft effects." },
            { "lang.names", "Effect names" },
            { "lang.names.hint", "English shows English group and effect names in the Effects panel; System uses your language." },
            { "lang.params", "Parameter language" },
            { "lang.params.hint", "Parameter names, options and help inside the effect controls. Defaults to English unless the system language is Chinese." },
            { "lang.system", "System" },
            { "lang.chinese", "中文" },
            { "lang.english", "English" },
            { "comp.installed", "Installed {0}" },
            { "comp.notInstalled", "Not installed" },
            { "comp.update", "Update available" },
            { "dest.title", "Install location" },
            { "dest.change", "Change…" },
            { "dest.hint", "Writes to Plug-ins, helpers and Scripts\\Startup. Other plug-ins and projects are untouched." },
            { "dest.newFolder", "No EffectCraft settings here yet; the needed folders are created automatically." },
            { "btn.install", "Install" },
            { "btn.apply", "Apply changes" },
            { "btn.cancel", "Cancel" },
            { "btn.close", "Done" },
            { "btn.open", "Open folder" },
            { "btn.retry", "Retry" },
            { "btn.uninstall", "Uninstall" },
            { "work.title", "Installing" },
            { "err.title", "Installation did not finish" },
            { "err.helperRunning", "Quit EffectCraft completely (or end effectcraft-helper-mos-lightmix) and press Retry." },
            { "done.title", "Installation complete" },
            { "done.body", "Restart EffectCraft to load the effects. To show Light Mix right away, run Effects ▸ Scan Effect Helpers… in the open window." },
            { "unin.title", "Uninstall the MOS Suite?" },
            { "unin.body", "Removes the four effects and their language settings. Other plug-ins and projects are not touched." },
            { "unin.done", "Uninstalled" },
            { "unin.failed", "Uninstall did not finish" },
            { "state.installed", "Installed {0}" },
            { "state.detail", "names {0} · params {1}" },
            { "names.zh", "Chinese" },
            { "names.en", "English" },
            { "params.zh", "Chinese" },
            { "params.en", "English" },
            { "progress.extract", "Unpacking installer files…" },
            { "progress.copy", "Copying {0}…" },
            { "progress.helper", "Installing the Light Mix helper…" },
            { "progress.script", "Installing the startup script…" },
            { "progress.manual", "Writing the manual…" },
            { "progress.config", "Writing language settings…" },
            { "progress.verify", "Verifying…" },
            { "progress.done", "Done" }
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

        // "system" | "en" -> the language actually baked into the installed effect names.
        public static string ResolveNames(string raw)
        {
            return raw == "en" ? "en" : SystemLanguage();
        }

        // "system" | "zh" | "en" -> the language of the effect-control parameters.
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

        // Effect display names per names language.
        public static string EffectName(string id, string lang)
        {
            bool en = lang == "en";
            switch (id)
            {
                case "shadow": return en ? "mos_shadow" : "mos_阴影";
                case "shake": return en ? "mos_shake" : "mos_抖动";
                case "shrink": return en ? "mos_shrink" : "mos_收缩";
                case "lightmix": return en ? "mos_lightmix" : "mos_灯光混合";
            }
            return id;
        }

        public static string GroupName(string group, string lang)
        {
            bool en = lang == "en";
            if (group == "deform") return en ? "MOS Deform" : "MOS 形变";
            return en ? "MOS VFX" : "MOS 视效";
        }
    }
}
