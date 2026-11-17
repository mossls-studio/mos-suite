// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 MOSSLS

// Theme.cs — Material 3 design tokens for the MOS suite installer.
// Baseline light scheme, fixed type scale, per-component shape scale. C# 5 syntax on purpose:
// the installer builds with the .NET Framework csc.exe that ships with Windows.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MosSuite
{
    internal static class Theme
    {
        public static float Scale = 1f;

        public static int Dp(float dp)
        {
            return (int)Math.Round(dp * Scale);
        }

        // ---- color roles (M3 baseline dark, used by default) ----
        public static readonly Color Primary = Color.FromArgb(0xD0, 0xBC, 0xFF);
        public static readonly Color OnPrimary = Color.FromArgb(0x38, 0x1E, 0x72);
        public static readonly Color PrimaryContainer = Color.FromArgb(0x4F, 0x37, 0x8B);
        public static readonly Color OnPrimaryContainer = Color.FromArgb(0xEA, 0xDD, 0xFF);
        public static readonly Color Secondary = Color.FromArgb(0xCC, 0xC2, 0xDC);
        public static readonly Color SecondaryContainer = Color.FromArgb(0x4A, 0x44, 0x58);
        public static readonly Color OnSecondaryContainer = Color.FromArgb(0xE8, 0xDE, 0xF8);
        public static readonly Color Error = Color.FromArgb(0xF2, 0xB8, 0xB5);
        public static readonly Color OnError = Color.FromArgb(0x60, 0x14, 0x10);
        public static readonly Color ErrorContainer = Color.FromArgb(0x8C, 0x1D, 0x18);
        public static readonly Color OnErrorContainer = Color.FromArgb(0xF9, 0xDE, 0xDC);
        public static readonly Color Surface = Color.FromArgb(0x14, 0x12, 0x18);
        public static readonly Color SurfaceLowest = Color.FromArgb(0x0F, 0x0D, 0x13);
        public static readonly Color SurfaceLow = Color.FromArgb(0x1D, 0x1B, 0x20);
        public static readonly Color SurfaceContainer = Color.FromArgb(0x21, 0x1F, 0x26);
        public static readonly Color SurfaceHigh = Color.FromArgb(0x2B, 0x29, 0x30);
        public static readonly Color SurfaceHighest = Color.FromArgb(0x36, 0x34, 0x3B);
        public static readonly Color OnSurface = Color.FromArgb(0xE6, 0xE0, 0xE9);
        public static readonly Color OnSurfaceVariant = Color.FromArgb(0xCA, 0xC4, 0xD0);
        public static readonly Color Outline = Color.FromArgb(0x93, 0x8F, 0x99);
        public static readonly Color OutlineVariant = Color.FromArgb(0x49, 0x45, 0x4F);
        public static readonly Color InverseSurface = Color.FromArgb(0xE6, 0xE0, 0xE9);

        // ---- shape scale ----
        public const int RadiusChip = 8;
        public const int RadiusCard = 12;
        public const int RadiusDialog = 28;

        // ---- type scale (px at 96 dpi) ----
        public static string Family = "Roboto";
        public static string MediumFamily = "Roboto Medium";

        public static Font HeadlineSmall;
        public static Font TitleLarge;
        public static Font TitleMedium;
        public static Font BodyLarge;
        public static Font BodyMedium;
        public static Font BodySmall;
        public static Font BodyTiny;
        public static Font LabelLarge;
        public static Font LabelMedium;
        public static Font LabelSmall;
        public static Font Monogram;

        public static void Init(float scale, bool chinese)
        {
            Scale = scale;
            Family = chinese ? "Noto Sans SC" : "Roboto";
            MediumFamily = chinese ? "Noto Sans SC Medium" : "Roboto Medium";

            DisposeFonts();
            HeadlineSmall = Make(Family, 24f, FontStyle.Regular);
            TitleLarge = Make(MediumFamily, 22f, FontStyle.Regular);
            TitleMedium = Make(MediumFamily, 16f, FontStyle.Regular);
            BodyLarge = Make(Family, 16f, FontStyle.Regular);
            BodyMedium = Make(Family, 14f, FontStyle.Regular);
            BodySmall = Make(Family, 12f, FontStyle.Regular);
            BodyTiny = Make(Family, 10f, FontStyle.Regular);
            LabelLarge = Make(MediumFamily, 14f, FontStyle.Regular);
            LabelMedium = Make(MediumFamily, 12f, FontStyle.Regular);
            LabelSmall = Make(MediumFamily, 11f, FontStyle.Regular);
            Monogram = Make(MediumFamily, 18f, FontStyle.Regular);
        }

        private static void DisposeFonts()
        {
            Font[] all = { HeadlineSmall, TitleLarge, TitleMedium, BodyLarge, BodyMedium, BodySmall, BodyTiny, LabelLarge, LabelMedium, LabelSmall, Monogram };
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null) all[i].Dispose();
            }
        }

        private static Font Make(string family, float px, FontStyle style)
        {
            return new Font(family, px * Scale, style, GraphicsUnit.Pixel);
        }

        // ---- brand mark ----
        private static Image logo;

        public static Image Logo()
        {
            if (logo == null)
            {
                try
                {
                    using (System.IO.Stream s = typeof(Theme).Assembly.GetManifestResourceStream("Logo"))
                    {
                        if (s != null)
                        {
                            using (Image raw = Image.FromStream(s))
                            {
                                logo = new Bitmap(raw);
                            }
                        }
                    }
                }
                catch { }
            }
            return logo;
        }

        public static void DrawLogo(Graphics g, RectangleF rect)
        {
            Image img = Logo();
            if (img == null) return;
            InterpolationMode old = g.InterpolationMode;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(img, rect);
            g.InterpolationMode = old;
        }

        // ---- drawing helpers ----
        public static GraphicsPath RoundPath(RectangleF r, float radius)
        {
            GraphicsPath p = new GraphicsPath();
            float d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
            if (d < 1f) d = 1f;
            p.AddArc(r.X, r.Y, d, d, 180f, 90f);
            p.AddArc(r.Right - d, r.Y, d, d, 270f, 90f);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0f, 90f);
            p.AddArc(r.X, r.Bottom - d, d, d, 90f, 90f);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Color color, RectangleF r, float radius)
        {
            using (GraphicsPath p = RoundPath(r, radius))
            using (SolidBrush b = new SolidBrush(color))
            {
                g.FillPath(b, p);
            }
        }

        public static void StrokeRound(Graphics g, Color color, RectangleF r, float radius, float width)
        {
            using (GraphicsPath p = RoundPath(r, radius))
            using (Pen pen = new Pen(color, width))
            {
                pen.Alignment = PenAlignment.Inset;
                g.DrawPath(pen, p);
            }
        }

        public static Color Over(Color baseColor, Color layer, int alpha)
        {
            float a = alpha / 255f;
            return Color.FromArgb(
                baseColor.A,
                (int)Math.Round(baseColor.R * (1 - a) + layer.R * a),
                (int)Math.Round(baseColor.G * (1 - a) + layer.G * a),
                (int)Math.Round(baseColor.B * (1 - a) + layer.B * a));
        }

        public static Color Fade(Color color, int alpha)
        {
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        public static int Measure(string text, Font font)
        {
            return TextRenderer.MeasureText(text, font).Width;
        }

        public static void DrawText(Graphics g, string text, Font font, Color color, RectangleF rect, TextFormatFlags flags)
        {
            TextRenderer.DrawText(g, text, font, Rectangle.Round(rect), color,
                flags | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }

        public static void Text(Graphics g, string text, Font font, Color color, RectangleF rect)
        {
            DrawText(g, text, font, color, rect, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        public static void DrawCheck(Graphics g, RectangleF box, Color color, float width)
        {
            using (Pen pen = new Pen(color, width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                PointF a = new PointF(box.X + box.Width * 0.24f, box.Y + box.Height * 0.52f);
                PointF b = new PointF(box.X + box.Width * 0.43f, box.Y + box.Height * 0.71f);
                PointF c = new PointF(box.X + box.Width * 0.76f, box.Y + box.Height * 0.31f);
                g.DrawLines(pen, new PointF[] { a, b, c });
            }
        }

        public static void DrawClose(Graphics g, RectangleF r, Color color, float width)
        {
            using (Pen pen = new Pen(color, width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                float inset = r.Width * 0.28f;
                g.DrawLine(pen, r.X + inset, r.Y + inset, r.Right - inset, r.Bottom - inset);
                g.DrawLine(pen, r.Right - inset, r.Y + inset, r.X + inset, r.Bottom - inset);
            }
        }

        public static void DrawArrowDown(Graphics g, RectangleF r, Color color, float width)
        {
            using (Pen pen = new Pen(color, width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                float cx = r.X + r.Width / 2f;
                g.DrawLine(pen, cx, r.Y + r.Height * 0.2f, cx, r.Y + r.Height * 0.78f);
                g.DrawLine(pen, cx - r.Width * 0.18f, r.Y + r.Height * 0.58f, cx, r.Y + r.Height * 0.78f);
                g.DrawLine(pen, cx + r.Width * 0.18f, r.Y + r.Height * 0.58f, cx, r.Y + r.Height * 0.78f);
            }
        }
    }
}
