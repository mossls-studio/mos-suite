// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 MOSSLS

// InstallerForm.cs — Material Design 3 Expressive step-by-step wizard installer.
// Features Expanded Navigation Rail with spring-sliding active pill, Shared Axis X
// bitmap-composited page transitions, Connected Button Groups, animated effect cards,
// and animated completion feedback.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace MosSuite
{
    internal sealed class InstallerForm : Form
    {
        private enum InstallOutcome { Working, Done, Error }

        private readonly float dpiScale;
        private string uiLang = L10n.SystemLanguage();
        private string namesLanguage = "system";
        private string paramsLanguage = "system";
        private string root = InstallerCore.DefaultRoot();

        private readonly List<ComponentInfo> components = InstallerCore.Components();
        private InstallState state = new InstallState();

        // Wizard step: 0 Welcome, 1 Language, 2 Effects, 3 Location, 4 Install/Result
        private int step = 0;
        private int maxUnlocked = 3;
        private InstallOutcome outcome = InstallOutcome.Working;
        private bool working = false;
        private bool lastWasUninstall = false;
        private int lastInstalledCount = 0;
        private bool lastInstalledHelper = false;
        private string errorText = "";

        // Chrome & layout panels
        private M3NavRail rail;
        private M3ConnectedButtonGroup uiLangToggle;
        private Panel topBar;
        private M3IconButton minButton;
        private M3IconButton closeButton;
        private TransitionStage stage;
        private Panel footer;
        private M3Button backButton;
        private M3Button secondaryButton;
        private M3Button primaryButton;

        // Live references inside active page
        private M3Text langPreviewValue;
        private M3Text effectsCountChip;
        private M3Button toggleAllBtn;
        private readonly List<M3EffectCard> effectCards = new List<M3EffectCard>();
        private M3Text pathValueLabel;
        private M3Text pathStatusLabel;
        private M3ProgressBar progressBar;
        private M3Text statusLabel;
        private M3Spinner spinner;

        private const int RailW = 220;
        private const int TopH = 56;
        private const int FooterH = 76;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

        public InstallerForm() : this(null, null, null) { }

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
            Icon = SystemIcons.Application;

            float scale = 1f;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) scale = g.DpiX / 96f;
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
            BackColor = Theme.Surface;
            Font = Theme.BodyMedium;
            ClientSize = new Size(Theme.Dp(840), Theme.Dp(592));

            BuildShell();
            ShowStep(0, 0, false);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000;   // CS_DROPSHADOW
                cp.ExStyle |= 0x02000000;   // WS_EX_COMPOSITED
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

        // ---------------- Shell ----------------

        private void BuildShell()
        {
            SuspendLayout();
            Controls.Clear();

            int railW = Theme.Dp(RailW);
            int topH = Theme.Dp(TopH);
            int footH = Theme.Dp(FooterH);
            int rightW = ClientSize.Width - railW;
            int stageH = ClientSize.Height - topH - footH;

            // Left M3 Expanded Navigation Rail
            rail = new M3NavRail();
            rail.SetBounds(0, 0, railW, ClientSize.Height);
            rail.MaxUnlockedIndex = maxUnlocked;
            rail.StepClicked += OnRailStepClicked;
            rail.MouseDown += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left && e.Y < Theme.Dp(88)) DragWindow(); };
            Controls.Add(rail);

            // UI Language quick toggle pinned at bottom of rail
            uiLangToggle = new M3ConnectedButtonGroup();
            uiLangToggle.Compact = true;
            uiLangToggle.Values = new string[] { "zh", "en" };
            uiLangToggle.Labels = new string[] { "中文", "EN" };
            uiLangToggle.Selected = uiLang;
            uiLangToggle.BackColor = Theme.SurfaceLow;
            uiLangToggle.SetBounds(Theme.Dp(16), ClientSize.Height - Theme.Dp(52), railW - Theme.Dp(32), Theme.Dp(32));
            uiLangToggle.SnapState();
            uiLangToggle.SelectionChanged += OnUiLangChanged;
            rail.Controls.Add(uiLangToggle);

            // Right Top Bar
            topBar = new Panel();
            topBar.BackColor = Theme.Surface;
            topBar.SetBounds(railW, 0, rightW, topH);
            topBar.Paint += PaintTopBar;
            topBar.MouseDown += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) DragWindow(); };
            Controls.Add(topBar);

            closeButton = new M3IconButton();
            closeButton.Icon = M3IconButton.Glyph.Close;
            closeButton.BackColor = Theme.Surface;
            closeButton.Location = new Point(rightW - Theme.Dp(16) - Theme.Dp(40), Theme.Dp(8));
            closeButton.Click += delegate { if (!working) Close(); };
            topBar.Controls.Add(closeButton);

            minButton = new M3IconButton();
            minButton.Icon = M3IconButton.Glyph.Minimize;
            minButton.BackColor = Theme.Surface;
            minButton.Location = new Point(closeButton.Left - Theme.Dp(4) - Theme.Dp(40), Theme.Dp(8));
            minButton.Click += delegate { WindowState = FormWindowState.Minimized; };
            topBar.Controls.Add(minButton);

            // Main Animated Viewport Stage
            stage = new TransitionStage();
            stage.BackColor = Theme.Surface;
            stage.SetBounds(railW, topH, rightW, stageH);
            Controls.Add(stage);

            // Right Bottom Action Bar
            footer = new Panel();
            footer.BackColor = Theme.Surface;
            footer.SetBounds(railW, ClientSize.Height - footH, rightW, footH);
            footer.Paint += PaintFooter;
            Controls.Add(footer);

            backButton = new M3Button();
            backButton.Style = M3ButtonStyle.Text;
            backButton.BackColor = Theme.Surface;
            backButton.Click += OnBackClick;
            footer.Controls.Add(backButton);

            secondaryButton = new M3Button();
            secondaryButton.Style = M3ButtonStyle.Tonal;
            secondaryButton.BackColor = Theme.Surface;
            secondaryButton.Click += OnSecondaryClick;
            footer.Controls.Add(secondaryButton);

            primaryButton = new M3Button();
            primaryButton.Style = M3ButtonStyle.Filled;
            primaryButton.BackColor = Theme.Surface;
            primaryButton.Click += OnPrimaryClick;
            footer.Controls.Add(primaryButton);

            RefreshRailLabels();
            ResumeLayout(true);
        }

        private void RefreshRailLabels()
        {
            rail.Labels = new string[] {
                L10n.T("step.welcome"),
                L10n.T("step.language"),
                L10n.T("step.effects"),
                L10n.T("step.location"),
                L10n.T("step.install")
            };
            rail.Invalidate();
        }

        private void OnUiLangChanged(object sender, EventArgs e)
        {
            if (working) return;
            uiLang = uiLangToggle.Selected;
            L10n.UiLang = uiLang;
            Theme.Init(dpiScale, uiLang == "zh");
            Font = Theme.BodyMedium;
            RefreshRailLabels();
            topBar.Invalidate();
            ShowStep(step, 0, false);
        }

        private void OnRailStepClicked(int targetStep)
        {
            if (working || targetStep == step) return;
            int dir = targetStep > step ? 1 : -1;
            ShowStep(targetStep, dir, true);
        }

        private void PaintTopBar(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            if (step < 4)
            {
                string counter = L10n.F("step.counter", step + 1);
                int cw = Theme.Measure(counter, Theme.LabelMedium) + Theme.Dp(20);
                RectangleF chip = new RectangleF(Theme.Dp(28), (topBar.Height - Theme.Dp(24)) * 0.5f, cw, Theme.Dp(24));
                Theme.FillRound(g, Theme.SurfaceContainer, chip, Theme.Dpf(Theme.RadiusSmall));
                TextRenderer.DrawText(g, counter, Theme.LabelMedium, Rectangle.Round(chip), Theme.OnSurfaceVariant,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            }
        }

        private void PaintFooter(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Theme.OutlineVariant, Theme.Dpf(1f)))
            {
                e.Graphics.DrawLine(pen, Theme.Dp(24), 0, footer.Width - Theme.Dp(24), 0);
            }
        }

        // ---------------- Step Navigation ----------------

        private void ShowStep(int targetStep, int direction, bool animate)
        {
            step = targetStep;
            rail.Locked = working || (step == 4 && outcome == InstallOutcome.Working);
            rail.SetActive(step, !animate);
            topBar.Invalidate();

            Panel nextPage = BuildStepPage(step);
            stage.ShowPage(nextPage, direction, animate);
            UpdateFooter();
        }

        private void UpdateFooter()
        {
            int pad = Theme.Dp(28);
            int y = (footer.Height - Theme.Dp(40)) / 2;

            bool showBack = false;
            bool showSecondary = false;
            bool showPrimary = true;

            primaryButton.Enabled = true;
            primaryButton.ShowArrow = false;
            primaryButton.Font = Theme.LabelLarge;
            backButton.Font = Theme.LabelLarge;
            secondaryButton.Font = Theme.LabelLarge;

            if (step == 0)
            {
                primaryButton.Text = L10n.T("btn.start");
                primaryButton.ShowArrow = true;
                if (state.Installed)
                {
                    showSecondary = true;
                    secondaryButton.Style = M3ButtonStyle.Text;
                    secondaryButton.Text = L10n.T("btn.uninstall");
                }
            }
            else if (step == 1)
            {
                showBack = true;
                backButton.Text = L10n.T("btn.back");
                primaryButton.Text = L10n.T("btn.next");
                primaryButton.ShowArrow = true;
            }
            else if (step == 2)
            {
                showBack = true;
                backButton.Text = L10n.T("btn.back");
                primaryButton.Text = L10n.T("btn.next");
                primaryButton.ShowArrow = true;
                primaryButton.Enabled = SelectedCount() > 0;
            }
            else if (step == 3)
            {
                showBack = true;
                backButton.Text = L10n.T("btn.back");
                primaryButton.Text = state.Installed ? L10n.T("btn.apply") : L10n.T("btn.install");
                primaryButton.Enabled = SelectedCount() > 0;
            }
            else
            {
                if (outcome == InstallOutcome.Working)
                {
                    showPrimary = false;
                }
                else if (outcome == InstallOutcome.Done)
                {
                    showSecondary = !lastWasUninstall;
                    secondaryButton.Style = M3ButtonStyle.Tonal;
                    secondaryButton.Text = L10n.T("btn.open");
                    primaryButton.Text = L10n.T("btn.close");
                }
                else
                {
                    showBack = true;
                    backButton.Text = L10n.T("btn.back");
                    primaryButton.Text = L10n.T("btn.retry");
                }
            }

            backButton.Visible = showBack;
            secondaryButton.Visible = showSecondary;
            primaryButton.Visible = showPrimary;

            primaryButton.FitWidth();
            primaryButton.Location = new Point(footer.Width - pad - primaryButton.Width, y);

            if (showSecondary)
            {
                secondaryButton.FitWidth();
                if (step == 0)
                {
                    secondaryButton.Location = new Point(pad, y);
                }
                else
                {
                    secondaryButton.Location = new Point(primaryButton.Left - Theme.Dp(10) - secondaryButton.Width, y);
                }
            }

            if (showBack)
            {
                backButton.FitWidth();
                backButton.Location = new Point(pad, y);
            }

            closeButton.Enabled = !working;
            uiLangToggle.Enabled = !working;
            footer.Invalidate(true);
        }

        private void OnBackClick(object sender, EventArgs e)
        {
            if (working) return;
            if (step == 4)
            {
                ShowStep(3, -1, true);
            }
            else if (step > 0)
            {
                ShowStep(step - 1, -1, true);
            }
        }

        private void OnSecondaryClick(object sender, EventArgs e)
        {
            if (working) return;
            if (step == 0 && state.Installed)
            {
                using (M3DialogForm dlg = new M3DialogForm(L10n.T("unin.title"), L10n.T("unin.body"), L10n.T("btn.cancel"), L10n.T("btn.uninstall")))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        StartUninstall();
                    }
                }
            }
            else if (step == 4 && outcome == InstallOutcome.Done)
            {
                InstallerCore.OpenFolder(root);
            }
        }

        private void OnPrimaryClick(object sender, EventArgs e)
        {
            if (working) return;
            if (step < 3)
            {
                ShowStep(step + 1, 1, true);
            }
            else if (step == 3)
            {
                StartInstall();
            }
            else if (step == 4)
            {
                if (outcome == InstallOutcome.Done) Close();
                else if (outcome == InstallOutcome.Error)
                {
                    if (lastWasUninstall) StartUninstall();
                    else StartInstall();
                }
            }
        }

        // ---------------- Step Page Builders ----------------

        private Panel BuildStepPage(int s)
        {
            Panel p = new Panel();
            p.BackColor = Theme.Surface;
            p.Size = stage.Size;

            if (s == 0) BuildWelcomePage(p);
            else if (s == 1) BuildLanguagePage(p);
            else if (s == 2) BuildEffectsPage(p);
            else if (s == 3) BuildLocationPage(p);
            else BuildInstallPage(p);

            return p;
        }

        // Step 0: Welcome
        private void BuildWelcomePage(Panel p)
        {
            int pad = Theme.Dp(28);
            int innerW = p.Width - pad * 2;

            M3Text title = Widgets.Label(L10n.T("welcome.title"), Theme.HeadlineSmall, Theme.OnSurface, Theme.Surface);
            Widgets.Place(title, pad, Theme.Dp(8), innerW, Theme.Dp(34));
            p.Controls.Add(title);

            M3Text body = Widgets.Label(L10n.T("welcome.body"), Theme.BodyMedium, Theme.OnSurfaceVariant, Theme.Surface);
            body.Wrapped = true;
            Widgets.Place(body, pad, Theme.Dp(48), innerW, Theme.Dp(46));
            p.Controls.Add(body);

            // Overview card with 5 effect groups
            M3Card card = new M3Card();
            card.FillColor = Theme.SurfaceContainer;
            Widgets.Place(card, pad, Theme.Dp(106), innerW, Theme.Dp(224));
            p.Controls.Add(card);

            string[] groups = {
                L10n.T("welcome.group.deform"),
                L10n.T("welcome.group.liquid"),
                L10n.T("welcome.group.vfx"),
                L10n.T("welcome.group.effects"),
                L10n.T("welcome.group.blur")
            };
            Color[] dots = { Theme.Primary, Theme.Tertiary, Theme.Secondary, Theme.Tertiary, Theme.Primary };

            card.Paint += delegate(object s, PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                for (int i = 0; i < groups.Length; i++)
                {
                    int rowY = Theme.Dp(14) + i * Theme.Dp(40);
                    RectangleF dot = new RectangleF(Theme.Dp(20), rowY + Theme.Dp(10), Theme.Dp(8), Theme.Dp(8));
                    Theme.FillRound(g, dots[i], dot, Theme.Dpf(4f));

                    Rectangle tr = new Rectangle(Theme.Dp(40), rowY, innerW - Theme.Dp(60), Theme.Dp(28));
                    TextRenderer.DrawText(g, groups[i], Theme.BodyMedium, tr, Theme.OnSurface,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

                    if (i < groups.Length - 1)
                    {
                        using (Pen pen = new Pen(Theme.OutlineVariant, Theme.Dpf(1f)))
                        {
                            g.DrawLine(pen, Theme.Dp(40), rowY + Theme.Dp(34), innerW - Theme.Dp(20), rowY + Theme.Dp(34));
                        }
                    }
                }
            };

            // Status banner
            M3Card statusCard = new M3Card();
            statusCard.FillColor = state.Installed ? Theme.Blend(Theme.SurfaceContainer, Theme.PrimaryContainer, 0.45f) : Theme.SurfaceLow;
            statusCard.BorderColor = state.Installed ? Color.Transparent : Theme.OutlineVariant;
            Widgets.Place(statusCard, pad, Theme.Dp(344), innerW, Theme.Dp(54));
            p.Controls.Add(statusCard);

            string statusText = state.Installed
                ? L10n.F("welcome.installed",
                    string.IsNullOrEmpty(state.SuiteVersion) ? "1.x" : state.SuiteVersion,
                    L10n.NamesLabel(L10n.ResolveNames(state.NamesLanguage)),
                    L10n.ParamsLabel(L10n.ResolveParams(state.ParamsLanguage)))
                : L10n.T("welcome.fresh");

            M3Text st = Widgets.Label(statusText, Theme.BodyMedium, state.Installed ? Theme.OnPrimaryContainer : Theme.OnSurfaceVariant, statusCard.FillColor);
            Widgets.Place(st, Theme.Dp(16), Theme.Dp(12), innerW - Theme.Dp(32), Theme.Dp(30));
            statusCard.Controls.Add(st);
        }

        // Step 1: Language
        private void BuildLanguagePage(Panel p)
        {
            int pad = Theme.Dp(28);
            int innerW = p.Width - pad * 2;

            M3Text title = Widgets.Label(L10n.T("lang.title"), Theme.HeadlineSmall, Theme.OnSurface, Theme.Surface);
            Widgets.Place(title, pad, Theme.Dp(8), innerW, Theme.Dp(34));
            p.Controls.Add(title);

            M3Text body = Widgets.Label(L10n.T("lang.body"), Theme.BodyMedium, Theme.OnSurfaceVariant, Theme.Surface);
            body.Wrapped = true;
            Widgets.Place(body, pad, Theme.Dp(46), innerW, Theme.Dp(36));
            p.Controls.Add(body);

            // Card 1: Effect names
            M3Card c1 = new M3Card();
            c1.FillColor = Theme.SurfaceContainer;
            Widgets.Place(c1, pad, Theme.Dp(96), innerW, Theme.Dp(118));
            p.Controls.Add(c1);

            M3Text nLbl = Widgets.Label(L10n.T("lang.names"), Theme.TitleSmall, Theme.OnSurface, Theme.SurfaceContainer);
            Widgets.Place(nLbl, Theme.Dp(18), Theme.Dp(14), innerW - Theme.Dp(36), Theme.Dp(22));
            c1.Controls.Add(nLbl);

            M3ConnectedButtonGroup grpNames = new M3ConnectedButtonGroup();
            grpNames.Values = new string[] { "system", "en" };
            grpNames.Labels = new string[] { L10n.T("lang.system"), L10n.T("lang.english") };
            grpNames.Selected = namesLanguage == "en" ? "en" : "system";
            grpNames.BackColor = Theme.SurfaceContainer;
            Widgets.Place(grpNames, Theme.Dp(18), Theme.Dp(42), innerW - Theme.Dp(36), Theme.Dp(40));
            grpNames.SnapState();
            grpNames.SelectionChanged += delegate
            {
                namesLanguage = grpNames.Selected;
                RefreshLangPreview();
            };
            c1.Controls.Add(grpNames);

            M3Text nHint = Widgets.Label(L10n.T("lang.names.hint"), Theme.BodySmall, Theme.OnSurfaceVariant, Theme.SurfaceContainer);
            Widgets.Place(nHint, Theme.Dp(18), Theme.Dp(88), innerW - Theme.Dp(36), Theme.Dp(20));
            c1.Controls.Add(nHint);

            // Card 2: Parameter language
            M3Card c2 = new M3Card();
            c2.FillColor = Theme.SurfaceContainer;
            Widgets.Place(c2, pad, Theme.Dp(226), innerW, Theme.Dp(118));
            p.Controls.Add(c2);

            M3Text pLbl = Widgets.Label(L10n.T("lang.params"), Theme.TitleSmall, Theme.OnSurface, Theme.SurfaceContainer);
            Widgets.Place(pLbl, Theme.Dp(18), Theme.Dp(14), innerW - Theme.Dp(36), Theme.Dp(22));
            c2.Controls.Add(pLbl);

            M3ConnectedButtonGroup grpParams = new M3ConnectedButtonGroup();
            grpParams.Values = new string[] { "system", "zh", "en" };
            grpParams.Labels = new string[] { L10n.T("lang.system"), L10n.T("lang.chinese"), L10n.T("lang.english") };
            grpParams.Selected = (paramsLanguage == "zh" || paramsLanguage == "en") ? paramsLanguage : "system";
            grpParams.BackColor = Theme.SurfaceContainer;
            Widgets.Place(grpParams, Theme.Dp(18), Theme.Dp(42), innerW - Theme.Dp(36), Theme.Dp(40));
            grpParams.SnapState();
            grpParams.SelectionChanged += delegate
            {
                paramsLanguage = grpParams.Selected;
                RefreshLangPreview();
            };
            c2.Controls.Add(grpParams);

            M3Text pHint = Widgets.Label(L10n.T("lang.params.hint"), Theme.BodySmall, Theme.OnSurfaceVariant, Theme.SurfaceContainer);
            Widgets.Place(pHint, Theme.Dp(18), Theme.Dp(88), innerW - Theme.Dp(36), Theme.Dp(20));
            c2.Controls.Add(pHint);

            // Live preview strip
            M3Card prev = new M3Card();
            prev.FillColor = Theme.SurfaceLow;
            prev.BorderColor = Theme.OutlineVariant;
            Widgets.Place(prev, pad, Theme.Dp(358), innerW, Theme.Dp(54));
            p.Controls.Add(prev);

            M3Text prevTag = Widgets.Label(L10n.T("lang.preview.label"), Theme.LabelMedium, Theme.Primary, Theme.SurfaceLow);
            Widgets.Place(prevTag, Theme.Dp(16), Theme.Dp(16), Theme.Dp(92), Theme.Dp(22));
            prev.Controls.Add(prevTag);

            langPreviewValue = Widgets.Label("", Theme.BodyMedium, Theme.OnSurface, Theme.SurfaceLow);
            Widgets.Place(langPreviewValue, Theme.Dp(112), Theme.Dp(16), innerW - Theme.Dp(128), Theme.Dp(22));
            prev.Controls.Add(langPreviewValue);
            RefreshLangPreview();
        }

        private void RefreshLangPreview()
        {
            if (langPreviewValue == null) return;
            string n = L10n.ResolveNames(namesLanguage);
            string pr = L10n.ResolveParams(paramsLanguage);
            string group = L10n.GroupName("liquid", n);
            string eff = L10n.EffectName("rain", n);
            string paramSample = pr == "zh" ? "雨滴密度 · 折射强度" : "Drop Density · Refraction";
            langPreviewValue.Text = group + "  ▸  " + eff + "   (" + paramSample + ")";
        }

        // Step 2: Effects Selection (2-column clean M3 cards)
        private void BuildEffectsPage(Panel p)
        {
            effectCards.Clear();
            int pad = Theme.Dp(28);
            int innerW = p.Width - pad * 2;

            M3Text title = Widgets.Label(L10n.T("effects.title"), Theme.HeadlineSmall, Theme.OnSurface, Theme.Surface);
            Widgets.Place(title, pad, Theme.Dp(8), innerW - Theme.Dp(190), Theme.Dp(34));
            p.Controls.Add(title);

            toggleAllBtn = new M3Button();
            toggleAllBtn.Style = M3ButtonStyle.Text;
            toggleAllBtn.BackColor = Theme.Surface;
            toggleAllBtn.Height = Theme.Dp(34);
            toggleAllBtn.Click += OnToggleAllEffects;
            p.Controls.Add(toggleAllBtn);

            effectsCountChip = Widgets.Label("", Theme.LabelMedium, Theme.Primary, Theme.Surface);
            effectsCountChip.Alignment = TextFormatFlags.Right | TextFormatFlags.VerticalCenter;
            Widgets.Place(effectsCountChip, pad + innerW - Theme.Dp(108), Theme.Dp(48), Theme.Dp(108), Theme.Dp(22));
            p.Controls.Add(effectsCountChip);

            M3Text body = Widgets.Label(L10n.T("effects.body"), Theme.BodyMedium, Theme.OnSurfaceVariant, Theme.Surface);
            Widgets.Place(body, pad, Theme.Dp(46), innerW - Theme.Dp(116), Theme.Dp(26));
            p.Controls.Add(body);

            string resolvedNames = L10n.ResolveNames(namesLanguage);
            int gapX = Theme.Dp(12);
            int gapY = Theme.Dp(8);
            int cardW = (innerW - gapX) / 2;
            int cardH = Theme.Dp(52);
            int startY = Theme.Dp(80);

            for (int i = 0; i < components.Count; i++)
            {
                ComponentInfo c = components[i];
                int col = i % 2;
                int row = i / 2;

                M3EffectCard card = new M3EffectCard(c.Selected);
                card.Title = c.Name(resolvedNames);
                card.Meta = L10n.GroupName(c.Group, resolvedNames) + " · v" + c.Version;
                card.Badge = c.IsHelper ? L10n.T("effects.helperTag") : L10n.T("effects.wasmTag");
                card.BadgePrimary = c.IsHelper;
                card.StatusTag = state.Has(c) ? L10n.T("effects.installedTag") : "";
                card.BackColor = Theme.Surface;
                Widgets.Place(card, pad + col * (cardW + gapX), startY + row * (cardH + gapY), cardW, cardH);

                ComponentInfo captured = c;
                card.Toggled += delegate
                {
                    captured.Selected = card.Selected;
                    RefreshEffectsHeader();
                    UpdateFooter();
                };
                p.Controls.Add(card);
                effectCards.Add(card);
            }

            RefreshEffectsHeader();
        }

        private void OnToggleAllEffects(object sender, EventArgs e)
        {
            bool selectAll = SelectedCount() < components.Count;
            for (int i = 0; i < components.Count; i++)
            {
                components[i].Selected = selectAll;
                if (i < effectCards.Count) effectCards[i].Selected = selectAll;
            }
            RefreshEffectsHeader();
            UpdateFooter();
        }

        private void RefreshEffectsHeader()
        {
            int count = SelectedCount();
            if (effectsCountChip != null)
            {
                effectsCountChip.Text = L10n.F("effects.count", count, components.Count);
            }
            if (toggleAllBtn != null)
            {
                toggleAllBtn.Text = count < components.Count ? L10n.T("effects.selectAll") : L10n.T("effects.selectNone");
                toggleAllBtn.FitWidth();
                toggleAllBtn.Location = new Point(stage.Width - Theme.Dp(28) - toggleAllBtn.Width, Theme.Dp(8));
            }
        }

        private int SelectedCount()
        {
            int n = 0;
            for (int i = 0; i < components.Count; i++) if (components[i].Selected) n++;
            return n;
        }

        private int SelectedHelperCount()
        {
            int n = 0;
            for (int i = 0; i < components.Count; i++) if (components[i].Selected && components[i].IsHelper) n++;
            return n;
        }

        // Step 3: Location & Summary
        private void BuildLocationPage(Panel p)
        {
            int pad = Theme.Dp(28);
            int innerW = p.Width - pad * 2;

            M3Text title = Widgets.Label(L10n.T("loc.title"), Theme.HeadlineSmall, Theme.OnSurface, Theme.Surface);
            Widgets.Place(title, pad, Theme.Dp(8), innerW, Theme.Dp(34));
            p.Controls.Add(title);

            M3Text body = Widgets.Label(L10n.T("loc.body"), Theme.BodyMedium, Theme.OnSurfaceVariant, Theme.Surface);
            body.Wrapped = true;
            Widgets.Place(body, pad, Theme.Dp(46), innerW, Theme.Dp(40));
            p.Controls.Add(body);

            // Folder card with M3 Outlined field (4dp radius)
            M3Card locCard = new M3Card();
            locCard.FillColor = Theme.SurfaceContainer;
            Widgets.Place(locCard, pad, Theme.Dp(100), innerW, Theme.Dp(126));
            p.Controls.Add(locCard);

            M3Text fLbl = Widgets.Label(L10n.T("loc.folder"), Theme.TitleSmall, Theme.OnSurface, Theme.SurfaceContainer);
            Widgets.Place(fLbl, Theme.Dp(18), Theme.Dp(14), innerW - Theme.Dp(36), Theme.Dp(22));
            locCard.Controls.Add(fLbl);

            M3Button browse = new M3Button();
            browse.Style = M3ButtonStyle.Outlined;
            browse.Text = L10n.T("loc.browse");
            browse.BackColor = Theme.SurfaceContainer;
            browse.Height = Theme.Dp(40);
            browse.FitWidth();
            browse.Location = new Point(innerW - Theme.Dp(18) - browse.Width, Theme.Dp(44));
            browse.Click += OnBrowseFolder;
            locCard.Controls.Add(browse);

            // M3 Outlined text field box (4dp radius per M3 spec)
            int fieldW = browse.Left - Theme.Dp(18) - Theme.Dp(12);
            M3Card pathBox = new M3Card();
            pathBox.FillColor = Theme.SurfaceLow;
            pathBox.BorderColor = Theme.Outline;
            pathBox.CornerDp = Theme.RadiusExtraSmall;
            Widgets.Place(pathBox, Theme.Dp(18), Theme.Dp(44), fieldW, Theme.Dp(40));
            locCard.Controls.Add(pathBox);

            pathBox.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Theme.DrawFolderIcon(e.Graphics, new RectangleF(Theme.Dp(10), Theme.Dp(11), Theme.Dp(18), Theme.Dp(18)), Theme.Primary, Theme.Dpf(1.6f));
            };

            pathValueLabel = Widgets.Label(root, Theme.BodyMedium, Theme.OnSurface, Theme.SurfaceLow);
            Widgets.Place(pathValueLabel, Theme.Dp(34), Theme.Dp(6), fieldW - Theme.Dp(42), Theme.Dp(28));
            pathBox.Controls.Add(pathValueLabel);

            pathStatusLabel = Widgets.Label("", Theme.BodySmall, Theme.OnSurfaceVariant, Theme.SurfaceContainer);
            Widgets.Place(pathStatusLabel, Theme.Dp(18), Theme.Dp(94), innerW - Theme.Dp(36), Theme.Dp(20));
            locCard.Controls.Add(pathStatusLabel);
            RefreshPathStatus();

            // Summary card
            M3Card sumCard = new M3Card();
            sumCard.FillColor = Theme.SurfaceLow;
            sumCard.BorderColor = Theme.OutlineVariant;
            Widgets.Place(sumCard, pad, Theme.Dp(242), innerW, Theme.Dp(172));
            p.Controls.Add(sumCard);

            M3Text sumTitle = Widgets.Label(L10n.T("loc.summary"), Theme.TitleSmall, Theme.Primary, Theme.SurfaceLow);
            Widgets.Place(sumTitle, Theme.Dp(18), Theme.Dp(16), innerW - Theme.Dp(36), Theme.Dp(22));
            sumCard.Controls.Add(sumTitle);

            string nLabel = L10n.NamesLabel(L10n.ResolveNames(namesLanguage));
            string pLabel = L10n.ParamsLabel(L10n.ResolveParams(paramsLanguage));
            M3Text sumLang = Widgets.Label(L10n.F("loc.summary.lang", nLabel, pLabel), Theme.BodyMedium, Theme.OnSurface, Theme.SurfaceLow);
            Widgets.Place(sumLang, Theme.Dp(18), Theme.Dp(46), innerW - Theme.Dp(36), Theme.Dp(24));
            sumCard.Controls.Add(sumLang);

            int selCount = SelectedCount();
            int helperCount = SelectedHelperCount();
            string countLine = helperCount > 0
                ? L10n.F("loc.summary.count", selCount, helperCount)
                : L10n.F("loc.summary.countWasmOnly", selCount);
            M3Text sumCount = Widgets.Label(countLine, Theme.BodyMedium, Theme.OnSurface, Theme.SurfaceLow);
            Widgets.Place(sumCount, Theme.Dp(18), Theme.Dp(74), innerW - Theme.Dp(36), Theme.Dp(24));
            sumCard.Controls.Add(sumCount);

            // Selected effect chips summary (wrapped so all 11 names remain visible)
            string resolvedNames = L10n.ResolveNames(namesLanguage);
            List<string> names = new List<string>();
            for (int i = 0; i < components.Count; i++) if (components[i].Selected) names.Add(components[i].Name(resolvedNames));
            M3Text sumList = Widgets.Label(string.Join(" · ", names.ToArray()), Theme.BodySmall, Theme.OnSurfaceVariant, Theme.SurfaceLow);
            sumList.Wrapped = true;
            Widgets.Place(sumList, Theme.Dp(18), Theme.Dp(104), innerW - Theme.Dp(36), Theme.Dp(56));
            sumCard.Controls.Add(sumList);
        }

        private void OnBrowseFolder(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.SelectedPath = Directory.Exists(root) ? root : InstallerCore.DefaultRoot();
                if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedPath))
                {
                    root = dlg.SelectedPath;
                    state = InstallerCore.Detect(root);
                    if (pathValueLabel != null) pathValueLabel.Text = root;
                    RefreshPathStatus();
                    UpdateFooter();
                }
            }
        }

        private void RefreshPathStatus()
        {
            if (pathStatusLabel == null) return;
            bool exists = Directory.Exists(Path.Combine(root, "Plug-ins")) || File.Exists(Path.Combine(root, "settings.json")) || state.Installed;
            pathStatusLabel.Text = exists ? L10n.T("loc.found") : L10n.T("loc.willCreate");
            pathStatusLabel.ForeColor = exists ? Theme.Primary : Theme.OnSurfaceVariant;
        }

        // Step 4: Working / Done / Error
        private void BuildInstallPage(Panel p)
        {
            int pad = Theme.Dp(28);
            int innerW = p.Width - pad * 2;
            progressBar = null;
            statusLabel = null;
            spinner = null;

            if (outcome == InstallOutcome.Working)
            {
                M3Card card = new M3Card();
                card.FillColor = Theme.SurfaceContainer;
                int cardH = Theme.Dp(196);
                Widgets.Place(card, pad, (p.Height - cardH) / 2 - Theme.Dp(12), innerW, cardH);
                p.Controls.Add(card);

                spinner = new M3Spinner();
                spinner.BackColor = Theme.SurfaceContainer;
                Widgets.Place(spinner, Theme.Dp(24), Theme.Dp(28), Theme.Dp(40), Theme.Dp(40));
                card.Controls.Add(spinner);

                string wTitle = lastWasUninstall ? L10n.T("work.uninstalling") : L10n.T("work.title");
                M3Text title = Widgets.Label(wTitle, Theme.TitleLarge, Theme.OnSurface, Theme.SurfaceContainer);
                Widgets.Place(title, Theme.Dp(78), Theme.Dp(24), innerW - Theme.Dp(102), Theme.Dp(30));
                card.Controls.Add(title);

                M3Text sub = Widgets.Label(L10n.T("work.sub"), Theme.BodySmall, Theme.OnSurfaceVariant, Theme.SurfaceContainer);
                Widgets.Place(sub, Theme.Dp(78), Theme.Dp(54), innerW - Theme.Dp(102), Theme.Dp(20));
                card.Controls.Add(sub);

                progressBar = new M3ProgressBar();
                progressBar.BackColor = Theme.SurfaceContainer;
                Widgets.Place(progressBar, Theme.Dp(24), Theme.Dp(108), innerW - Theme.Dp(48), Theme.Dp(6));
                card.Controls.Add(progressBar);

                statusLabel = Widgets.Label(L10n.T("progress.extract"), Theme.BodyMedium, Theme.OnSurfaceVariant, Theme.SurfaceContainer);
                Widgets.Place(statusLabel, Theme.Dp(24), Theme.Dp(132), innerW - Theme.Dp(48), Theme.Dp(36));
                card.Controls.Add(statusLabel);
            }
            else if (outcome == InstallOutcome.Done)
            {
                DoneHeroBadge badge = new DoneHeroBadge();
                badge.BackColor = Theme.Surface;
                Widgets.Place(badge, (p.Width - Theme.Dp(76)) / 2, Theme.Dp(28), Theme.Dp(76), Theme.Dp(76));
                p.Controls.Add(badge);

                string dTitle = lastWasUninstall ? L10n.T("unin.done") : L10n.T("done.title");
                M3Text title = Widgets.Label(dTitle, Theme.HeadlineSmall, Theme.OnSurface, Theme.Surface);
                title.Alignment = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
                Widgets.Place(title, pad, Theme.Dp(118), innerW, Theme.Dp(34));
                p.Controls.Add(title);

                string dBody = lastWasUninstall ? L10n.T("unin.doneBody") : L10n.F("done.body", lastInstalledCount);
                M3Text body = Widgets.Label(dBody, Theme.BodyLarge, Theme.OnSurfaceVariant, Theme.Surface);
                body.Alignment = TextFormatFlags.HorizontalCenter | TextFormatFlags.Top;
                body.Wrapped = true;
                Widgets.Place(body, pad + Theme.Dp(24), Theme.Dp(160), innerW - Theme.Dp(48), Theme.Dp(54));
                p.Controls.Add(body);

                if (!lastWasUninstall && lastInstalledHelper)
                {
                    M3Card tip = new M3Card();
                    tip.FillColor = Theme.SurfaceContainer;
                    tip.BorderColor = Theme.OutlineVariant;
                    Widgets.Place(tip, pad, Theme.Dp(234), innerW, Theme.Dp(108));
                    p.Controls.Add(tip);

                    M3Text tipText = Widgets.Label(L10n.T("done.helperTip"), Theme.BodyMedium, Theme.OnSurfaceVariant, Theme.SurfaceContainer);
                    tipText.Wrapped = true;
                    Widgets.Place(tipText, Theme.Dp(20), Theme.Dp(18), innerW - Theme.Dp(40), Theme.Dp(74));
                    tip.Controls.Add(tipText);
                }
            }
            else
            {
                M3Card card = new M3Card();
                card.FillColor = Theme.SurfaceContainer;
                card.BorderColor = Theme.Error;
                int cardH = Theme.Dp(256);
                Widgets.Place(card, pad, (p.Height - cardH) / 2 - Theme.Dp(10), innerW, cardH);
                p.Controls.Add(card);

                card.Paint += delegate(object s, PaintEventArgs e)
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    RectangleF circle = new RectangleF(Theme.Dp(22), Theme.Dp(22), Theme.Dp(48), Theme.Dp(48));
                    Theme.FillRound(g, Theme.ErrorContainer, circle, circle.Width * 0.5f);
                    TextRenderer.DrawText(g, "!", Theme.TitleLarge, Rectangle.Round(circle), Theme.OnErrorContainer,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                };

                string eTitle = lastWasUninstall ? L10n.T("unin.failed") : L10n.T("err.title");
                M3Text title = Widgets.Label(eTitle, Theme.HeadlineSmall, Theme.OnSurface, Theme.SurfaceContainer);
                Widgets.Place(title, Theme.Dp(84), Theme.Dp(28), innerW - Theme.Dp(106), Theme.Dp(34));
                card.Controls.Add(title);

                M3Text detail = Widgets.Label(errorText, Theme.BodyMedium, Theme.OnSurfaceVariant, Theme.SurfaceContainer);
                detail.Wrapped = true;
                Widgets.Place(detail, Theme.Dp(22), Theme.Dp(88), innerW - Theme.Dp(44), Theme.Dp(148));
                card.Controls.Add(detail);
            }
        }

        // ---------------- Install / Uninstall Execution ----------------

        private void StartInstall()
        {
            working = true;
            lastWasUninstall = false;
            outcome = InstallOutcome.Working;
            ShowStep(4, 1, true);

            InstallOptions opts = new InstallOptions();
            opts.Root = root;
            opts.NamesLanguage = namesLanguage;
            opts.ParamsLanguage = paramsLanguage;
            foreach (ComponentInfo c in components)
            {
                opts.Components.Add(new ComponentInfo
                {
                    Id = c.Id,
                    Wasm = c.Wasm,
                    HelperExe = c.HelperExe,
                    LegacyWasm = c.LegacyWasm,
                    Version = c.Version,
                    Group = c.Group,
                    Selected = c.Selected
                });
            }

            ThreadPool.QueueUserWorkItem(delegate
            {
                InstallReport report = InstallerCore.Install(opts, ReportProgress);
                Thread.Sleep(240); // Let final progress bar spring finish smoothly
                InvokeOnUi(delegate
                {
                    working = false;
                    state = InstallerCore.Detect(root);
                    if (report.Ok)
                    {
                        lastInstalledCount = report.InstalledCount;
                        lastInstalledHelper = report.InstalledAnyHelper;
                        outcome = InstallOutcome.Done;
                    }
                    else
                    {
                        errorText = report.Error;
                        outcome = InstallOutcome.Error;
                    }
                    ShowStep(4, 1, true);
                });
            });
        }

        private void StartUninstall()
        {
            working = true;
            lastWasUninstall = true;
            outcome = InstallOutcome.Working;
            ShowStep(4, 1, true);

            string target = root;
            ThreadPool.QueueUserWorkItem(delegate
            {
                InstallReport report = InstallerCore.Uninstall(target, ReportProgress);
                Thread.Sleep(220);
                InvokeOnUi(delegate
                {
                    working = false;
                    state = InstallerCore.Detect(root);
                    if (report.Ok)
                    {
                        outcome = InstallOutcome.Done;
                    }
                    else
                    {
                        errorText = report.Error;
                        outcome = InstallOutcome.Error;
                    }
                    ShowStep(4, 1, true);
                });
            });
        }

        private void ReportProgress(float v, string text)
        {
            InvokeOnUi(delegate
            {
                if (progressBar != null) progressBar.Value = v;
                if (statusLabel != null) statusLabel.Text = text;
            });
        }

        private void InvokeOnUi(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke(action); } catch { }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && !working)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    // ---------------- Shared Axis X Bitmap-Composited Transition Stage ----------------

    internal sealed class TransitionStage : Panel
    {
        private Panel currentPage;
        private Bitmap outSnap;
        private Bitmap inSnap;
        private int slideDir;
        private readonly SpringValue spatial = new SpringValue(1f, SpringSpec.ExpressiveDefaultSpatial);
        private readonly SpringValue fade = new SpringValue(1f, SpringSpec.ExpressiveDefaultEffects);
        private bool animating;
        private bool subscribed;

        public TransitionStage()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
        }

        public void ShowPage(Panel nextPage, int direction, bool animate)
        {
            bool canAnimate = animate && direction != 0 && currentPage != null && Width > 0 && Height > 0 && IsHandleCreated;
            if (!canAnimate)
            {
                StopAnimation();
                ReplaceLivePage(nextPage);
                return;
            }

            Bitmap oldBmp = CaptureControl(currentPage);
            ReplaceLivePage(nextPage);
            Bitmap newBmp = CaptureControl(nextPage);

            DisposeSnapshots();
            outSnap = oldBmp;
            inSnap = newBmp;
            slideDir = direction;
            animating = true;
            currentPage.Visible = false;

            spatial.Snap(0f);
            spatial.Target = 1f;
            fade.Snap(0f);
            fade.Target = 1f;

            if (!subscribed)
            {
                subscribed = true;
                MotionClock.Subscribe(OnTick);
            }
            Invalidate();
        }

        private void ReplaceLivePage(Panel nextPage)
        {
            if (currentPage != null)
            {
                Controls.Remove(currentPage);
                currentPage.Dispose();
            }
            currentPage = nextPage;
            currentPage.SetBounds(0, 0, Width, Height);
            currentPage.Visible = true;
            Controls.Add(currentPage);
        }

        private Bitmap CaptureControl(Control c)
        {
            Bitmap bmp = new Bitmap(Math.Max(1, Width), Math.Max(1, Height));
            c.SetBounds(0, 0, Width, Height);
            c.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
            return bmp;
        }

        private void OnTick(float dt)
        {
            if (!animating) return;
            spatial.Step(dt);
            fade.Step(dt);

            if (spatial.IsSettled && fade.IsSettled)
            {
                StopAnimation();
                if (currentPage != null) currentPage.Visible = true;
            }
            Invalidate();
        }

        private void StopAnimation()
        {
            animating = false;
            if (subscribed)
            {
                MotionClock.Unsubscribe(OnTick);
                subscribed = false;
            }
            DisposeSnapshots();
        }

        private void DisposeSnapshots()
        {
            if (outSnap != null) { outSnap.Dispose(); outSnap = null; }
            if (inSnap != null) { inSnap.Dispose(); inSnap = null; }
        }

        protected override void Dispose(bool disposing)
        {
            StopAnimation();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush bg = new SolidBrush(BackColor)) g.FillRectangle(bg, ClientRectangle);

            if (!animating) return;

            float dist = Theme.Dpf(36f);
            float p = spatial.Value;
            float a = Math.Max(0f, Math.Min(1f, fade.Value));

            if (outSnap != null && a < 0.99f)
            {
                float outX = -slideDir * dist * Math.Min(1f, p);
                DrawWithAlpha(g, outSnap, outX, 0f, 1f - a);
            }
            if (inSnap != null && a > 0.01f)
            {
                float inX = slideDir * dist * (1f - p);
                DrawWithAlpha(g, inSnap, inX, 0f, a);
            }
        }

        private static void DrawWithAlpha(Graphics g, Bitmap bmp, float x, float y, float alpha)
        {
            ColorMatrix cm = new ColorMatrix();
            cm.Matrix33 = Math.Max(0f, Math.Min(1f, alpha));
            using (ImageAttributes ia = new ImageAttributes())
            {
                ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                Rectangle dst = new Rectangle((int)Math.Round(x), (int)Math.Round(y), bmp.Width, bmp.Height);
                g.DrawImage(bmp, dst, 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, ia);
            }
        }
    }

    // ---------------- Animated M3 Circular Spinner ----------------

    internal sealed class M3Spinner : Control
    {
        private float angle;
        private bool subscribed;

        public M3Spinner()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            TabStop = false;
            subscribed = true;
            MotionClock.Subscribe(OnTick);
        }

        private void OnTick(float dt)
        {
            angle = (angle + dt * 300f) % 360f;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (subscribed) { MotionClock.Unsubscribe(OnTick); subscribed = false; }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush bg = new SolidBrush(BackColor)) g.FillRectangle(bg, ClientRectangle);

            float penW = Theme.Dpf(3.5f);
            RectangleF r = new RectangleF(penW, penW, Width - penW * 2f, Height - penW * 2f);
            using (Pen track = new Pen(Theme.SurfaceHighest, penW))
            {
                g.DrawEllipse(track, r);
            }
            using (Pen arc = new Pen(Theme.Primary, penW))
            {
                arc.StartCap = LineCap.Round;
                arc.EndCap = LineCap.Round;
                float sweep = 80f + 50f * (float)Math.Sin(angle * Math.PI / 180.0);
                g.DrawArc(arc, r, angle, sweep);
            }
        }
    }

    // ---------------- Animated M3 Expressive Completion Badge ----------------

    internal sealed class DoneHeroBadge : Control
    {
        private readonly SpringValue scaleSpring = new SpringValue(0.45f, SpringSpec.ExpressiveDefaultSpatial);
        private float checkProg = 0f;
        private bool subscribed;

        public DoneHeroBadge()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            TabStop = false;
            scaleSpring.Target = 1f;
            subscribed = true;
            MotionClock.Subscribe(OnTick);
        }

        private void OnTick(float dt)
        {
            bool dirty = scaleSpring.Step(dt);
            if (scaleSpring.Value > 0.72f && checkProg < 1f)
            {
                checkProg = Math.Min(1f, checkProg + dt * 3.4f);
                dirty = true;
            }
            if (dirty) Invalidate();
            else if (scaleSpring.IsSettled && checkProg >= 1f)
            {
                MotionClock.Unsubscribe(OnTick);
                subscribed = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (subscribed) { MotionClock.Unsubscribe(OnTick); subscribed = false; }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush bg = new SolidBrush(BackColor)) g.FillRectangle(bg, ClientRectangle);

            float s = Math.Max(0.2f, Math.Min(1.12f, scaleSpring.Value));
            float d = (Math.Min(Width, Height) - Theme.Dpf(8f)) * s;
            RectangleF circle = new RectangleF((Width - d) * 0.5f, (Height - d) * 0.5f, d, d);
            Theme.FillRound(g, Theme.PrimaryContainer, circle, d * 0.5f);

            if (checkProg > 0.01f)
            {
                float inset = d * 0.22f;
                RectangleF cb = new RectangleF(circle.X + inset, circle.Y + inset, d - inset * 2f, d - inset * 2f);
                Theme.DrawCheck(g, cb, Theme.OnPrimaryContainer, Theme.Dpf(3.2f), checkProg);
            }
        }
    }
}
