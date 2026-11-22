using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace UpgradeTool
{
    // WinUI 3 / Fluent 语义色集合，用于主题切换时的颜色渐变
    internal sealed class Palette
    {
        public Color Layer;
        public Color LayerAlt;
        public Color Card;
        public Color CardHover;
        public Color CardPressed;
        public Color CardStroke;
        public Color Divider;
        public Color TextPrimary;
        public Color TextSecondary;
        public Color TextTertiary;
        public Color Accent;
        public Color AccentHover;
        public Color AccentPressed;
        public Color AccentSoft;
        public Color AccentSoftStrong;
        public Color OnAccent;
        public Color Subtle;
        public Color SubtleHover;
        public Color ControlFill;
        public Color ControlStroke;
        public Color Sunken;
        public Color Badge;
        public Color Warn;
        public Color Danger;
        public Color Ok;
        public Color Ghost;
        public Color HeaderTop;
    }

    internal static class Theme
    {
        public static bool Light;
        public static float Scale = 1f;

        // 语义色（同时兼容旧字段名）
        public static Color BgTop;
        public static Color BgBottom;
        public static Color Card;
        public static Color CardHover;
        public static Color CardPressed;
        public static Color Line;
        public static Color CardStroke;
        public static Color Divider;
        public static Color Accent;
        public static Color AccentHover;
        public static Color AccentPressed;
        public static Color AccentSoft;
        public static Color AccentSoftStrong;
        public static Color Accent2;
        public static Color OnAccent;
        public static Color Text;
        public static Color Muted;
        public static Color Dim;
        public static Color Warn;
        public static Color Danger;
        public static Color Ok;
        public static Color Sunken;
        public static Color BadgeBg;
        public static Color HeaderTop;
        public static Color Ghost;
        public static Color Subtle;
        public static Color SubtleHover;
        public static Color ControlFill;
        public static Color ControlStroke;

        public static Font Title;
        public static Font H1;
        public static Font H2;
        public static Font Body;
        public static Font BodyStrong;
        public static Font Small;
        public static Font Caption;
        public static Font Mono;
        public static Font MonoSmall;

        static Theme()
        {
            Init(false, 1f);
        }

        public static int S(int v)
        {
            return (int)Math.Round(v * Scale);
        }

        public static float SM(float v)
        {
            return v * Scale;
        }

        public static void Init(bool light, float scale)
        {
            if (scale < 0.7f) scale = 0.7f;
            if (scale > 3.5f) scale = 3.5f;
            Light = light;
            Scale = scale;
            Apply(Target(light));
            BuildFonts(scale);
        }

        private static void BuildFonts(float scale)
        {
            Font[] old = { Title, H1, H2, Body, BodyStrong, Small, Caption, Mono, MonoSmall };
            Title = null; H1 = null; H2 = null; Body = null; BodyStrong = null;
            Small = null; Caption = null; Mono = null; MonoSmall = null;
            foreach (Font f in old)
            {
                if (f != null)
                {
                    try { f.Dispose(); }
                    catch { }
                }
            }

            string ui = "Microsoft YaHei UI";
            Title = new Font(ui, 15f * scale, FontStyle.Bold);
            H1 = new Font(ui, 18f * scale, FontStyle.Bold);
            H2 = new Font(ui, 10.5f * scale, FontStyle.Bold);
            BodyStrong = new Font(ui, 10f * scale, FontStyle.Bold);
            Body = new Font(ui, 9.5f * scale, FontStyle.Regular);
            Small = new Font(ui, 8.5f * scale, FontStyle.Regular);
            Caption = new Font(ui, 8f * scale, FontStyle.Regular);
            Mono = new Font("Consolas", 9.5f * scale);
            MonoSmall = new Font("Consolas", 8.5f * scale);
        }

        public static Color Lerp(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static double Luminance(Color c)
        {
            return (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
        }

        public static Color TextOn(Color c)
        {
            return Luminance(c) > 0.6 ? Color.FromArgb(20, 20, 20) : Color.White;
        }

        public static Color SystemAccent()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM"))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("AccentColor");
                        if (v is int)
                        {
                            int n = (int)v;
                            int a = (n >> 24) & 0xFF;
                            int b = (n >> 16) & 0xFF;
                            int g = (n >> 8) & 0xFF;
                            int r = n & 0xFF;
                            if (a == 0) a = 255;
                            if (r + g + b > 30)
                                return Color.FromArgb(255, r, g, b);
                        }
                    }
                }
            }
            catch
            {
            }
            return Color.FromArgb(0, 120, 212);
        }

        private static Color ReadableAccent(Color c, bool light)
        {
            int i = 0;
            if (light)
            {
                while (Luminance(c) > 0.44 && i < 12)
                {
                    c = Lerp(c, Color.Black, 0.12f);
                    i++;
                }
            }
            else
            {
                while (Luminance(c) < 0.32 && i < 12)
                {
                    c = Lerp(c, Color.White, 0.1f);
                    i++;
                }
            }
            return c;
        }

        public static Palette Target(bool light)
        {
            Palette p = new Palette();
            Color sys = SystemAccent();
            Color acc = ReadableAccent(sys, light);
            p.Accent = acc;
            p.OnAccent = TextOn(acc);

            if (light)
            {
                p.Layer = Color.FromArgb(243, 243, 243);
                p.LayerAlt = Color.FromArgb(249, 249, 249);
                p.Card = Color.FromArgb(255, 255, 255);
                p.CardHover = Color.FromArgb(249, 249, 249);
                p.CardPressed = Color.FromArgb(242, 242, 242);
                p.CardStroke = Color.FromArgb(229, 229, 229);
                p.Divider = Color.FromArgb(222, 222, 222);
                p.TextPrimary = Color.FromArgb(26, 26, 26);
                p.TextSecondary = Color.FromArgb(91, 91, 91);
                p.TextTertiary = Color.FromArgb(138, 138, 138);
                p.AccentHover = Lerp(acc, Color.Black, 0.08f);
                p.AccentPressed = Lerp(acc, Color.Black, 0.16f);
                p.AccentSoft = Color.FromArgb(22, acc);
                p.AccentSoftStrong = Color.FromArgb(54, acc);
                p.Subtle = Color.FromArgb(0, 0, 0, 0);
                p.SubtleHover = Color.FromArgb(12, 0, 0, 0);
                p.ControlFill = Color.FromArgb(190, 255, 255, 255);
                p.ControlStroke = Color.FromArgb(28, 0, 0, 0);
                p.Sunken = Color.FromArgb(247, 247, 247);
                p.Badge = Color.FromArgb(236, 236, 236);
                p.Warn = Color.FromArgb(157, 93, 0);
                p.Danger = Color.FromArgb(196, 43, 28);
                p.Ok = Color.FromArgb(15, 123, 15);
                p.Ghost = Color.FromArgb(234, 234, 234);
                p.HeaderTop = Color.FromArgb(248, 248, 248);
            }
            else
            {
                p.Layer = Color.FromArgb(32, 32, 32);
                p.LayerAlt = Color.FromArgb(39, 39, 39);
                p.Card = Color.FromArgb(43, 43, 43);
                p.CardHover = Color.FromArgb(50, 50, 50);
                p.CardPressed = Color.FromArgb(38, 38, 38);
                p.CardStroke = Color.FromArgb(58, 58, 58);
                p.Divider = Color.FromArgb(56, 56, 56);
                p.TextPrimary = Color.FromArgb(255, 255, 255);
                p.TextSecondary = Color.FromArgb(200, 200, 200);
                p.TextTertiary = Color.FromArgb(148, 148, 148);
                p.AccentHover = Lerp(acc, Color.White, 0.10f);
                p.AccentPressed = Lerp(acc, Color.Black, 0.10f);
                p.AccentSoft = Color.FromArgb(32, acc);
                p.AccentSoftStrong = Color.FromArgb(78, acc);
                p.Subtle = Color.FromArgb(0, 255, 255, 255);
                p.SubtleHover = Color.FromArgb(16, 255, 255, 255);
                p.ControlFill = Color.FromArgb(14, 255, 255, 255);
                p.ControlStroke = Color.FromArgb(22, 255, 255, 255);
                p.Sunken = Color.FromArgb(26, 26, 26);
                p.Badge = Color.FromArgb(58, 58, 58);
                p.Warn = Color.FromArgb(252, 225, 0);
                p.Danger = Color.FromArgb(255, 153, 164);
                p.Ok = Color.FromArgb(108, 203, 95);
                p.Ghost = Color.FromArgb(49, 49, 49);
                p.HeaderTop = Color.FromArgb(37, 37, 37);
            }
            return p;
        }

        public static Palette Capture()
        {
            Palette p = new Palette();
            p.Layer = BgTop;
            p.LayerAlt = BgBottom;
            p.Card = Card;
            p.CardHover = CardHover;
            p.CardPressed = CardPressed;
            p.CardStroke = Line;
            p.Divider = Divider;
            p.TextPrimary = Text;
            p.TextSecondary = Muted;
            p.TextTertiary = Dim;
            p.Accent = Accent;
            p.AccentHover = AccentHover;
            p.AccentPressed = AccentPressed;
            p.AccentSoft = AccentSoft;
            p.AccentSoftStrong = AccentSoftStrong;
            p.OnAccent = OnAccent;
            p.Subtle = Subtle;
            p.SubtleHover = SubtleHover;
            p.ControlFill = ControlFill;
            p.ControlStroke = ControlStroke;
            p.Sunken = Sunken;
            p.Badge = BadgeBg;
            p.Warn = Warn;
            p.Danger = Danger;
            p.Ok = Ok;
            p.Ghost = Ghost;
            p.HeaderTop = HeaderTop;
            return p;
        }

        public static void Apply(Palette p)
        {
            BgTop = p.Layer;
            BgBottom = p.LayerAlt;
            Card = p.Card;
            CardHover = p.CardHover;
            CardPressed = p.CardPressed;
            Line = p.CardStroke;
            CardStroke = p.CardStroke;
            Divider = p.Divider;
            Text = p.TextPrimary;
            Muted = p.TextSecondary;
            Dim = p.TextTertiary;
            Accent = p.Accent;
            AccentHover = p.AccentHover;
            AccentPressed = p.AccentPressed;
            AccentSoft = p.AccentSoft;
            AccentSoftStrong = p.AccentSoftStrong;
            Accent2 = p.Accent;
            OnAccent = p.OnAccent;
            Subtle = p.Subtle;
            SubtleHover = p.SubtleHover;
            ControlFill = p.ControlFill;
            ControlStroke = p.ControlStroke;
            Sunken = p.Sunken;
            BadgeBg = p.Badge;
            Warn = p.Warn;
            Danger = p.Danger;
            Ok = p.Ok;
            Ghost = p.Ghost;
            HeaderTop = p.HeaderTop;
        }

        public static void Blend(Palette a, Palette b, float t)
        {
            Palette p = new Palette();
            p.Layer = Lerp(a.Layer, b.Layer, t);
            p.LayerAlt = Lerp(a.LayerAlt, b.LayerAlt, t);
            p.Card = Lerp(a.Card, b.Card, t);
            p.CardHover = Lerp(a.CardHover, b.CardHover, t);
            p.CardPressed = Lerp(a.CardPressed, b.CardPressed, t);
            p.CardStroke = Lerp(a.CardStroke, b.CardStroke, t);
            p.Divider = Lerp(a.Divider, b.Divider, t);
            p.TextPrimary = Lerp(a.TextPrimary, b.TextPrimary, t);
            p.TextSecondary = Lerp(a.TextSecondary, b.TextSecondary, t);
            p.TextTertiary = Lerp(a.TextTertiary, b.TextTertiary, t);
            p.Accent = Lerp(a.Accent, b.Accent, t);
            p.AccentHover = Lerp(a.AccentHover, b.AccentHover, t);
            p.AccentPressed = Lerp(a.AccentPressed, b.AccentPressed, t);
            p.AccentSoft = Lerp(a.AccentSoft, b.AccentSoft, t);
            p.AccentSoftStrong = Lerp(a.AccentSoftStrong, b.AccentSoftStrong, t);
            p.OnAccent = Lerp(a.OnAccent, b.OnAccent, t);
            p.Subtle = Lerp(a.Subtle, b.Subtle, t);
            p.SubtleHover = Lerp(a.SubtleHover, b.SubtleHover, t);
            p.ControlFill = Lerp(a.ControlFill, b.ControlFill, t);
            p.ControlStroke = Lerp(a.ControlStroke, b.ControlStroke, t);
            p.Sunken = Lerp(a.Sunken, b.Sunken, t);
            p.Badge = Lerp(a.Badge, b.Badge, t);
            p.Warn = Lerp(a.Warn, b.Warn, t);
            p.Danger = Lerp(a.Danger, b.Danger, t);
            p.Ok = Lerp(a.Ok, b.Ok, t);
            p.Ghost = Lerp(a.Ghost, b.Ghost, t);
            p.HeaderTop = Lerp(a.HeaderTop, b.HeaderTop, t);
            Apply(p);
        }

        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 0) d = 1;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Rectangle r, int radius, Color c)
        {
            if (c.A == 0) return;
            using (GraphicsPath p = RoundRect(r, radius))
            using (SolidBrush b = new SolidBrush(c))
            {
                g.FillPath(b, p);
            }
        }

        public static void StrokeRound(Graphics g, Rectangle r, int radius, Color c, float width)
        {
            if (c.A == 0) return;
            using (GraphicsPath p = RoundRect(r, radius))
            using (Pen pen = new Pen(c, width))
            {
                g.DrawPath(pen, p);
            }
        }

        public static void EnableSmooth(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }
    }
}
