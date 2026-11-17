// InstallerCore.cs — payload extraction, install/uninstall, state detection, silent CLI helpers.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MosSuite
{
    internal sealed class ComponentInfo
    {
        public string Id;
        public string Wasm;
        public string Version;
        public string Group;
        public bool Selected = true;

        public string Name(string effectsLang)
        {
            return L10n.EffectName(Id, effectsLang);
        }
    }

    internal sealed class InstallOptions
    {
        public string Root;
        public string EffectsLanguage = "zh";
        public string InstallerLanguage = "zh";
        public List<ComponentInfo> Components = new List<ComponentInfo>();
    }

    internal sealed class InstallReport
    {
        public bool Ok;
        public string Error;
        public string Root;
    }

    internal sealed class InstallState
    {
        public bool Installed;
        public string SuiteVersion = "";
        public string EffectsLanguage = "zh";
        public string InstallerLanguage = "zh";
        public string Root = "";
        public List<string> Files = new List<string>();

        // A component counts as present when the install record lists it or the
        // file is physically there (manual installs and older copies have no record).
        public bool Has(string id)
        {
            if (Files.Exists(delegate(string f) { return f.IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0; }))
                return true;
            if (Root.Length == 0) return false;
            if (id == "lightmix")
                return File.Exists(Path.Combine(Root, "helpers", InstallerCore.HelperExe));
            string wasm = "";
            if (id == "shake") wasm = "mos_shake.wasm";
            else if (id == "shrink") wasm = "mos_shrink.wasm";
            else if (id == "shadow") wasm = "mos_shadow.wasm";
            return wasm.Length > 0 && File.Exists(Path.Combine(Root, "Plug-ins", wasm));
        }
    }

    internal static class InstallerCore
    {
        public const string SuiteVersion = "1.0.0";
        public const string ConfigName = "mos-suite.json";
        public const string RecordDir = "mos-suite";
        public const string HelperExe = "effectcraft-helper-mos-lightmix.exe";
        public const string StartupScript = "mos-load-helpers.jsx";

        public static string DefaultRoot()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EffectCraft");
        }

        public static List<ComponentInfo> Components()
        {
            List<ComponentInfo> list = new List<ComponentInfo>();
            list.Add(new ComponentInfo { Id = "shake", Wasm = "mos_shake.wasm", Version = "1.2.0", Group = "deform" });
            list.Add(new ComponentInfo { Id = "shrink", Wasm = "mos_shrink.wasm", Version = "1.3.7", Group = "deform" });
            list.Add(new ComponentInfo { Id = "shadow", Wasm = "mos_shadow.wasm", Version = "0.6.2", Group = "vfx" });
            list.Add(new ComponentInfo { Id = "lightmix", Wasm = "", Version = "1.4.0", Group = "vfx" });
            return list;
        }

        public static bool HasLightmix(InstallOptions o)
        {
            foreach (ComponentInfo c in o.Components)
            {
                if (c.Id == "lightmix" && c.Selected) return true;
            }
            return false;
        }

        // ---------------- payload ----------------

        private static Stream PayloadStream()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            return asm.GetManifestResourceStream("Payload");
        }

        private static void ExtractPayload(string target)
        {
            using (Stream s = PayloadStream())
            {
                if (s == null) throw new InvalidOperationException("The installer payload is missing. / 安装包不完整：缺少 payload 资源。");
                using (ZipArchive zip = new ZipArchive(s, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        if (entry.FullName.EndsWith("/")) continue;
                        string rel = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                        string dest = Path.Combine(target, rel);
                        string dir = Path.GetDirectoryName(dest);
                        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                        using (Stream src = entry.Open())
                        using (FileStream dst = File.Create(dest))
                        {
                            src.CopyTo(dst);
                        }
                    }
                }
            }
        }

        // ---------------- install ----------------

        public static InstallReport Install(InstallOptions o, Action<float, string> progress)
        {
            InstallReport report = new InstallReport();
            report.Root = o.Root;
            string temp = Path.Combine(Path.GetTempPath(), "mos-suite-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try
            {
                KillHelper();
                if (File.Exists(Path.Combine(o.Root, "EffectCraft.exe")))
                {
                    // The host may lock its Plug-ins folder while running; a copy failure below reports it.
                }
                Directory.CreateDirectory(o.Root);

                progress(0.04f, L10n.T("progress.extract"));
                ExtractPayload(temp);

                List<string> relative = new List<string>();
                int selected = 0;
                foreach (ComponentInfo c in o.Components)
                {
                    if (c.Selected) selected++;
                }
                float step = 0.72f / Math.Max(1, selected + (HasLightmix(o) ? 2 : 0));
                float done = 0.08f;

                bool lightmix = false;
                foreach (ComponentInfo c in o.Components)
                {
                    if (!c.Selected) continue;
                    if (c.Id == "lightmix")
                    {
                        lightmix = true;
                        continue;
                    }
                    progress(done, L10n.F("progress.copy", c.Name(o.EffectsLanguage)));
                    string src = Path.Combine(temp, "plug-ins", o.EffectsLanguage, c.Wasm);
                    if (!File.Exists(src)) throw new FileNotFoundException("Payload entry missing: " + src);
                    string dst = Path.Combine(o.Root, "Plug-ins", c.Wasm);
                    Directory.CreateDirectory(Path.GetDirectoryName(dst));
                    File.Copy(src, dst, true);
                    relative.Add("Plug-ins\\" + c.Wasm);
                    done += step;
                }

                if (lightmix)
                {
                    progress(done, L10n.T("progress.helper"));
                    string helperSrc = Path.Combine(temp, "helper", HelperExe);
                    if (!File.Exists(helperSrc)) throw new FileNotFoundException("Payload entry missing: " + helperSrc);
                    Directory.CreateDirectory(Path.Combine(o.Root, "helpers"));
                    File.Copy(helperSrc, Path.Combine(o.Root, "helpers", HelperExe), true);
                    relative.Add("helpers\\" + HelperExe);

                    progress(done + step, L10n.T("progress.script"));
                    string jsxSrc = Path.Combine(temp, "scripts", StartupScript);
                    Directory.CreateDirectory(Path.Combine(o.Root, "Scripts", "Startup"));
                    File.Copy(jsxSrc, Path.Combine(o.Root, "Scripts", "Startup", StartupScript), true);
                    relative.Add("Scripts\\Startup\\" + StartupScript);
                    done += step * 2f;
                }

                progress(done, L10n.T("progress.manual"));
                CopyManual(temp, o.Root, "zh", "安装说明.md", relative);
                CopyManual(temp, o.Root, "en", "Install-Guide.md", relative);

                progress(0.9f, L10n.T("progress.config"));
                WriteConfig(o, relative);

                progress(0.96f, L10n.T("progress.verify"));
                foreach (string rel in relative)
                {
                    string full = Path.Combine(o.Root, rel);
                    if (!File.Exists(full)) throw new IOException("Verification failed: " + full);
                }

                progress(1f, L10n.T("progress.done"));
                report.Ok = true;
                return report;
            }
            catch (Exception ex)
            {
                report.Ok = false;
                report.Error = DescribeError(ex);
                return report;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(temp)) Directory.Delete(temp, true);
                }
                catch { }
            }
        }

        private static void CopyManual(string temp, string root, string lang, string fileName, List<string> relative)
        {
            string src = Path.Combine(temp, "manual", lang, fileName);
            if (!File.Exists(src)) return;
            string rel = Path.Combine(RecordDir, "manual", lang, fileName);
            string dst = Path.Combine(root, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dst));
            File.Copy(src, dst, true);
            relative.Add(rel);
        }

        private static string DescribeError(Exception ex)
        {
            string message = ex.Message;
            if (ex is IOException || ex is UnauthorizedAccessException)
            {
                message = message + "\r\n\r\n" + L10n.T("err.helperRunning");
            }
            return message;
        }

        private static void WriteConfig(InstallOptions o, List<string> relative)
        {
            StringBuilder components = new StringBuilder();
            foreach (ComponentInfo c in o.Components)
            {
                if (!c.Selected) continue;
                if (components.Length > 0) components.Append(", ");
                components.Append("\"" + c.Id + "\": \"" + Json(c.Version) + "\"");
            }

            StringBuilder files = new StringBuilder();
            foreach (string rel in relative)
            {
                if (files.Length > 0) files.Append(", ");
                files.Append("\"" + Json(rel) + "\"");
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"schema\": 1,");
            sb.AppendLine("  \"suiteVersion\": \"" + SuiteVersion + "\",");
            sb.AppendLine("  \"effectsLanguage\": \"" + Json(o.EffectsLanguage) + "\",");
            sb.AppendLine("  \"installerLanguage\": \"" + Json(o.InstallerLanguage) + "\",");
            sb.AppendLine("  \"installedAt\": \"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\",");
            sb.AppendLine("  \"root\": \"" + Json(o.Root) + "\",");
            sb.AppendLine("  \"components\": { " + components + " },");
            sb.AppendLine("  \"files\": [" + files + "]");
            sb.AppendLine("}");

            string root = o.Root;
            Directory.CreateDirectory(Path.Combine(root, RecordDir));
            File.WriteAllText(Path.Combine(root, ConfigName), sb.ToString(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(root, RecordDir, "install.json"), sb.ToString(), new UTF8Encoding(false));
        }

        private static string Json(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        // ---------------- uninstall ----------------

        public static InstallReport Uninstall(string root, Action<float, string> progress)
        {
            InstallReport report = new InstallReport();
            report.Root = root;
            try
            {
                progress(0.1f, L10n.T("progress.config"));
                KillHelper();
                InstallState state = Detect(root);
                List<string> files = new List<string>(state.Files);
                string[] known = {
                    "Plug-ins\\mos_shadow.wasm", "Plug-ins\\mos_shake.wasm", "Plug-ins\\mos_shrink.wasm",
                    "helpers\\" + HelperExe, "Scripts\\Startup\\" + StartupScript,
                    Path.Combine(RecordDir, "manual", "zh", "安装说明.md"),
                    Path.Combine(RecordDir, "manual", "en", "Install-Guide.md")
                };
                foreach (string f in known)
                {
                    if (!files.Contains(f)) files.Add(f);
                }

                int i = 0;
                foreach (string rel in files)
                {
                    progress(0.2f + 0.6f * (i / (float)Math.Max(1, files.Count)), L10n.F("progress.copy", Path.GetFileName(rel)));
                    string full = Path.Combine(root, rel);
                    try
                    {
                        if (File.Exists(full)) File.Delete(full);
                    }
                    catch { }
                    i++;
                }

                string record = Path.Combine(root, RecordDir);
                try
                {
                    if (Directory.Exists(record)) Directory.Delete(record, true);
                }
                catch { }
                string cfg = Path.Combine(root, ConfigName);
                try
                {
                    if (File.Exists(cfg)) File.Delete(cfg);
                }
                catch { }

                progress(1f, L10n.T("progress.done"));
                report.Ok = true;
                return report;
            }
            catch (Exception ex)
            {
                report.Ok = false;
                report.Error = ex.Message;
                return report;
            }
        }

        // ---------------- state ----------------

        public static InstallState Detect(string root)
        {
            InstallState state = new InstallState();
            state.Root = root;
            string cfg = Path.Combine(root, ConfigName);
            if (!File.Exists(cfg))
            {
                // No suite record: components may still be present (manual install),
                // which Has() reports per file; the suite itself is not installed.
                return state;
            }
            state.Installed = true;
            string text = File.ReadAllText(cfg, Encoding.UTF8);
            state.SuiteVersion = Match(text, "suiteVersion");
            string effects = Match(text, "effectsLanguage");
            if (effects == "en" || effects == "zh") state.EffectsLanguage = effects;
            string installer = Match(text, "installerLanguage");
            if (installer == "en" || installer == "zh") state.InstallerLanguage = installer;

            Match m = Regex.Match(text, "\"files\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
            if (m.Success)
            {
                foreach (Match item in Regex.Matches(m.Groups[1].Value, "\"((?:[^\"\\\\]|\\\\.)*)\""))
                {
                    state.Files.Add(item.Groups[1].Value.Replace("\\\\", "\\"));
                }
            }
            return state;
        }

        private static string Match(string text, string key)
        {
            Match m = Regex.Match(text, "\"" + key + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : "";
        }

        // ---------------- helpers ----------------

        public static void KillHelper()
        {
            Process[] list = Process.GetProcessesByName("effectcraft-helper-mos-lightmix");
            foreach (Process p in list)
            {
                try
                {
                    p.Kill();
                    p.WaitForExit(2000);
                }
                catch { }
                finally { p.Dispose(); }
            }
        }

        public static void OpenFolder(string path)
        {
            try
            {
                if (Directory.Exists(path)) Process.Start("explorer.exe", "\"" + path + "\"");
            }
            catch { }
        }
    }
}
