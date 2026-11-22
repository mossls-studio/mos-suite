// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 MOSSLS

// Widgets.cs — Material Design 3 Expressive custom controls with spring physics,
// state layers, corner morphing, and Material ripples.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace MosSuite
{
    internal enum M3ButtonStyle
    {
        Filled,
        Tonal,
        Outlined,
        Text,
        Danger
    }

    internal static class Widgets
    {
        public static M3Text Label(string text, Font font, Color color, Color bg)
        {
            M3Text t = new M3Text();
            t.Text = text;
            t.Font = font;
            t.ForeColor = color;
            t.BackColor = bg;
            return t;
        }

        public static void Place(Control c, int x, int y, int w, int h)
        {
            c.Location = new Point(x, y);
            c.Size = new Size(w, h);
        }
    }

    // ---------------- M3Text ----------------

    internal sealed class M3Text : Control
    {
        public bool Wrapped = false;
        public TextFormatFlags Alignment = TextFormatFlags.Left | TextFormatFlags.VerticalCenter;

        public M3Text()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (SolidBrush b = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(b, ClientRectangle);
            }
            TextFormatFlags flags = Alignment | TextFormatFlags.NoPrefix;
            if (Wrapped)
            {
                flags = (flags & ~TextFormatFlags.VerticalCenter) | TextFormatFlags.Top | TextFormatFlags.WordBreak;
            }
            else
            {
                flags |= TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor, flags);
        }
    }

    // ---------------- M3Card ----------------

    internal sealed class M3Card : Panel
    {
        public Color FillColor = Theme.SurfaceContainer;
        public Color BorderColor = Color.Transparent;
        public int CornerDp = Theme.RadiusMedium;

        public M3Card()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Surface;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush bg = new SolidBrush(Parent != null ? Parent.BackColor : Theme.Surface))
            {
                g.FillRectangle(bg, ClientRectangle);
            }
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float rad = Theme.Dpf(CornerDp);
            Theme.FillRound(g, FillColor, r, rad);
            if (BorderColor.A > 0)
            {
                Theme.StrokeRound(g, BorderColor, r, rad, Theme.Dpf(1f));
            }
        }
    }

    // ---------------- M3Button ----------------

    internal sealed class M3Button : Control
    {
        public M3ButtonStyle Style = M3ButtonStyle.Filled;
        public bool ShowArrow = false;

        private bool hovered;
        private bool pressed;
        private readonly SpringValue stateAlpha = new SpringValue(0f, SpringSpec.ExpressiveFastEffects);
        private readonly SpringValue pressMorph = new SpringValue(0f, SpringSpec.ExpressiveFastSpatial);
        private readonly List<RippleWave> ripples = new List<RippleWave>();
        private bool subscribed;

        public M3Button()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.Selectable, true);
            Cursor = Cursors.Hand;
            Font = Theme.LabelLarge;
            Height = Theme.Dp(40);
            TabStop = true;
        }

        public void FitWidth()
        {
            int pad = (Style == M3ButtonStyle.Text) ? Theme.Dp(16) : Theme.Dp(24);
            int extra = ShowArrow ? Theme.Dp(22) : 0;
            int w = Theme.Measure(Text, Font) + pad * 2 + extra;
            int minW = (Style == M3ButtonStyle.Text) ? Theme.Dp(64) : Theme.Dp(96);
            Width = Math.Max(minW, w);
        }

        private void EnsureTicking()
        {
            if (!subscribed)
            {
                subscribed = true;
                MotionClock.Subscribe(OnTick);
            }
        }

        private void OnTick(float dt)
        {
            bool dirty = false;
            if (stateAlpha.Step(dt)) dirty = true;
            if (pressMorph.Step(dt)) dirty = true;
            for (int i = ripples.Count - 1; i >= 0; i--)
            {
                dirty = true;
                if (!ripples[i].Step(dt)) ripples.RemoveAt(i);
            }
            if (dirty) Invalidate();
            else if (ripples.Count == 0 && stateAlpha.IsSettled && pressMorph.IsSettled)
            {
                MotionClock.Unsubscribe(OnTick);
                subscribed = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (subscribed)
            {
                MotionClock.Unsubscribe(OnTick);
                subscribed = false;
            }
            base.Dispose(disposing);
        }

        private void UpdateTargets()
        {
            if (!Enabled)
            {
                stateAlpha.Target = 0f;
                pressMorph.Target = 0f;
            }
            else
            {
                stateAlpha.Target = pressed ? Theme.StatePress : (hovered ? Theme.StateHover : (Focused ? Theme.StateFocus : 0f));
                pressMorph.Target = pressed ? 1f : 0f;
            }
            EnsureTicking();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            UpdateTargets();
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; UpdateTargets(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; pressed = false; ReleaseRipples(); UpdateTargets(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); UpdateTargets(); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); UpdateTargets(); Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Enabled || e.Button != MouseButtons.Left) return;
            pressed = true;
            Focus();
            SpawnRipple(e.X, e.Y);
            UpdateTargets();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (pressed)
            {
                pressed = false;
                ReleaseRipples();
                UpdateTargets();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (Enabled && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter))
            {
                SpawnRipple(Width * 0.5f, Height * 0.5f);
                ReleaseRipples();
                EnsureTicking();
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void SpawnRipple(float x, float y)
        {
            float dx = Math.Max(x, Width - x);
            float dy = Math.Max(y, Height - y);
            ripples.Add(new RippleWave
            {
                X = x,
                Y = y,
                MaxRadius = (float)Math.Sqrt(dx * dx + dy * dy) * 1.05f,
                Progress = 0.05f,
                Alpha = 0.6f,
                Released = false
            });
            EnsureTicking();
        }

        private void ReleaseRipples()
        {
            for (int i = 0; i < ripples.Count; i++) ripples[i].Released = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color parentBg = BackColor;
            using (SolidBrush clear = new SolidBrush(parentBg))
            {
                g.FillRectangle(clear, ClientRectangle);
            }

            RectangleF r = new RectangleF(Theme.Dpf(1.5f), Theme.Dpf(1.5f), Width - Theme.Dpf(3f), Height - Theme.Dpf(3f));
            float fullR = r.Height * 0.5f;
            float morphR = Theme.Dpf(Theme.RadiusMedium);
            float rad = fullR + (morphR - fullR) * Math.Max(0f, Math.Min(1.15f, pressMorph.Value));
            if (rad < Theme.Dpf(8f)) rad = Theme.Dpf(8f);

            Color fill;
            Color fg;
            Color stateTint;
            bool stroke = false;
            Color strokeColor = Theme.Outline;

            if (!Enabled)
            {
                fill = (Style == M3ButtonStyle.Text || Style == M3ButtonStyle.Outlined)
                    ? parentBg
                    : Theme.Over(parentBg, Theme.OnSurface, Theme.StateDisabledContainer);
                fg = Theme.Over(parentBg, Theme.OnSurface, Theme.StateDisabledContent);
                stateTint = Theme.OnSurface;
                stroke = (Style == M3ButtonStyle.Outlined);
                strokeColor = Theme.Over(parentBg, Theme.OnSurface, Theme.StateDisabledContainer);
            }
            else if (Style == M3ButtonStyle.Filled)
            {
                fill = Theme.Primary;
                fg = Theme.OnPrimary;
                stateTint = Theme.OnPrimary;
            }
            else if (Style == M3ButtonStyle.Tonal)
            {
                fill = Theme.SecondaryContainer;
                fg = Theme.OnSecondaryContainer;
                stateTint = Theme.OnSecondaryContainer;
            }
            else if (Style == M3ButtonStyle.Danger)
            {
                fill = Theme.ErrorContainer;
                fg = Theme.OnErrorContainer;
                stateTint = Theme.OnErrorContainer;
            }
            else if (Style == M3ButtonStyle.Outlined)
            {
                fill = parentBg;
                fg = Theme.Primary;
                stateTint = Theme.Primary;
                stroke = true;
            }
            else
            {
                fill = parentBg;
                fg = Theme.Primary;
                stateTint = Theme.Primary;
            }

            Color renderedFill = Theme.Over(fill, stateTint, Math.Max(0f, stateAlpha.Value));

            using (GraphicsPath path = Theme.RoundPath(r, rad))
            {
                if (renderedFill != parentBg || Style == M3ButtonStyle.Filled || Style == M3ButtonStyle.Tonal || Style == M3ButtonStyle.Danger)
                {
                    using (SolidBrush b = new SolidBrush(renderedFill)) g.FillPath(b, path);
                }
                Theme.DrawRipples(g, path, ripples, stateTint);
                if (stroke)
                {
                    using (Pen pen = new Pen(strokeColor, Theme.Dpf(1f)))
                    {
                        pen.Alignment = PenAlignment.Inset;
                        g.DrawPath(pen, path);
                    }
                }
            }

            if (Focused && Enabled)
            {
                RectangleF fr = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
                Theme.StrokeRound(g, Theme.Primary, fr, rad + Theme.Dpf(1.5f), Theme.Dpf(1.5f));
            }

            if (ShowArrow)
            {
                int textW = Theme.Measure(Text, Font);
                int iconSize = Theme.Dp(16);
                int gap = Theme.Dp(6);
                int total = textW + gap + iconSize;
                int startX = (Width - total) / 2;
                Rectangle tr = new Rectangle(startX, 0, textW + Theme.Dp(4), Height);
                TextRenderer.DrawText(g, Text, Font, tr, fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                RectangleF ir = new RectangleF(startX + textW + gap, (Height - iconSize) * 0.5f, iconSize, iconSize);
                Theme.DrawArrowRight(g, ir, fg, Theme.Dpf(1.8f));
            }
            else
            {
                TextRenderer.DrawText(g, Text, Font, Rectangle.Round(r), fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            }
        }
    }

    // ---------------- M3IconButton ----------------

    internal sealed class M3IconButton : Control
    {
        public enum Glyph { Close, Minimize }
        public Glyph Icon = Glyph.Close;

        private bool hovered;
        private bool pressed;
        private readonly SpringValue stateAlpha = new SpringValue(0f, SpringSpec.ExpressiveFastEffects);
        private bool subscribed;

        public M3IconButton()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.Selectable, true);
            Cursor = Cursors.Hand;
            Size = new Size(Theme.Dp(40), Theme.Dp(40));
            TabStop = true;
        }

        private void EnsureTicking()
        {
            if (!subscribed)
            {
                subscribed = true;
                MotionClock.Subscribe(OnTick);
            }
        }

        private void OnTick(float dt)
        {
            if (stateAlpha.Step(dt)) Invalidate();
            else
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

        private void UpdateTargets()
        {
            stateAlpha.Target = pressed ? Theme.StatePress : (hovered ? Theme.StateHover : (Focused ? Theme.StateFocus : 0f));
            EnsureTicking();
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; UpdateTargets(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; pressed = false; UpdateTargets(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { pressed = true; UpdateTargets(); } }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); pressed = false; UpdateTargets(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); UpdateTargets(); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); UpdateTargets(); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color bg = Parent != null ? Parent.BackColor : BackColor;
            using (SolidBrush b = new SolidBrush(bg)) g.FillRectangle(b, ClientRectangle);

            RectangleF r = new RectangleF(Theme.Dpf(2f), Theme.Dpf(2f), Width - Theme.Dpf(4f), Height - Theme.Dpf(4f));
            Color tint = (Icon == Glyph.Close && hovered) ? Theme.Error : Theme.OnSurfaceVariant;
            if (stateAlpha.Value > 0.002f)
            {
                Theme.FillRound(g, Theme.Over(bg, tint, stateAlpha.Value), r, r.Width * 0.5f);
            }
            if (Focused)
            {
                Theme.StrokeRound(g, Theme.Primary, r, r.Width * 0.5f, Theme.Dpf(1.5f));
            }
            RectangleF iconRect = new RectangleF(Width * 0.25f, Height * 0.25f, Width * 0.5f, Height * 0.5f);
            if (Icon == Glyph.Close) Theme.DrawClose(g, iconRect, tint, Theme.Dpf(1.8f));
            else Theme.DrawMinimize(g, iconRect, tint, Theme.Dpf(1.8f));
        }
    }

    // ---------------- M3ConnectedButtonGroup (M3 Expressive replacement for Segmented Button) ----------------

    internal sealed class M3ConnectedButtonGroup : Control
    {
        public string[] Values = new string[0];
        public string[] Labels = new string[0];
        public bool Compact = false;
        public event EventHandler SelectionChanged;

        private string selected = "";
        private int hoverIndex = -1;
        private int pressIndex = -1;
        private SpringValue[] selectSprings = new SpringValue[0];
        private SpringValue[] hoverSprings = new SpringValue[0];
        private bool subscribed;

        public string Selected
        {
            get { return selected; }
            set
            {
                if (selected == value) return;
                selected = value;
                SyncSpringTargets(false);
            }
        }

        public M3ConnectedButtonGroup()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.Selectable, true);
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public void SnapState()
        {
            SyncSpringTargets(true);
            Invalidate();
        }

        private void EnsureArrays()
        {
            if (selectSprings.Length == Values.Length) return;
            selectSprings = new SpringValue[Values.Length];
            hoverSprings = new SpringValue[Values.Length];
            for (int i = 0; i < Values.Length; i++)
            {
                float init = (Values[i] == selected) ? 1f : 0f;
                selectSprings[i] = new SpringValue(init, SpringSpec.ExpressiveFastSpatial);
                hoverSprings[i] = new SpringValue(0f, SpringSpec.ExpressiveFastEffects);
            }
        }

        private void SyncSpringTargets(bool snap)
        {
            EnsureArrays();
            for (int i = 0; i < Values.Length; i++)
            {
                float s = (Values[i] == selected) ? 1f : 0f;
                float h = (i == pressIndex) ? Theme.StatePress : (i == hoverIndex ? Theme.StateHover : 0f);
                if (snap)
                {
                    selectSprings[i].Snap(s);
                    hoverSprings[i].Snap(h);
                }
                else
                {
                    selectSprings[i].Target = s;
                    hoverSprings[i].Target = h;
                }
            }
            if (!snap) EnsureTicking();
        }

        private void EnsureTicking()
        {
            if (!subscribed)
            {
                subscribed = true;
                MotionClock.Subscribe(OnTick);
            }
        }

        private void OnTick(float dt)
        {
            bool dirty = false;
            bool allSettled = true;
            for (int i = 0; i < selectSprings.Length; i++)
            {
                if (selectSprings[i].Step(dt)) dirty = true;
                if (hoverSprings[i].Step(dt)) dirty = true;
                if (!selectSprings[i].IsSettled || !hoverSprings[i].IsSettled) allSettled = false;
            }
            if (dirty) Invalidate();
            if (allSettled)
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

        private int HitTest(int x)
        {
            if (Values.Length == 0 || Width <= 0) return -1;
            int idx = (int)(x * Values.Length / (float)Width);
            if (idx < 0) idx = 0;
            if (idx >= Values.Length) idx = Values.Length - 1;
            return idx;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int idx = HitTest(e.X);
            if (idx != hoverIndex) { hoverIndex = idx; SyncSpringTargets(false); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoverIndex = -1;
            pressIndex = -1;
            SyncSpringTargets(false);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();
            int idx = HitTest(e.X);
            pressIndex = idx;
            if (idx >= 0 && idx < Values.Length && Values[idx] != selected)
            {
                selected = Values[idx];
                SyncSpringTargets(false);
                if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
            }
            else
            {
                SyncSpringTargets(false);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            pressIndex = -1;
            SyncSpringTargets(false);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            EnsureArrays();
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color bg = BackColor;
            using (SolidBrush b = new SolidBrush(bg)) g.FillRectangle(b, ClientRectangle);
            if (Values.Length == 0) return;

            float gap = Theme.Dpf(3f);
            float itemW = (Width - gap * (Values.Length - 1)) / Values.Length;
            float pillR = (Height - Theme.Dpf(2f)) * 0.5f;
            float innerR = Theme.Dpf(Theme.RadiusExtraSmall + 2f); // 6dp inner corner for connected group
            Font f = Compact ? Theme.LabelMedium : Theme.LabelLarge;

            for (int i = 0; i < Values.Length; i++)
            {
                float x = i * (itemW + gap);
                RectangleF rect = new RectangleF(x, Theme.Dpf(1f), itemW, Height - Theme.Dpf(2f));
                float t = Math.Max(0f, Math.Min(1f, selectSprings[i].Value));

                // M3 Expressive Connected Button Group: outer corners always pill; inner corners morph 6dp -> pill when selected!
                float tl = (i == 0) ? pillR : (innerR + (pillR - innerR) * t);
                float bl = tl;
                float tr = (i == Values.Length - 1) ? pillR : (innerR + (pillR - innerR) * t);
                float br = tr;

                Color baseFill = Theme.Blend(Theme.SurfaceHigh, Theme.SecondaryContainer, t);
                Color fg = Theme.Blend(Theme.OnSurface, Theme.OnSecondaryContainer, t);
                Color fill = Theme.Over(baseFill, fg, Math.Max(0f, hoverSprings[i].Value));

                Theme.FillRound(g, fill, rect, tl, tr, br, bl);

                string label = i < Labels.Length ? Labels[i] : Values[i];
                if (!Compact && t > 0.08f)
                {
                    int iconSize = Theme.Dp(15);
                    int textW = Theme.Measure(label, f);
                    int iconSpace = (int)Math.Round((iconSize + Theme.Dp(6)) * t);
                    int totalW = textW + iconSpace;
                    int startX = (int)Math.Round(rect.X + (rect.Width - totalW) * 0.5f);

                    RectangleF checkBox = new RectangleF(startX, rect.Y + (rect.Height - iconSize) * 0.5f, iconSize, iconSize);
                    Theme.DrawCheck(g, checkBox, fg, Theme.Dpf(1.8f), t);

                    Rectangle textRect = new Rectangle(startX + iconSpace, (int)rect.Y, textW + Theme.Dp(6), (int)rect.Height);
                    TextRenderer.DrawText(g, label, f, textRect, fg,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                }
                else
                {
                    TextRenderer.DrawText(g, label, f, Rectangle.Round(rect), fg,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                }
            }
        }
    }

    // ---------------- M3EffectCard (Selectable Card with Expressive Checkmark & Ripple) ----------------

    internal sealed class M3EffectCard : Control
    {
        public string Title = "";
        public string Meta = "";
        public string Badge = "";
        public bool BadgePrimary = false;
        public string StatusTag = "";
        public event EventHandler Toggled;

        private bool selected = true;
        private bool hovered;
        private bool pressed;
        private readonly SpringValue selSpring;
        private readonly SpringValue hoverSpring = new SpringValue(0f, SpringSpec.ExpressiveFastEffects);
        private readonly SpringValue pressSpring = new SpringValue(0f, SpringSpec.ExpressiveFastSpatial);
        private readonly List<RippleWave> ripples = new List<RippleWave>();
        private bool subscribed;

        public bool Selected
        {
            get { return selected; }
            set
            {
                if (selected == value) return;
                selected = value;
                selSpring.Target = selected ? 1f : 0f;
                EnsureTicking();
            }
        }

        public M3EffectCard(bool initialSelected)
        {
            selected = initialSelected;
            selSpring = new SpringValue(initialSelected ? 1f : 0f, SpringSpec.ExpressiveFastSpatial);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.Selectable, true);
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        private void EnsureTicking()
        {
            if (!subscribed)
            {
                subscribed = true;
                MotionClock.Subscribe(OnTick);
            }
        }

        private void OnTick(float dt)
        {
            bool dirty = false;
            if (selSpring.Step(dt)) dirty = true;
            if (hoverSpring.Step(dt)) dirty = true;
            if (pressSpring.Step(dt)) dirty = true;
            for (int i = ripples.Count - 1; i >= 0; i--)
            {
                dirty = true;
                if (!ripples[i].Step(dt)) ripples.RemoveAt(i);
            }
            if (dirty) Invalidate();
            else if (ripples.Count == 0 && selSpring.IsSettled && hoverSpring.IsSettled && pressSpring.IsSettled)
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

        private void UpdateHover()
        {
            hoverSpring.Target = pressed ? Theme.StatePress : (hovered ? Theme.StateHover : (Focused ? Theme.StateFocus : 0f));
            pressSpring.Target = pressed ? 1f : 0f;
            EnsureTicking();
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; UpdateHover(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; pressed = false; ReleaseRipples(); UpdateHover(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); UpdateHover(); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); UpdateHover(); Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            pressed = true;
            Focus();
            float dx = Math.Max(e.X, Width - e.X);
            float dy = Math.Max(e.Y, Height - e.Y);
            ripples.Add(new RippleWave { X = e.X, Y = e.Y, MaxRadius = (float)Math.Sqrt(dx * dx + dy * dy), Progress = 0.05f, Alpha = 0.55f });
            UpdateHover();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (pressed)
            {
                pressed = false;
                ReleaseRipples();
                if (ClientRectangle.Contains(e.Location))
                {
                    Selected = !Selected;
                    if (Toggled != null) Toggled(this, EventArgs.Empty);
                }
                UpdateHover();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                Selected = !Selected;
                if (Toggled != null) Toggled(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void ReleaseRipples()
        {
            for (int i = 0; i < ripples.Count; i++) ripples[i].Released = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color bg = Parent != null ? Parent.BackColor : Theme.Surface;
            using (SolidBrush b = new SolidBrush(bg)) g.FillRectangle(b, ClientRectangle);

            float t = Math.Max(0f, Math.Min(1f, selSpring.Value));
            float rad = Theme.Dpf(Theme.RadiusMedium) + Theme.Dpf(3f) * Math.Max(0f, pressSpring.Value);
            RectangleF r = new RectangleF(Theme.Dpf(1f), Theme.Dpf(1f), Width - Theme.Dpf(2f), Height - Theme.Dpf(2f));

            Color selectedTint = Theme.Blend(Theme.SurfaceContainer, Theme.PrimaryContainer, 0.36f);
            Color baseFill = Theme.Blend(Theme.SurfaceContainer, selectedTint, t);
            Color renderedFill = Theme.Over(baseFill, Theme.OnSurface, Math.Max(0f, hoverSpring.Value));
            Color border = Theme.Blend(Theme.OutlineVariant, Theme.Blend(Theme.OutlineVariant, Theme.Primary, 0.55f), t);

            using (GraphicsPath path = Theme.RoundPath(r, rad))
            {
                using (SolidBrush fill = new SolidBrush(renderedFill)) g.FillPath(fill, path);
                Theme.DrawRipples(g, path, ripples, Theme.Primary);
                using (Pen pen = new Pen(border, Theme.Dpf(1f)))
                {
                    pen.Alignment = PenAlignment.Inset;
                    g.DrawPath(pen, path);
                }
            }

            // Left check circle (22dp)
            float checkD = Theme.Dpf(20f);
            RectangleF cb = new RectangleF(Theme.Dpf(14f), (Height - checkD) * 0.5f, checkD, checkD);
            if (t > 0.02f)
            {
                float scale = 0.72f + 0.28f * selSpring.Value;
                float sw = checkD * scale;
                RectangleF scb = new RectangleF(cb.X + (checkD - sw) * 0.5f, cb.Y + (checkD - sw) * 0.5f, sw, sw);
                Theme.FillRound(g, Theme.Fade(Theme.Primary, t), scb, sw * 0.5f);
                Theme.DrawCheck(g, scb, Theme.OnPrimary, Theme.Dpf(2f), t);
            }
            if (t < 0.98f)
            {
                Theme.StrokeRound(g, Theme.Fade(Theme.Outline, 1f - t), cb, checkD * 0.5f, Theme.Dpf(1.6f));
            }

            // Right badge chip (8dp radius per M3 Chip spec)
            int rightEdge = Width - Theme.Dp(12);
            if (!string.IsNullOrEmpty(Badge))
            {
                int bw = Theme.Measure(Badge, Theme.LabelSmall) + Theme.Dp(14);
                int bh = Theme.Dp(20);
                RectangleF chip = new RectangleF(rightEdge - bw, (Height - bh) * 0.5f, bw, bh);
                Color chipBg = BadgePrimary ? Theme.TertiaryContainer : Theme.SurfaceHighest;
                Color chipFg = BadgePrimary ? Theme.OnTertiaryContainer : Theme.OnSurfaceVariant;
                Theme.FillRound(g, chipBg, chip, Theme.Dpf(Theme.RadiusSmall));
                TextRenderer.DrawText(g, Badge, Theme.LabelSmall, Rectangle.Round(chip), chipFg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                rightEdge -= bw + Theme.Dp(6);
            }

            // Title + meta
            int textX = Theme.Dp(46);
            int textW = Math.Max(Theme.Dp(40), rightEdge - textX);
            Rectangle titleRect = new Rectangle(textX, Theme.Dp(9), textW, Theme.Dp(20));
            TextRenderer.DrawText(g, Title, Theme.TitleSmall, titleRect, Theme.OnSurface,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

            string sub = string.IsNullOrEmpty(StatusTag) ? Meta : (Meta + " · " + StatusTag);
            Rectangle metaRect = new Rectangle(textX, Theme.Dp(30), textW, Theme.Dp(16));
            TextRenderer.DrawText(g, sub, Theme.BodySmall, metaRect, Theme.OnSurfaceVariant,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }
    }

    // ---------------- M3ProgressBar (M3 Expressive Linear Progress Indicator) ----------------

    internal sealed class M3ProgressBar : Control
    {
        private readonly SpringValue valueSpring = new SpringValue(0f, SpringSpec.ExpressiveDefaultEffects);
        private float shimmerPhase;
        private bool subscribed;

        public float Value
        {
            get { return valueSpring.Target; }
            set
            {
                float v = Math.Max(0f, Math.Min(1f, value));
                valueSpring.Target = v;
                EnsureTicking();
            }
        }

        public M3ProgressBar()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            Height = Theme.Dp(6);
            TabStop = false;
        }

        public void Reset()
        {
            valueSpring.Snap(0f);
            shimmerPhase = 0f;
            Invalidate();
        }

        private void EnsureTicking()
        {
            if (!subscribed)
            {
                subscribed = true;
                MotionClock.Subscribe(OnTick);
            }
        }

        private void OnTick(float dt)
        {
            bool dirty = valueSpring.Step(dt);
            if (valueSpring.Value > 0.01f && valueSpring.Value < 0.995f)
            {
                shimmerPhase = (shimmerPhase + dt * 1.15f) % 1f;
                dirty = true;
            }
            if (dirty) Invalidate();
            else if (valueSpring.IsSettled && (valueSpring.Value <= 0.01f || valueSpring.Value >= 0.995f))
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
            Color bg = Parent != null ? Parent.BackColor : BackColor;
            using (SolidBrush b = new SolidBrush(bg)) g.FillRectangle(b, ClientRectangle);

            float h = Height;
            float r = h * 0.5f;
            float v = Math.Max(0f, Math.Min(1f, valueSpring.Value));
            float activeW = Width * v;
            float gap = Theme.Dpf(4f);

            // Inactive track (right of active bar with M3 gap)
            float trackX = activeW > 1f ? Math.Min(Width, activeW + gap) : 0f;
            float trackW = Width - trackX;
            if (trackW > 1f)
            {
                Theme.FillRound(g, Theme.SurfaceHighest, new RectangleF(trackX, 0, trackW, h), r);
                // M3 2024+ stop indicator dot at end of track
                if (v < 0.99f)
                {
                    float dot = h * 0.66f;
                    Theme.FillRound(g, Theme.Primary, new RectangleF(Width - dot - (h - dot) * 0.5f, (h - dot) * 0.5f, dot, dot), dot * 0.5f);
                }
            }

            // Active indicator
            if (activeW > 1f)
            {
                RectangleF activeRect = new RectangleF(0, 0, Math.Max(h, activeW), h);
                Theme.FillRound(g, Theme.Primary, activeRect, r);
            }
        }
    }

    // ---------------- M3NavRail (Expanded Navigation Rail with Spring-Sliding Active Pill) ----------------

    internal sealed class M3NavRail : Control
    {
        public string[] Labels = new string[0];
        public int ActiveIndex = 0;
        public int MaxUnlockedIndex = 0;
        public bool Locked = false;
        public event Action<int> StepClicked;

        private int hoverIndex = -1;
        private readonly SpringValue pillY = new SpringValue(0f, SpringSpec.ExpressiveDefaultSpatial);
        private bool pillInitialized;
        private bool subscribed;

        public M3NavRail()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            BackColor = Theme.SurfaceLow;
            Cursor = Cursors.Default;
        }

        public void SetActive(int idx, bool snap)
        {
            ActiveIndex = idx;
            float targetY = ItemTop(idx);
            if (snap || !pillInitialized)
            {
                pillY.Snap(targetY);
                pillInitialized = true;
                Invalidate();
            }
            else
            {
                pillY.Target = targetY;
                EnsureTicking();
            }
        }

        private float ItemTop(int idx)
        {
            return Theme.Dp(96) + idx * Theme.Dp(52);
        }

        private void EnsureTicking()
        {
            if (!subscribed)
            {
                subscribed = true;
                MotionClock.Subscribe(OnTick);
            }
        }

        private void OnTick(float dt)
        {
            if (pillY.Step(dt)) Invalidate();
            else
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

        private int HitTest(int y)
        {
            for (int i = 0; i < Labels.Length; i++)
            {
                float top = ItemTop(i);
                if (y >= top && y <= top + Theme.Dp(46)) return i;
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int idx = HitTest(e.Y);
            bool clickable = !Locked && idx >= 0 && idx <= MaxUnlockedIndex && idx < 4;
            Cursor = clickable ? Cursors.Hand : Cursors.Default;
            if (idx != hoverIndex)
            {
                hoverIndex = clickable ? idx : -1;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoverIndex = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (Locked || e.Button != MouseButtons.Left) return;
            int idx = HitTest(e.Y);
            if (idx >= 0 && idx <= MaxUnlockedIndex && idx < 4 && idx != ActiveIndex)
            {
                if (StepClicked != null) StepClicked(idx);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (SolidBrush bg = new SolidBrush(Theme.SurfaceLow)) g.FillRectangle(bg, ClientRectangle);

            // Brand header
            int pad = Theme.Dp(20);
            int logoSize = Theme.Dp(32);
            Theme.DrawLogo(g, new RectangleF(pad, Theme.Dp(24), logoSize, logoSize));

            Rectangle titleRect = new Rectangle(pad + logoSize + Theme.Dp(10), Theme.Dp(22), Width - pad - logoSize - Theme.Dp(20), Theme.Dp(20));
            TextRenderer.DrawText(g, L10n.T("app.title"), Theme.TitleMedium, titleRect, Theme.OnSurface,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

            // Version chip (8dp)
            string ver = "v" + InstallerCore.SuiteVersion;
            int vw = Theme.Measure(ver, Theme.LabelSmall) + Theme.Dp(12);
            RectangleF vchip = new RectangleF(pad + logoSize + Theme.Dp(10), Theme.Dp(45), vw, Theme.Dp(18));
            Theme.FillRound(g, Theme.SurfaceHigh, vchip, Theme.Dpf(Theme.RadiusSmall));
            TextRenderer.DrawText(g, ver, Theme.LabelSmall, Rectangle.Round(vchip), Theme.OnSurfaceVariant,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

            // Spring-sliding active indicator pill
            if (Labels.Length > 0)
            {
                RectangleF activePill = new RectangleF(Theme.Dp(12), pillY.Value, Width - Theme.Dp(24), Theme.Dp(44));
                Theme.FillRound(g, Theme.SecondaryContainer, activePill, activePill.Height * 0.5f);
            }

            // Step items
            for (int i = 0; i < Labels.Length; i++)
            {
                float top = ItemTop(i);
                RectangleF itemRect = new RectangleF(Theme.Dp(12), top, Width - Theme.Dp(24), Theme.Dp(44));
                bool isActive = (i == ActiveIndex);
                bool isCompleted = (i < ActiveIndex);
                bool isHover = (i == hoverIndex && !isActive);

                if (isHover)
                {
                    Theme.FillRound(g, Theme.Over(Theme.SurfaceLow, Theme.OnSurface, Theme.StateHover), itemRect, itemRect.Height * 0.5f);
                }

                // Step badge circle (22dp)
                float badgeD = Theme.Dpf(22f);
                RectangleF badgeRect = new RectangleF(itemRect.X + Theme.Dpf(12f), itemRect.Y + (itemRect.Height - badgeD) * 0.5f, badgeD, badgeD);
                if (isCompleted)
                {
                    Theme.FillRound(g, Theme.Primary, badgeRect, badgeD * 0.5f);
                    Theme.DrawCheck(g, badgeRect, Theme.OnPrimary, Theme.Dpf(1.8f), 1f);
                }
                else if (isActive)
                {
                    Theme.FillRound(g, Theme.Primary, badgeRect, badgeD * 0.5f);
                    TextRenderer.DrawText(g, (i + 1).ToString(), Theme.LabelSmall, Rectangle.Round(badgeRect), Theme.OnPrimary,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }
                else
                {
                    Theme.StrokeRound(g, Theme.OutlineVariant, badgeRect, badgeD * 0.5f, Theme.Dpf(1.4f));
                    TextRenderer.DrawText(g, (i + 1).ToString(), Theme.LabelSmall, Rectangle.Round(badgeRect), Theme.OnSurfaceVariant,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }

                Color textColor = isActive ? Theme.OnSecondaryContainer : (isCompleted ? Theme.OnSurface : Theme.OnSurfaceVariant);
                Font textFont = isActive ? Theme.LabelLarge : Theme.BodyMedium;
                Rectangle textRect = new Rectangle((int)(badgeRect.Right + Theme.Dp(12)), (int)itemRect.Y, (int)(itemRect.Right - badgeRect.Right - Theme.Dp(20)), (int)itemRect.Height);
                TextRenderer.DrawText(g, Labels[i], textFont, textRect, textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }

            // Subtle right divider
            using (Pen pen = new Pen(Theme.OutlineVariant, Theme.Dpf(1f)))
            {
                g.DrawLine(pen, Width - 1, 0, Width - 1, Height);
            }
        }
    }
}
