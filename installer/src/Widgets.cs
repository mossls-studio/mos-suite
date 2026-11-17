// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 MOSSLS

// Widgets.cs — Material 3 controls painted by hand (C# 5 / .NET Framework).
using System;
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
        TextDanger,
        Danger
    }

    internal sealed class M3Button : Control
    {
        public M3ButtonStyle Style = M3ButtonStyle.Filled;
        private bool hover;
        private bool pressed;

        public M3Button()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Theme.LabelLarge;
            Height = Theme.Dp(40);
            TabStop = true;
            Cursor = Cursors.Hand;
            BackColor = Theme.Surface;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            FitWidth();
            Invalidate();
        }

        public void FitWidth()
        {
            bool textOnly = Style == M3ButtonStyle.Text || Style == M3ButtonStyle.TextDanger;
            int pad = textOnly ? 24 : 48;
            int w = Theme.Measure(Text, Font) + Theme.Dp(pad);
            Width = Math.Max(Theme.Dp(textOnly ? 56 : 88), w);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                pressed = true;
                Focus();
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool fire = pressed && ClientRectangle.Contains(e.Location);
            pressed = false;
            Invalidate();
            if (fire) OnClick(EventArgs.Empty);
            base.OnMouseUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) && Enabled)
            {
                pressed = true;
                Invalidate();
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (pressed && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) && Enabled)
            {
                pressed = false;
                Invalidate();
                OnClick(EventArgs.Empty);
            }
            base.OnKeyUp(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            if (BackColor.A > 0)
            {
                using (SolidBrush bg = new SolidBrush(BackColor))
                {
                    g.FillRectangle(bg, ClientRectangle);
                }
            }

            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = Height / 2f;
            bool on = Enabled;

            Color bgColor = Color.Transparent;
            Color fg = Theme.Primary;
            switch (Style)
            {
                case M3ButtonStyle.Filled:
                    bgColor = on ? Theme.Primary : Theme.Over(Theme.Surface, Theme.OnSurface, 12);
                    fg = on ? Theme.OnPrimary : Theme.Over(Theme.Surface, Theme.OnSurface, 38);
                    break;
                case M3ButtonStyle.Tonal:
                    bgColor = on ? Theme.SecondaryContainer : Theme.Over(Theme.Surface, Theme.OnSurface, 12);
                    fg = on ? Theme.OnSecondaryContainer : Theme.Over(Theme.Surface, Theme.OnSurface, 38);
                    break;
                case M3ButtonStyle.Outlined:
                    fg = on ? Theme.Primary : Theme.Over(Theme.Surface, Theme.OnSurface, 38);
                    break;
                case M3ButtonStyle.Text:
                    fg = on ? Theme.Primary : Theme.Over(Theme.Surface, Theme.OnSurface, 38);
                    break;
                case M3ButtonStyle.TextDanger:
                    fg = on ? Theme.Error : Theme.Over(Theme.Surface, Theme.OnSurface, 38);
                    break;
                case M3ButtonStyle.Danger:
                    bgColor = on ? Theme.Error : Theme.Over(Theme.Surface, Theme.OnSurface, 12);
                    fg = on ? Theme.OnError : Theme.Over(Theme.Surface, Theme.OnSurface, 38);
                    break;
            }

            if (bgColor.A > 0) Theme.FillRound(g, bgColor, r, radius);
            if (Style == M3ButtonStyle.Outlined && on) Theme.StrokeRound(g, Theme.Primary, r, radius, Theme.Dp(1));

            if (hover && on) Theme.FillRound(g, Theme.Fade(fg, 20), r, radius);
            if (pressed && on) Theme.FillRound(g, Theme.Fade(fg, 30), r, radius);

            TextRenderer.DrawText(g, Text, Font, Rectangle.Round(r), fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

            if (Focused && ShowFocusCues)
            {
                Theme.StrokeRound(g, Theme.Primary, new RectangleF(Theme.Dp(2), Theme.Dp(2), Width - Theme.Dp(4), Height - Theme.Dp(4)), radius, Theme.Dp(2));
            }
        }
    }

    // Connected segmented button (M3 "segmented button"): one pill, filled selected segment.
    internal sealed class M3Segmented : Control
    {
        public string[] Values = new string[0];
        public string[] Labels = new string[0];
        public string Selected = "";
        public event EventHandler SelectionChanged;

        private int hoverIndex = -1;

        public M3Segmented()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Theme.LabelLarge;
            Height = Theme.Dp(40);
            TabStop = true;
            Cursor = Cursors.Hand;
            BackColor = Theme.SurfaceLow;
        }

        private int IndexAt(int x)
        {
            if (Values.Length == 0) return -1;
            float inset = 1f;
            float w = (Width - 2f) / (float)Values.Length;
            int i = (int)((x - inset) / w);
            if (i < 0 || i >= Values.Length) return -1;
            return i;
        }

        private void Select(int index)
        {
            if (index < 0 || index >= Values.Length) return;
            if (Values[index] == Selected) return;
            Selected = Values[index];
            Invalidate();
            if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int i = IndexAt(e.X);
            if (i != hoverIndex)
            {
                hoverIndex = i;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverIndex = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Focus();
                Select(IndexAt(e.X));
            }
            base.OnMouseDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            int i = Array.IndexOf(Values, Selected);
            if (e.KeyCode == Keys.Left && i > 0) Select(i - 1);
            if (e.KeyCode == Keys.Right && i < Values.Length - 1) Select(i + 1);
            base.OnKeyDown(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (SolidBrush bg = new SolidBrush(BackColor))
            {
                g.FillRectangle(bg, ClientRectangle);
            }

            if (Values.Length == 0) return;

            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = Height / 2f;
            float w = (Width - 2f) / Values.Length;

            Theme.FillRound(g, Theme.SurfaceLowest, r, radius);
            Theme.StrokeRound(g, Theme.Outline, r, radius, Theme.Dp(1));

            GraphicsState state = g.Save();
            using (GraphicsPath clip = Theme.RoundPath(new RectangleF(1, 1, Width - 2, Height - 2), radius - 1))
            {
                g.SetClip(clip);
                for (int i = 0; i < Values.Length; i++)
                {
                    RectangleF seg = new RectangleF(1f + i * w, 1f, w, Height - 2f);
                    bool selected = Values[i] == Selected;
                    if (selected)
                    {
                        Theme.FillRound(g, Theme.SecondaryContainer, seg, 0f);
                    }
                    else if (i == hoverIndex)
                    {
                        Theme.FillRound(g, Theme.Fade(Theme.OnSurface, 20), seg, 0f);
                    }
                }
                g.Restore(state);

                // dividers between non-selected neighbours
                for (int i = 1; i < Values.Length; i++)
                {
                    bool nearSelected = Values[i] == Selected || Values[i - 1] == Selected;
                    if (nearSelected) continue;
                    float x = 1f + i * w;
                    using (Pen pen = new Pen(Theme.OutlineVariant, Theme.Dp(1)))
                    {
                        g.DrawLine(pen, x, Theme.Dp(9), x, Height - Theme.Dp(9));
                    }
                }
            }

            for (int i = 0; i < Values.Length; i++)
            {
                bool selected = Values[i] == Selected;
                Color fg = selected ? Theme.OnSecondaryContainer : Theme.OnSurface;
                RectangleF seg = new RectangleF(1f + i * w, 0f, w, Height);
                TextRenderer.DrawText(g, Labels[i], Font, Rectangle.Round(seg), fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            }

            if (Focused && ShowFocusCues)
            {
                Theme.StrokeRound(g, Theme.Primary, new RectangleF(Theme.Dp(3), Theme.Dp(3), Width - Theme.Dp(6), Height - Theme.Dp(6)), radius - Theme.Dp(2), Theme.Dp(2));
            }
        }
    }

    internal sealed class M3Card : Panel
    {
        public M3Card()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.SurfaceLow;
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Theme.FillRound(e.Graphics, BackColor, new RectangleF(0, 0, Width, Height), Theme.Dp(Theme.RadiusCard));
            base.OnPaint(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 0 && Height > 0)
            {
                Region old = Region;
                using (GraphicsPath p = Theme.RoundPath(new RectangleF(0, 0, Width, Height), Theme.Dp(Theme.RadiusCard)))
                {
                    Region = new Region(p);
                }
                if (old != null) old.Dispose();
            }
        }
    }

    internal sealed class M3IconButton : Control
    {
        public enum Glyph
        {
            Close,
            Check
        }

        public Glyph Icon = Glyph.Close;
        private bool hover;

        public M3IconButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(Theme.Dp(40), Theme.Dp(40));
            Cursor = Cursors.Hand;
            BackColor = Theme.Surface;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush bg = new SolidBrush(BackColor))
            {
                g.FillRectangle(bg, ClientRectangle);
            }
            if (hover)
            {
                Theme.FillRound(g, Theme.Fade(Theme.OnSurface, 20), new RectangleF(0, 0, Width, Height), Width / 2f);
            }
            RectangleF icon = new RectangleF(Width / 2f - Theme.Dp(10), Height / 2f - Theme.Dp(10), Theme.Dp(20), Theme.Dp(20));
            if (Icon == Glyph.Close) Theme.DrawClose(g, icon, Theme.OnSurfaceVariant, Theme.Dp(1.6f));
            else Theme.DrawCheck(g, icon, Theme.OnSurfaceVariant, Theme.Dp(2f));
        }
    }

    internal sealed class M3ProgressBar : Control
    {
        private float value;

        public M3ProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = Theme.Dp(4);
            BackColor = Theme.Surface;
        }

        public float Value
        {
            get { return value; }
            set
            {
                this.value = Math.Max(0f, Math.Min(1f, value));
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush bg = new SolidBrush(BackColor))
            {
                g.FillRectangle(bg, ClientRectangle);
            }
            RectangleF track = new RectangleF(0, Height / 2f - Theme.Dp(2), Width, Theme.Dp(4));
            if (track.Width < 4) return;
            Theme.FillRound(g, Theme.SurfaceHighest, track, Theme.Dp(2));
            float w = track.Width * value;
            if (w > 0.5f)
            {
                Theme.FillRound(g, Theme.Primary, new RectangleF(track.X, track.Y, w, track.Height), Theme.Dp(2));
            }
        }
    }

    // One component row: checkbox + effect name + install state.
    internal sealed class ComponentRow : Control
    {
        public string ComponentId = "";
        public string EffectName = "";
        public string Version = "";
        public bool Installed;
        public bool Checked = true;
        public event EventHandler CheckedChanged;

        private bool hover;

        public ComponentRow()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.SurfaceLow;
            Height = Theme.Dp(56);
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Focus();
                Toggle();
            }
            base.OnMouseDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space) Toggle();
            base.OnKeyDown(e);
        }

        private void Toggle()
        {
            Checked = !Checked;
            Invalidate();
            if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (SolidBrush bg = new SolidBrush(BackColor))
            {
                g.FillRectangle(bg, ClientRectangle);
            }

            RectangleF r = new RectangleF(0, 0, Width, Height);
            if (hover) Theme.FillRound(g, Theme.Fade(Theme.OnSurface, 20), r, Theme.Dp(8));

            int box = Theme.Dp(18);
            float bx = Theme.Dp(10);
            float by = (Height - box) / 2f;
            RectangleF boxRect = new RectangleF(bx, by, box, box);
            if (Checked)
            {
                Theme.FillRound(g, Theme.Primary, boxRect, Theme.Dp(2));
                Theme.DrawCheck(g, boxRect, Theme.OnPrimary, Theme.Dp(1.8f));
            }
            else
            {
                Theme.StrokeRound(g, Theme.Outline, boxRect, Theme.Dp(2), Theme.Dp(2));
            }

            string state = Installed ? L10n.F("comp.installed", Version) : L10n.T("comp.notInstalled");
            int stateWidth = Theme.Measure(state, Theme.LabelSmall) + Theme.Dp(10);
            float textX = bx + box + Theme.Dp(14);
            RectangleF nameRect = new RectangleF(textX, 0, Width - textX - stateWidth - Theme.Dp(6), Height);
            Color nameColor = Checked ? Theme.OnSurface : Theme.OnSurfaceVariant;
            Theme.Text(g, EffectName, Theme.TitleMedium, nameColor, nameRect);

            RectangleF stateRect = new RectangleF(Width - stateWidth, 0, stateWidth - Theme.Dp(8), Height);
            Theme.DrawText(g, state, Theme.LabelSmall,
                Installed ? Theme.Primary : Theme.OnSurfaceVariant,
                stateRect, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

            if (Focused && ShowFocusCues)
            {
                Theme.StrokeRound(g, Theme.Primary, new RectangleF(Theme.Dp(3), Theme.Dp(3), Width - Theme.Dp(6), Height - Theme.Dp(6)), Theme.Dp(6), Theme.Dp(2));
            }
        }
    }

    internal static class Widgets
    {
        public static Label Label(string text, Font font, Color color, Color background)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = font;
            l.ForeColor = color;
            l.BackColor = background;
            l.AutoSize = false;
            l.UseMnemonic = false;
            l.Margin = new Padding(0);
            return l;
        }

        public static void Place(Control c, int x, int y, int w, int h)
        {
            c.Location = new Point(x, y);
            c.Size = new Size(w, h);
        }
    }
}
