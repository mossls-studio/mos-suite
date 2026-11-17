// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 MOSSLS

// Program.cs — entry point and silent (/S) command line.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MosSuite
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            bool silent = false;
            bool uninstall = false;
            string names = null;
            string parameters = null;
            string dir = null;

            foreach (string raw in args)
            {
                string a = raw.Trim();
                if (a.Length == 0) continue;
                if (Equals(a, "/S") || Equals(a, "--silent") || Equals(a, "/silent")) silent = true;
                else if (Equals(a, "/uninstall") || Equals(a, "--uninstall")) uninstall = true;
                else if (a.StartsWith("/names=", StringComparison.OrdinalIgnoreCase)) names = NormalizeNames(a.Substring(7));
                else if (a.StartsWith("/params=", StringComparison.OrdinalIgnoreCase)) parameters = NormalizeParams(a.Substring(8));
                else if (a.StartsWith("/effects=", StringComparison.OrdinalIgnoreCase))
                {
                    // 1.0 compatibility: one switch set both languages.
                    string both = NormalizeParams(a.Substring(9));
                    names = NormalizeNames(a.Substring(9));
                    parameters = both;
                }
                else if (a.StartsWith("/dir=", StringComparison.OrdinalIgnoreCase)) dir = TrimQuotes(a.Substring(5));
            }

            if (!silent)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new InstallerForm(names, parameters, dir));
                return 0;
            }

            return RunSilent(names, parameters, dir, uninstall);
        }

        private static string NormalizeNames(string value)
        {
            return value.Trim().ToLowerInvariant() == "en" ? "en" : "system";
        }

        private static string NormalizeParams(string value)
        {
            string v = value.Trim().ToLowerInvariant();
            if (v == "en" || v == "en-us" || v == "english") return "en";
            if (v == "zh" || v == "zh-cn" || v == "zh-hans" || v == "chinese") return "zh";
            return "system";
        }

        private static string TrimQuotes(string value)
        {
            string v = value.Trim();
            if (v.Length >= 2 && v[0] == '"' && v[v.Length - 1] == '"') v = v.Substring(1, v.Length - 2);
            return v;
        }

        private static int RunSilent(string names, string parameters, string dir, bool uninstall)
        {
            string root = string.IsNullOrEmpty(dir) ? InstallerCore.DefaultRoot() : dir;
            List<string> lines = new List<string>();
            try
            {
                L10n.UiLang = L10n.SystemLanguage();
                if (uninstall)
                {
                    InstallReport report = InstallerCore.Uninstall(root, delegate(float v, string s) { });
                    lines.Add("uninstall " + (report.Ok ? "ok" : "failed: " + report.Error));
                    WriteLog(lines);
                    return report.Ok ? 0 : 1;
                }

                InstallOptions options = new InstallOptions();
                options.Root = root;
                options.NamesLanguage = string.IsNullOrEmpty(names) ? "system" : names;
                options.ParamsLanguage = string.IsNullOrEmpty(parameters) ? "system" : parameters;
                options.Components = InstallerCore.Components();
                InstallReport result = InstallerCore.Install(options, delegate(float v, string s) { lines.Add(s); });
                lines.Add("install " + (result.Ok ? "ok" : "failed: " + result.Error));
                lines.Add("root " + root);
                lines.Add("names " + options.NamesLanguage);
                lines.Add("params " + options.ParamsLanguage);
                WriteLog(lines);
                return result.Ok ? 0 : 1;
            }
            catch (Exception ex)
            {
                lines.Add("exception: " + ex.ToString());
                WriteLog(lines);
                return 1;
            }
        }

        private static void WriteLog(List<string> lines)
        {
            try
            {
                string path = Path.Combine(Path.GetTempPath(), "mos-suite-setup.log");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                foreach (string line in lines) sb.AppendLine(line);
                sb.AppendLine();
                File.AppendAllText(path, sb.ToString(), new UTF8Encoding(false));
            }
            catch { }
        }

        private static bool Equals(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
