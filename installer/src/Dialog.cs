// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 MOSSLS

// Dialog.cs — Material 3 confirmation dialog (28dp corners, surface container high).
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MosSuite
{
    internal sealed class M3DialogForm : Form
    {
        private readonly string titleText;
        private readonly string bodyText;
        private readonly string cancelText;
        private readonly string confirmText;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public M3DialogForm(string title, string body, string cancel, string confirm)
        {
            titleText = title;
            bodyText = body;
            cancelText = cancel;
            confirmText = confirm;

            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Theme.SurfaceHigh;
            ClientSize = new Size(Theme.Dp(440), Theme.Dp(228));
            Padding = new Padding(0);

            M3Button cancelButton = new M3Button();
            cancelButton.Style = M3ButtonStyle.Text;
            cancelButton.Text = cancelText;
            cancelButton.BackColor = Theme.SurfaceHigh;
            cancelButton.Height = Theme.Dp(40);
            cancelButton.FitWidth();
            cancelButton.Location = new Point(Theme.Dp(24), ClientSize.Height - Theme.Dp(24) - Theme.Dp(40));
            cancelButton.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancelButton);

            M3Button confirmButton = new M3Button();
            confirmButton.Style = M3ButtonStyle.Danger;
            confirmButton.Text = confirmText;
            confirmButton.BackColor = Theme.SurfaceHigh;
            confirmButton.Height = Theme.Dp(40);
            confirmButton.FitWidth();
            confirmButton.Location = new Point(ClientSize.Width - Theme.Dp(24) - confirmButton.Width,
                ClientSize.Height - Theme.Dp(24) - Theme.Dp(40));
            confirmButton.Click += delegate { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(confirmButton);

            cancelButton.Focus();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int rounded = 2;
                DwmSetWindowAttribute(Handle, 33, ref rounded, 4);
            }
            catch { }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 0 && Height > 0)
            {
                Region old = Region;
                using (GraphicsPath path = Theme.RoundPath(new RectangleF(0, 0, Width, Height), Theme.Dp(Theme.RadiusDialog)))
                {
                    Region = new Region(path);
                }
                if (old != null) old.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using (SolidBrush bg = new SolidBrush(Theme.SurfaceHigh))
            {
                g.FillRectangle(bg, ClientRectangle);
            }

            Rectangle titleRect = new Rectangle(Theme.Dp(24), Theme.Dp(24), ClientSize.Width - Theme.Dp(48), Theme.Dp(32));
            TextRenderer.DrawText(g, titleText, Theme.TitleLarge, titleRect, Theme.OnSurface,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

            Rectangle bodyRect = new Rectangle(Theme.Dp(24), Theme.Dp(64), ClientSize.Width - Theme.Dp(48), Theme.Dp(96));
            TextRenderer.DrawText(g, bodyText, Theme.BodyMedium, bodyRect, Theme.OnSurfaceVariant,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            if (keyData == Keys.Enter)
            {
                DialogResult = DialogResult.OK;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
