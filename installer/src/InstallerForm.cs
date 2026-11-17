// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 MOSSLS

// InstallerForm.cs — the Material 3 installer window (dark scheme, system UI language).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace MosSuite
{
    internal sealed class InstallerForm : Form
    {
        private enum Mode
        {
            Configure,
            Working,
            Done,
            Error
        }

        private readonly float dpiScale;
        private Mode mode = Mode.Configure;
        private bool working;

        // The installer UI always uses the system language.
        private readonly string uiLang = L10n.SystemLanguage();

        // Two independent effect languages: what the Effects panel calls things,
        // and what the effect controls say.
        private string namesLanguage = "system";   // "system" | "en"
        private string paramsLanguage = "system";  // "system" | "zh" | "en"

        private string root = InstallerCore.DefaultRoot();
        private readonly List<ComponentInfo> components = InstallerCore.Components();
        private InstallState state = new InstallState();

        private readonly List<ComponentRow> rows = new List<ComponentRow>();
        private readonly List<M3Text> groupLabels = new List<M3Text>();
        private M3ProgressBar bar;
        private M3Text statusLabel;
        private string errorText = "";
        private string doneTitle = "";
        private string doneBody = "";

        private M3Text pathLabel;
        private M3Text destHint;
        private Panel topBar;
        private Panel content;
        private Panel footer;
        private M3IconButton closeButton;
        private M3Button primaryButton;
        private M3Button uninstallButton;
        private M3Text footerState;

        public InstallerForm() : this(null, null, null)
        {
        }

        // Optional command-line overrides: /names= /params= /dir= also work for the
        // GUI, which makes scripted screenshots and pre-configured runs possible.
        public InstallerForm(string startNames, string startParams, string startRoot)
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "MOS Suite";
            KeyPreview = true;
            DoubleBuffered = true;
            BackColor = Theme.Surface;
            Icon = SystemIcons.Application;

            float scale = 1f;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                scale = g.DpiX / 96f;
            }
            if (scale < 1f) scale = 1f;
            dpiScale = scale;

            if (!string.IsNullOrEmpty(startRoot)) root = startRoot;
            state = InstallerCore.Detect(root);
            if (state.Installed)
            {
                namesLanguage = state.NamesLanguage;
                paramsLanguage = state.ParamsLanguage;
            }
            if (!string.IsNullOrEmpty(startNames)) namesLanguage = startNames;
            if (!string.IsNullOrEmpty(startParams)) paramsLanguage = startParams;

            Theme.Init(dpiScale, uiLang == "zh");
            L10n.UiLang = uiLang;
            Font = Theme.BodyMedium;
            ClientSize = new Size(Theme.Dp(760), Theme.Dp(800));
            Rebuild();
        }

        // ---------------- window chrome ----------------

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000;   // CS_DROPSHADOW
                cp.ExStyle |= 0x02000000;   // WS_EX_COMPOSITED: paint whole window per frame, no flash
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int rounded = 2; // DWMWCP_ROUND
                DwmSetWindowAttribute(Handle, 33, ref rounded, 4);
            }
            catch { }
        }

        private void DragWindow()
        {
            try
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero);
            }
            catch { }
        }

        // ---------------- build ----------------

        private void Rebuild()
        {
            SuspendLayout();
            List<Control> old = new List<Control>();
            foreach (Control c in Controls) old.Add(c);
            Controls.Clear();
            foreach (Control c in old) c.Dispose();

            rows.Clear();
            groupLabels.Clear();
            bar = null;
            statusLabel = null;
            closeButton = null;
            primaryButton = null;
            uninstallButton = null;
            footerState = null;
            pathLabel = null;
            destHint = null;

            Theme.Init(dpiScale, uiLang == "zh");
            L10n.UiLang = uiLang;
            Font = Theme.BodyMedium;
            BackColor = Theme.Surface;

            content = new Panel();
            content.BackColor = Theme.Surface;
            content.AutoScroll = false;

            topBar = new Panel();
            topBar.BackColor = Theme.Surface;
            topBar.Paint += PaintTopBar;
            topBar.MouseDown += OnTopBarMouseDown;

            footer = new Panel();
            footer.BackColor = Theme.Surface;
            footer.Paint += PaintFooter;

            Controls.Add(content);
            Controls.Add(topBar);
            Controls.Add(footer);
            // Explicit bounds: the form is built before its handle exists, so Dock
            // layout has not distributed the space yet when children are placed.
            topBar.SetBounds(0, 0, ClientSize.Width, Theme.Dp(64));
            footer.SetBounds(0, ClientSize.Height - Theme.Dp(88), ClientSize.Width, Theme.Dp(88));
            content.SetBounds(0, Theme.Dp(64), ClientSize.Width, ClientSize.Height - Theme.Dp(64) - Theme.Dp(88));

            BuildTopBarChildren();
            BuildModeContent();
            BuildFooterChildren();
            ResumeLayout(true);

            Invalidate(true);
        }

        private void OnTopBarMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) DragWindow();
        }

        private void BuildTopBarChildren()
        {
            closeButton = new M3IconButton();
            closeButton.Icon = M3IconButton.Glyph.Close;
            closeButton.Size = new Size(Theme.Dp(40), Theme.Dp(40));
            closeButton.Location = new Point(topBar.Width - Theme.Dp(24) - Theme.Dp(40), Theme.Dp(12));
            closeButton.AccessibleName = L10n.T("close.tip");
            closeButton.BackColor = Theme.Surface;
            closeButton.Click += delegate { Close(); };
            topBar.Controls.Add(closeButton);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush bg = new SolidBrush(Theme.Surface))
            {
                e.Graphics.FillRectangle(bg, ClientRectangle);
            }
        }

        private void PaintTopBar(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int pad = Theme.Dp(24);
            int mark = Theme.Dp(28);
            RectangleF markRect = new RectangleF(pad, (Theme.Dp(64) - mark) / 2f, mark, mark);
            Theme.DrawLogo(g, markRect);

            Rectangle titleRect = new Rectangle(pad + mark + Theme.Dp(12), Theme.Dp(16), topBar.Width / 2, Theme.Dp(32));
            TextRenderer.DrawText(g, L10n.T("app.title"), Theme.TitleLarge, titleRect, Theme.OnSurface,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

            string version = "v" + InstallerCore.SuiteVersion;
            int textW = Theme.Measure(version, Theme.LabelSmall);
            int chipW = textW + Theme.Dp(24);
            int chipH = Theme.Dp(24);
            int chipX = topBar.Width - pad - Theme.Dp(40) - Theme.Dp(8) - chipW;
            RectangleF chip = new RectangleF(chipX, (Theme.Dp(64) - chipH) / 2f, chipW, chipH);
            Theme.FillRound(g, Theme.SecondaryContainer, chip, Theme.Dp(8));
            TextRenderer.DrawText(g, version, Theme.LabelSmall, Rectangle.Round(chip), Theme.OnSecondaryContainer,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private void PaintFooter(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Pen pen = new Pen(Theme.OutlineVariant, Theme.Dp(1)))
            {
                g.DrawLine(pen, 0, 0, footer.Width, 0);
            }
        }

        // ---------------- content ----------------

        private void BuildModeContent()
        {
            int w = ClientSize.Width;
            int cardW = w - Theme.Dp(48);
            int x = Theme.Dp(24);
            int y = Theme.Dp(24);

            if (mode == Mode.Configure)
            {
                M3Text hero = Widgets.Label(L10n.T("hero.title"), Theme.HeadlineSmall, Theme.OnSurface, Theme.Surface);
                Widgets.Place(hero, x, y, cardW, Theme.Dp(32));
                content.Controls.Add(hero);

                M3Text sub = Widgets.Label(L10n.T("hero.body"), Theme.BodyMedium, Theme.OnSurfaceVariant, Theme.Surface);
                Widgets.Place(sub, x, y + Theme.Dp(36), cardW, Theme.Dp(22));
                content.Controls.Add(sub);

                y += Theme.Dp(80);
                BuildLanguageCard(x, y, cardW);
                y += Theme.Dp(212) + Theme.Dp(16);
                BuildComponentsCard(x, y, cardW);
                y += Theme.Dp(168) + Theme.Dp(16);
                BuildDestinationCard(x, y, cardW);
            }
            else
            {
                // Focus states (working / done / error) sit as one centered card.
                int focusW = Math.Min(cardW, Theme.Dp(560));
                int focusX = x + (cardW - focusW) / 2;

                if (mode == Mode.Working)
                {
                    M3Card card = new M3Card();
                    int cardH = Theme.Dp(152);
                    int cardY = Math.Max(Theme.Dp(24), (content.Height - cardH) / 2);
                    Widgets.Place(card, focusX, cardY, focusW, cardH);
                    content.Controls.Add(card);

                    M3Text title = Widgets.Label(L10n.T("work.title"), Theme.TitleLarge, Theme.OnSurface, Theme.SurfaceContainer);
                    Widgets.Place(title, Theme.Dp(16), Theme.Dp(16), focusW - Theme.Dp(32), Theme.Dp(28));
                    card.Controls.Add(title);

                    bar = new M3ProgressBar();
                    Widgets.Place(bar, Theme.Dp(16), Theme.Dp(60), focusW - Theme.Dp(32), Theme.Dp(4));
                    bar.Value = 0f;
                    bar.BackColor = Theme.SurfaceContainer;
                    card.Controls.Add(bar);

                    statusLabel = Widgets.Label("", Theme.BodyMedium, Theme.OnSurfaceVariant, Theme.SurfaceContainer);
                    Widgets.Place(statusLabel, Theme.Dp(16), Theme.Dp(78), focusW - Theme.Dp(32), Theme.Dp(40));
                    card.Controls.Add(statusLabel);
                }
                else if (mode == Mode.Done)
                {
                    M3Card card = new M3Card();
                    int cardH = Theme.Dp(200);
                    int cardY = Math.Max(Theme.Dp(24), (content.Height - cardH) / 2);
                    Widgets.Place(card, focusX, cardY, focusW, cardH);
                    content.Controls.Add(card);

                    card.Paint += delegate(object s, PaintEventArgs e)
                    {
                        Graphics g = e.Graphics;
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        RectangleF circle = new RectangleF(Theme.Dp(20), Theme.Dp(20), Theme.Dp(56), Theme.Dp(56));
                        Theme.FillRound(g, Theme.PrimaryContainer, circle, circle.Width / 2f);
                        Theme.DrawCheck(g, new RectangleF(circle.X + Theme.Dp(10), circle.Y + Theme.Dp(10), circle.Width - Theme.Dp(20), circle.Height - Theme.Dp(20)), Theme.OnPrimaryContainer, Theme.Dp(2.4f));
                    };

                    M3Text title = Widgets.Label(doneTitle, Theme.HeadlineSmall, Theme.OnSurface, Theme.SurfaceContainer);
                    Widgets.Place(title, Theme.Dp(92), Theme.Dp(20), focusW - Theme.Dp(112), Theme.Dp(32));
                    card.Controls.Add(title);

                    M3Text body = Widgets.Label(doneBody, Theme.BodyMedium, Theme.OnSurfaceVariant, Theme.SurfaceContainer);
                    body.Wrapped = true;
                    Widgets.Place(body, Theme.Dp(20), Theme.Dp(92), focusW - Theme.Dp(40), Theme.Dp(88));
                    card.Controls.Add(body);
                }
                else
                {
                    M3Card card = new M3Card();
                    int cardH = Theme.Dp(240);
                    int cardY = Math.Max(Theme.Dp(24), (content.Height - cardH) / 2);
                    Widgets.Place(card, focusX, cardY, focusW, cardH);
                    content.Controls.Add(card);

                    card.Paint += delegate(object s, PaintEventArgs e)
                    {
                        Graphics g = e.Graphics;
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        RectangleF circle = new RectangleF(Theme.Dp(20), Theme.Dp(20), Theme.Dp(56), Theme.Dp(56));
                        Theme.FillRound(g, Theme.ErrorContainer, circle, circle.Width / 2f);
                        TextRenderer.DrawText(g, "!", Theme.TitleLarge, Rectangle.Round(circle), Theme.OnErrorContainer,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                    };

                    M3Text title = Widgets.Label(L10n.T("err.title"), Theme.HeadlineSmall, Theme.OnSurface, Theme.SurfaceContainer);
                    Widgets.Place(title, Theme.Dp(92), Theme.Dp(20), focusW - Theme.Dp(112), Theme.Dp(32));
                    card.Controls.Add(title);

                    M3Text detail = Widgets.Label(errorText, Theme.BodySmall, Theme.OnSurfaceVariant, Theme.SurfaceContainer);
                    detail.Wrapped = true;
                    Widgets.Place(detail, Theme.Dp(20), Theme.Dp(92), focusW - Theme.Dp(40), Theme.Dp(132));
                    card.Controls.Add(detail);
                }
            }
        }

        private void BuildLanguageCard(int x, int y, int width)
        {
            M3Card card = new M3Card();
            Widgets.Place(card, x, y, width, Theme.Dp(212));
            content.Controls.Add(card);

            int inner = width - Theme.Dp(32);

            M3Text namesLabel = Widgets.Label(L10n.T("lang.names"), Theme.LabelLarge, Theme.OnSurface, Theme.SurfaceContainer);
            Widgets.Place(namesLabel, Theme.Dp(16), Theme.Dp(14), inner, Theme.Dp(20));
            card.Controls.Add(namesLabel);

            M3Segmented segNames = new M3Segmented();
            segNames.Values = new string[] { "system", "en" };
            segNames.Labels = new string[] { L10n.T("lang.system"), L10n.T("lang.english") };
            segNames.Selected = namesLanguage == "en" ? "en" : "system";
            segNames.BackColor = Theme.SurfaceContainer;
            Widgets.Place(segNames, Theme.Dp(16), Theme.Dp(38), inner, Theme.Dp(40));
            segNames.SelectionChanged += OnNamesLanguageChanged;
            card.Controls.Add(segNames);

            M3Text namesHint = Widgets.Label(L10n.T("lang.names.hint"), Theme.BodySmall, Theme.OnSurfaceVariant, Theme.SurfaceContainer);
            Widgets.Place(namesHint, Theme.Dp(16), Theme.Dp(84), inner, Theme.Dp(20));
            card.Controls.Add(namesHint);

            M3Text paramsLabel = Widgets.Label(L10n.T("lang.params"), Theme.LabelLarge, Theme.OnSurface, Theme.SurfaceContainer);
            Widgets.Place(paramsLabel, Theme.Dp(16), Theme.Dp(116), inner, Theme.Dp(20));
            card.Controls.Add(paramsLabel);

            M3Segmented segParams = new M3Segmented();
            segParams.Values = new string[] { "system", "zh", "en" };
            segParams.Labels = new string[] { L10n.T("lang.system"), L10n.T("lang.chinese"), L10n.T("lang.english") };
            segParams.Selected = paramsLanguage;
            segParams.BackColor = Theme.SurfaceContainer;
            Widgets.Place(segParams, Theme.Dp(16), Theme.Dp(140), inner, Theme.Dp(40));
            segParams.SelectionChanged += OnParamsLanguageChanged;
            card.Controls.Add(segParams);

            M3Text paramsHint = Widgets.Label(L10n.T("lang.params.hint"), Theme.BodySmall, Theme.OnSurfaceVariant, Theme.SurfaceContainer);
            Widgets.Place(paramsHint, Theme.Dp(16), Theme.Dp(186), inner, Theme.Dp(20));
            card.Controls.Add(paramsHint);
        }

        private void OnNamesLanguageChanged(object sender, EventArgs e)
        {
            M3Segmented seg = (M3Segmented)sender;
            namesLanguage = seg.Selected == "en" ? "en" : "system";
            ApplyNames();
        }

        private void OnParamsLanguageChanged(object sender, EventArgs e)
        {
            M3Segmented seg = (M3Segmented)sender;
            paramsLanguage = seg.Selected;
            UpdateFooterState();
        }

        // Re-label the effect rows and group headers without rebuilding the window
        // (a full rebuild is what used to flash on every click).
        private void ApplyNames()
        {
            string names = L10n.ResolveNames(namesLanguage);
            for (int i = 0; i < groupLabels.Count; i++)
            {
                groupLabels[i].Text = L10n.GroupName(i == 0 ? "deform" : "vfx", names);
            }
            foreach (ComponentRow row in rows)
            {
                row.EffectName = L10n.EffectName(row.ComponentId, names);
            }
            UpdateFooterState();
        }

        private void BuildComponentsCard(int x, int y, int width)
        {
            M3Card card = new M3Card();
            Widgets.Place(card, x, y, width, Theme.Dp(168));
            content.Controls.Add(card);

            int colW = (width - Theme.Dp(48)) / 2;
            int leftX = Theme.Dp(16);
            int rightX = leftX + colW + Theme.Dp(16);

            string names = L10n.ResolveNames(namesLanguage);
            M3Text left = Widgets.Label(L10n.GroupName("deform", names), Theme.LabelLarge, Theme.Primary, Theme.SurfaceContainer);
            Widgets.Place(left, leftX, Theme.Dp(14), colW, Theme.Dp(20));
            card.Controls.Add(left);
            groupLabels.Add(left);

            M3Text right = Widgets.Label(L10n.GroupName("vfx", names), Theme.LabelLarge, Theme.Primary, Theme.SurfaceContainer);
            Widgets.Place(right, rightX, Theme.Dp(14), colW, Theme.Dp(20));
            card.Controls.Add(right);
            groupLabels.Add(right);

            AddRow(card, leftX, Theme.Dp(40), colW, "shake");
            AddRow(card, leftX, Theme.Dp(96), colW, "shrink");
            AddRow(card, rightX, Theme.Dp(40), colW, "shadow");
            AddRow(card, rightX, Theme.Dp(96), colW, "lightmix");
        }

        private void AddRow(M3Card card, int x, int y, int width, string id)
        {
            ComponentInfo info = components.Find(delegate(ComponentInfo c) { return c.Id == id; });
            if (info == null) return;

            ComponentRow row = new ComponentRow();
            row.ComponentId = id;
            row.EffectName = info.Name(L10n.ResolveNames(namesLanguage));
            row.Version = info.Version;
            row.Installed = state.Has(id);
            row.Checked = info.Selected;
            row.BackColor = Theme.SurfaceContainer;
            Widgets.Place(row, x, y, width, Theme.Dp(56));
            row.CheckedChanged += delegate(object sender, EventArgs e)
            {
                ComponentRow r = (ComponentRow)sender;
                info.Selected = r.Checked;
                UpdateFooterState();
            };
            rows.Add(row);
            card.Controls.Add(row);
        }

        private void BuildDestinationCard(int x, int y, int width)
        {
            M3Card card = new M3Card();
            Widgets.Place(card, x, y, width, Theme.Dp(96));
            content.Controls.Add(card);

            M3Text label = Widgets.Label(L10n.T("dest.title"), Theme.LabelLarge, Theme.OnSurface, Theme.SurfaceContainer);
            Widgets.Place(label, Theme.Dp(16), Theme.Dp(14), width - Theme.Dp(32) - Theme.Dp(92), Theme.Dp(20));
            card.Controls.Add(label);

            M3Button change = new M3Button();
            change.Style = M3ButtonStyle.Text;
            change.Text = L10n.T("dest.change");
            change.BackColor = Theme.SurfaceContainer;
            change.FitWidth();
            change.Height = Theme.Dp(36);
            change.Location = new Point(width - Theme.Dp(16) - change.Width, Theme.Dp(8));
            change.Click += delegate { ChooseFolder(); };
            card.Controls.Add(change);

            pathLabel = Widgets.Label(root, Theme.BodySmall, Theme.OnSurfaceVariant, Theme.SurfaceContainer);
            Widgets.Place(pathLabel, Theme.Dp(16), Theme.Dp(40), width - Theme.Dp(32) - change.Width - Theme.Dp(8), Theme.Dp(20));
            card.Controls.Add(pathLabel);

            destHint = Widgets.Label(Directory.Exists(root) ? L10n.T("dest.hint") : L10n.T("dest.newFolder"),
                Theme.BodySmall, Theme.OnSurfaceVariant, Theme.SurfaceContainer);
            Widgets.Place(destHint, Theme.Dp(16), Theme.Dp(64), width - Theme.Dp(32), Theme.Dp(20));
            card.Controls.Add(destHint);
        }

        private void ChooseFolder()
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = L10n.T("dest.title");
                dialog.SelectedPath = root;
                dialog.ShowNewFolderButton = true;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    root = dialog.SelectedPath;
                    // Update in place: rebuilding here made the whole window flash.
                    if (pathLabel != null) pathLabel.Text = root;
                    if (destHint != null)
                    {
                        destHint.Text = Directory.Exists(root) ? L10n.T("dest.hint") : L10n.T("dest.newFolder");
                    }
                }
            }
        }

        // ---------------- footer ----------------

        private void BuildFooterChildren()
        {
            int h = Theme.Dp(40);
            int y = (Theme.Dp(88) - h) / 2;
            int right = footer.Width - Theme.Dp(24);

            if (mode == Mode.Done)
            {
                primaryButton = MakePrimary(L10n.T("btn.close"), h);
                primaryButton.Click += delegate { Close(); };
                footer.Controls.Add(primaryButton);

                M3Button open = new M3Button();
                open.Style = M3ButtonStyle.Text;
                open.Text = L10n.T("btn.open");
                open.BackColor = Theme.Surface;
                open.Height = h;
                open.FitWidth();
                open.Location = new Point(primaryButton.Left - Theme.Dp(8) - open.Width, y);
                open.Click += delegate { InstallerCore.OpenFolder(root); };
                footer.Controls.Add(open);
            }
            else if (mode == Mode.Error)
            {
                M3Button retry = new M3Button();
                retry.Style = M3ButtonStyle.Text;
                retry.Text = L10n.T("btn.retry");
                retry.BackColor = Theme.Surface;
                retry.Height = h;
                retry.FitWidth();
                retry.Location = new Point(right - retry.Width, y);
                retry.Click += delegate
                {
                    mode = Mode.Configure;
                    Rebuild();
                };
                footer.Controls.Add(retry);

                primaryButton = null;
            }
            else if (mode == Mode.Working)
            {
                // The progress card owns the content area; the footer stays empty
                // so the running operation cannot be cancelled half-way.
            }
            else
            {
                primaryButton = MakePrimary(state.Installed ? L10n.T("btn.apply") : L10n.T("btn.install"), h);
                primaryButton.Click += delegate { StartInstall(); };
                footer.Controls.Add(primaryButton);

                M3Button cancel = new M3Button();
                cancel.Style = M3ButtonStyle.Text;
                cancel.Text = L10n.T("btn.cancel");
                cancel.BackColor = Theme.Surface;
                cancel.Height = h;
                cancel.FitWidth();
                cancel.Location = new Point(right - primaryButton.Width - Theme.Dp(8) - cancel.Width, y);
                cancel.Click += delegate { Close(); };
                footer.Controls.Add(cancel);

                if (state.Installed)
                {
                    uninstallButton = new M3Button();
                    uninstallButton.Style = M3ButtonStyle.TextDanger;
                    uninstallButton.Text = L10n.T("btn.uninstall");
                    uninstallButton.BackColor = Theme.Surface;
                    uninstallButton.Height = h;
                    uninstallButton.FitWidth();
                    uninstallButton.Location = new Point(Theme.Dp(24), y);
                    uninstallButton.Click += delegate { StartUninstall(); };
                    footer.Controls.Add(uninstallButton);
                }
                AddFooterState(y, h);
            }

            UpdateFooterState();
        }

        private M3Button MakePrimary(string text, int height)
        {
            M3Button button = new M3Button();
            button.Style = M3ButtonStyle.Filled;
            button.Text = text;
            button.BackColor = Theme.Surface;
            button.Height = height;
            button.FitWidth();
            button.Location = new Point(footer.Width - Theme.Dp(24) - button.Width, (Theme.Dp(88) - height) / 2);
            button.AccessibleName = text;
            return button;
        }

        private void AddFooterState(int y, int h)
        {
            int x = state.Installed && uninstallButton != null
                ? uninstallButton.Location.X + uninstallButton.Width + Theme.Dp(12)
                : Theme.Dp(24);
            footerState = Widgets.Label("", Theme.LabelSmall, Theme.OnSurfaceVariant, Theme.Surface);
            Widgets.Place(footerState, x, y, Theme.Dp(420), h);
            footer.Controls.Add(footerState);
        }

        private void UpdateFooterState()
        {
            if (footerState == null) return;
            if (state.Installed)
            {
                string detail = L10n.F("state.detail",
                    L10n.NamesLabel(L10n.ResolveNames(namesLanguage)),
                    L10n.ParamsLabel(L10n.ResolveParams(paramsLanguage)));
                footerState.Text = L10n.F("state.installed", state.SuiteVersion) + " · " + detail;
            }
            else
            {
                footerState.Text = "";
            }
        }

        // ---------------- actions ----------------

        private bool AnySelected()
        {
            foreach (ComponentInfo c in components)
            {
                if (c.Selected) return true;
            }
            return false;
        }

        private InstallOptions BuildOptions()
        {
            InstallOptions o = new InstallOptions();
            o.Root = root;
            o.NamesLanguage = namesLanguage;
            o.ParamsLanguage = paramsLanguage;
            o.Components = components;
            return o;
        }

        private void StartInstall()
        {
            if (working) return;
            if (!AnySelected())
            {
                errorText = L10n.T("lang.names.hint");
                return;
            }
            InstallOptions options = BuildOptions();
            working = true;
            mode = Mode.Working;
            Rebuild();

            Thread thread = new Thread(delegate() { RunInstall(options); });
            thread.IsBackground = true;
            thread.Start();
        }

        private void RunInstall(InstallOptions options)
        {
            InstallReport report = InstallerCore.Install(options, ReportProgress);
            try
            {
                BeginInvoke(new Action(delegate()
                {
                    working = false;
                    if (report.Ok)
                    {
                        state = InstallerCore.Detect(root);
                        doneTitle = L10n.T("done.title");
                        doneBody = L10n.T("done.body");
                        mode = Mode.Done;
                    }
                    else
                    {
                        errorText = report.Error;
                        mode = Mode.Error;
                    }
                    Rebuild();
                }));
            }
            catch { }
        }

        private void ReportProgress(float value, string text)
        {
            if (IsDisposed) return;
            try
            {
                BeginInvoke(new Action(delegate()
                {
                    if (bar != null) bar.Value = value;
                    if (statusLabel != null) statusLabel.Text = text;
                }));
            }
            catch { }
        }

        private void StartUninstall()
        {
            if (working) return;
            M3DialogForm dialog = new M3DialogForm(L10n.T("unin.title"), L10n.T("unin.body"),
                L10n.T("btn.cancel"), L10n.T("btn.uninstall"));
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            working = true;
            mode = Mode.Working;
            Rebuild();

            Thread thread = new Thread(delegate()
            {
                InstallReport report = InstallerCore.Uninstall(root, ReportProgress);
                try
                {
                    BeginInvoke(new Action(delegate()
                    {
                        working = false;
                        if (report.Ok)
                        {
                            state = InstallerCore.Detect(root);
                            doneTitle = L10n.T("unin.done");
                            doneBody = "";
                            mode = Mode.Done;
                        }
                        else
                        {
                            errorText = report.Error;
                            mode = Mode.Error;
                        }
                        Rebuild();
                    }));
                }
                catch { }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        // ---------------- keyboard ----------------

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && !working)
            {
                Close();
                return true;
            }
            if ((keyData == Keys.Enter) && mode == Mode.Configure && primaryButton != null && !working)
            {
                StartInstall();
                return true;
            }
            if ((keyData == Keys.Enter) && mode == Mode.Done)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (primaryButton != null) primaryButton.Focus();
        }
    }
}
