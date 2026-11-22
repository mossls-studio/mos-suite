// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 MOSSLS

// Theme.cs — Material Design 3 (M3 Expressive) design tokens for the MOS Suite installer.
// Dark baseline scheme with proper Windows font resolution, 10-step shape scale,
// surface container elevation hierarchy, and state-layer alpha compositing.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
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

        public static float Dpf(float dp)
        {
            return dp * Scale;
        }

        // ---- M3 dark color roles (coherent tonal palette, ~90% neutral surface + <=10% primary accent) ----
        public static readonly Color Primary = Color.FromArgb(0xD0, 0xBC, 0xFF);
        public static readonly Color OnPrimary = Color.FromArgb(0x38, 0x1E, 0x72);
        public static readonly Color PrimaryContainer = Color.FromArgb(0x4F, 0x37, 0x8B);
        public static readonly Color OnPrimaryContainer = Color.FromArgb(0xEA, 0xDD, 0xFF);

        public static readonly Color Secondary = Color.FromArgb(0xCC, 0xC2, 0xDC);
        public static readonly Color OnSecondary = Color.FromArgb(0x33, 0x2D, 0x41);
        public static readonly Color SecondaryContainer = Color.FromArgb(0x4A, 0x44, 0x58);
        public static readonly Color OnSecondaryContainer = Color.FromArgb(0xE8, 0xDE, 0xF8);

        public static readonly Color Tertiary = Color.FromArgb(0xEF, 0xB8, 0xC8);
        public static readonly Color TertiaryContainer = Color.FromArgb(0x63, 0x3B, 0x48);
        public static readonly Color OnTertiaryContainer = Color.FromArgb(0xFF, 0xD8, 0xE4);

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

        // ---- M3 10-step shape scale ----
        public const int RadiusExtraSmall = 4;  // Text fields, menus, inner connected buttons
        public const int RadiusSmall = 8;       // Chips, compact badges
        public const int RadiusMedium = 12;     // Cards, pressed button morph target
        public const int RadiusLarge = 16;      // FAB, prominent containers
        public const int RadiusExtraLarge = 28; // Dialogs, sheets

        // ---- M3 state layer opacities ----
        public const float StateHover = 0.08f;
        public const float StateFocus = 0.10f;
        public const float StatePress = 0.10f;
        public const float StateDisabledContainer = 0.12f;
        public const float StateDisabledContent = 0.38f;

        // ---- M3 type scale (px at 96 dpi, strict official sizes) ----
        public static string Family = "Microsoft YaHei UI";
        public static string MediumFamily = "Microsoft YaHei UI";

        public static Font HeadlineMedium; // 28
        public static Font HeadlineSmall;  // 24
        public static Font TitleLarge;     // 22
        public static Font TitleMedium;    // 16 (500)
        public static Font TitleSmall;     // 14 (500)
        public static Font BodyLarge;      // 16
        public static Font BodyMedium;     // 14
        public static Font BodySmall;      // 12
        public static Font LabelLarge;     // 14 (500)
        public static Font LabelMedium;    // 12 (500)
        public static Font LabelSmall;     // 11 (500)

        private static HashSet<string> installedFonts;

        private static string PickFont(params string[] candidates)
        {
            if (installedFonts == null)
            {
                installedFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    using (InstalledFontCollection col = new InstalledFontCollection())
                    {
                        foreach (FontFamily f in col.Families)
                        {
                            installedFonts.Add(f.Name);
                        }
                    }
                }
                catch { }
            }
            for (int i = 0; i < candidates.Length; i++)
            {
                if (installedFonts.Contains(candidates[i])) return candidates[i];
            }
            return SystemFonts.MessageBoxFont.Name;
        }

        public static void Init(float scale, bool chinese)
        {
            Scale = scale;
            if (chinese)
            {
                Family = PickFont("Noto Sans SC", "Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI");
                MediumFamily = PickFont("Noto Sans SC Medium", "Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI Semibold");
            }
            else
            {
                Family = PickFont("Roboto", "Segoe UI Variable Text", "Segoe UI", "Microsoft YaHei UI");
                MediumFamily = PickFont("Roboto Medium", "Segoe UI Variable Display Semib", "Segoe UI Semibold", "Segoe UI");
            }

            bool emulateMedium = string.Equals(Family, MediumFamily, StringComparison.OrdinalIgnoreCase);
            FontStyle mediumStyle = emulateMedium ? FontStyle.Bold : FontStyle.Regular;

            DisposeFonts();
            HeadlineMedium = Make(Family, 28f, FontStyle.Regular);
            HeadlineSmall = Make(Family, 24f, FontStyle.Regular);
            TitleLarge = Make(Family, 22f, FontStyle.Regular);
            TitleMedium = Make(MediumFamily, 16f, mediumStyle);
            TitleSmall = Make(MediumFamily, 14f, mediumStyle);
            BodyLarge = Make(Family, 16f, FontStyle.Regular);
            BodyMedium = Make(Family, 14f, FontStyle.Regular);
            BodySmall = Make(Family, 12f, FontStyle.Regular);
            LabelLarge = Make(MediumFamily, 14f, mediumStyle);
            LabelMedium = Make(MediumFamily, 12f, mediumStyle);
            LabelSmall = Make(MediumFamily, 11f, mediumStyle);
        }

        private static void DisposeFonts()
        {
            Font[] all = {
                HeadlineMedium, HeadlineSmall, TitleLarge, TitleMedium, TitleSmall,
                BodyLarge, BodyMedium, BodySmall, LabelLarge, LabelMedium, LabelSmall
            };
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

        // ---- color helpers ----
        public static Color Blend(Color a, Color b, float t)
        {
            if (t <= 0f) return a;
            if (t >= 1f) return b;
            return Color.FromArgb(
                (int)Math.Round(a.A + (b.A - a.A) * t),
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }

        public static Color Over(Color baseColor, Color layer, float alpha)
        {
            if (alpha <= 0.001f) return baseColor;
            if (alpha > 1f) alpha = 1f;
            return Color.FromArgb(
                baseColor.A,
                (int)Math.Round(baseColor.R * (1f - alpha) + layer.R * alpha),
                (int)Math.Round(baseColor.G * (1f - alpha) + layer.G * alpha),
                (int)Math.Round(baseColor.B * (1f - alpha) + layer.B * alpha));
        }

        public static Color Fade(Color color, float alpha)
        {
            int a = (int)Math.Round(Math.Max(0f, Math.Min(1f, alpha)) * 255f);
            return Color.FromArgb(a, color.R, color.G, color.B);
        }

        // ---- shape & vector drawing helpers ----
        public static GraphicsPath RoundPath(RectangleF r, float radius)
        {
            return RoundPath(r, radius, radius, radius, radius);
        }

        public static GraphicsPath RoundPath(RectangleF r, float tl, float tr, float br, float bl)
        {
            GraphicsPath p = new GraphicsPath();
            float maxR = Math.Min(r.Width, r.Height) * 0.5f;
            tl = Math.Max(0.5f, Math.Min(tl, maxR));
            tr = Math.Max(0.5f, Math.Min(tr, maxR));
            br = Math.Max(0.5f, Math.Min(br, maxR));
            bl = Math.Max(0.5f, Math.Min(bl, maxR));

            p.AddArc(r.X, r.Y, tl * 2f, tl * 2f, 180f, 90f);
            p.AddArc(r.Right - tr * 2f, r.Y, tr * 2f, tr * 2f, 270f, 90f);
            p.AddArc(r.Right - br * 2f, r.Bottom - br * 2f, br * 2f, br * 2f, 0f, 90f);
            p.AddArc(r.X, r.Bottom - bl * 2f, bl * 2f, bl * 2f, 90f, 90f);
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

        public static void FillRound(Graphics g, Color color, RectangleF r, float tl, float tr, float br, float bl)
        {
            using (GraphicsPath p = RoundPath(r, tl, tr, br, bl))
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

        public static void DrawRipples(Graphics g, GraphicsPath clipPath, List<RippleWave> ripples, Color tint)
        {
            if (ripples == null || ripples.Count == 0) return;
            Region oldClip = g.Clip;
            g.SetClip(clipPath, CombineMode.Intersect);
            for (int i = 0; i < ripples.Count; i++)
            {
                RippleWave w = ripples[i];
                float rad = Math.Max(2f, w.MaxRadius * w.Progress);
                Color c = Fade(tint, 0.14f * w.Alpha);
                using (SolidBrush b = new SolidBrush(c))
                {
                    g.FillEllipse(b, w.X - rad, w.Y - rad, rad * 2f, rad * 2f);
                }
            }
            g.Clip = oldClip;
        }

        public static int Measure(string text, Font font)
        {
            return TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
        }

        public static void DrawText(Graphics g, string text, Font font, Color color, RectangleF rect, TextFormatFlags flags)
        {
            TextRenderer.DrawText(g, text, font, Rectangle.Round(rect), color,
                flags | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }

        public static void DrawCheck(Graphics g, RectangleF box, Color color, float width, float progress)
        {
            if (progress <= 0.01f) return;
            PointF a = new PointF(box.X + box.Width * 0.22f, box.Y + box.Height * 0.52f);
            PointF b = new PointF(box.X + box.Width * 0.42f, box.Y + box.Height * 0.72f);
            PointF c = new PointF(box.X + box.Width * 0.78f, box.Y + box.Height * 0.30f);

            using (Pen pen = new Pen(color, width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                if (progress <= 0.42f)
                {
                    float t = progress / 0.42f;
                    PointF p = new PointF(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
                    g.DrawLine(pen, a, p);
                }
                else
                {
                    float t = Math.Min(1f, (progress - 0.42f) / 0.58f);
                    PointF p = new PointF(b.X + (c.X - b.X) * t, b.Y + (c.Y - b.Y) * t);
                    g.DrawLines(pen, new PointF[] { a, b, p });
                }
            }
        }

        public static void DrawClose(Graphics g, RectangleF r, Color color, float width)
        {
            using (Pen pen = new Pen(color, width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                float inset = r.Width * 0.30f;
                g.DrawLine(pen, r.X + inset, r.Y + inset, r.Right - inset, r.Bottom - inset);
                g.DrawLine(pen, r.Right - inset, r.Y + inset, r.X + inset, r.Bottom - inset);
            }
        }

        public static void DrawMinimize(Graphics g, RectangleF r, Color color, float width)
        {
            using (Pen pen = new Pen(color, width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                float inset = r.Width * 0.28f;
                float cy = r.Y + r.Height * 0.54f;
                g.DrawLine(pen, r.X + inset, cy, r.Right - inset, cy);
            }
        }

        public static void DrawArrowRight(Graphics g, RectangleF r, Color color, float width)
        {
            using (Pen pen = new Pen(color, width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                float cy = r.Y + r.Height * 0.5f;
                g.DrawLine(pen, r.X + r.Width * 0.22f, cy, r.X + r.Width * 0.78f, cy);
                g.DrawLine(pen, r.X + r.Width * 0.54f, r.Y + r.Height * 0.26f, r.X + r.Width * 0.78f, cy);
                g.DrawLine(pen, r.X + r.Width * 0.54f, r.Y + r.Height * 0.74f, r.X + r.Width * 0.78f, cy);
            }
        }

        public static void DrawFolderIcon(Graphics g, RectangleF r, Color color, float width)
        {
            using (Pen pen = new Pen(color, width))
            {
                pen.LineJoin = LineJoin.Round;
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                float x = r.X + r.Width * 0.14f;
                float y = r.Y + r.Height * 0.22f;
                float w = r.Width * 0.72f;
                float h = r.Height * 0.56f;
                PointF[] pts = new PointF[] {
                    new PointF(x, y),
                    new PointF(x + w * 0.38f, y),
                    new PointF(x + w * 0.48f, y + h * 0.18f),
                    new PointF(x + w, y + h * 0.18f),
                    new PointF(x + w, y + h),
                    new PointF(x, y + h)
                };
                g.DrawPolygon(pen, pts);
            }
        }
    }
}
