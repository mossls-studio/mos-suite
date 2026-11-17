// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 MOSSLS

// L10n.cs — installer strings (zh / en) and effect-naming tables.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MosSuite
{
    internal static class L10n
    {
        // "zh" or "en" — the language the installer UI is drawn in.
        public static string UiLang = "zh";

        private static readonly Dictionary<string, string> Zh = new Dictionary<string, string>
        {
            { "app.title", "MOS 套件" },
            { "app.brand", "MOS" },
            { "close.tip", "关闭" },
            { "hero.title", "安装 MOS 效果套件" },
            { "hero.body", "阴影、抖动、收缩、灯光混合，4 个 EffectCraft 效果，中文与英文效果栏都支持。" },
            { "lang.installer", "安装器语言" },
            { "lang.effects", "效果栏语言" },
            { "lang.system", "跟随系统" },
            { "lang.chinese", "中文" },
            { "lang.english", "English" },
            { "lang.hint", "效果栏语言决定效果面板里的分组、效果名和参数名。装完随时可以重跑安装器修改。" },
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
            { "state.installed", "已安装 {0} · 效果栏 {1}" },
            { "effects.zh", "中文" },
            { "effects.en", "英文" },
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
            { "app.brand", "MOS" },
            { "close.tip", "Close" },
            { "hero.title", "Install the MOS effects suite" },
            { "hero.body", "Shadow, Shake, Shrink and Light Mix — four EffectCraft effects, in Chinese and English." },
            { "lang.installer", "Installer language" },
            { "lang.effects", "Effects panel language" },
            { "lang.system", "System" },
            { "lang.chinese", "中文" },
            { "lang.english", "English" },
            { "lang.hint", "The effects language sets the group, effect and parameter names in the Effects panel. Re-run the installer any time to change it." },
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
            { "state.installed", "Installed {0} · effects {1}" },
            { "effects.zh", "Chinese" },
            { "effects.en", "English" },
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

        public static string EffectsLabel(string lang)
        {
            return T(lang == "en" ? "effects.en" : "effects.zh");
        }

        // Effect display names per effects language.
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
