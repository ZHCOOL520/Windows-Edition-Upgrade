using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace UpgradeTool
{
    internal static class Theme
    {
        public static bool Light;
        public static float Scale = 1f;

        public static Color BgTop;
        public static Color BgBottom;
        public static Color Card;
        public static Color CardHover;
        public static Color Line;
        public static Color Accent;
        public static Color Accent2;
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

        public static Font Title;
        public static Font H1;
        public static Font H2;
        public static Font Body;
        public static Font Small;
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
            bool needFonts = Title == null || Math.Abs(Scale - scale) > 0.001f;
            Light = light;
            Scale = scale;

            if (light)
            {
                BgTop = Color.FromArgb(246, 248, 252);
                BgBottom = Color.FromArgb(255, 255, 255);
                Card = Color.FromArgb(255, 255, 255);
                CardHover = Color.FromArgb(240, 245, 252);
                Line = Color.FromArgb(213, 221, 233);
                Accent = Color.FromArgb(37, 99, 235);
                Accent2 = Color.FromArgb(13, 148, 176);
                Text = Color.FromArgb(24, 32, 46);
                Muted = Color.FromArgb(92, 105, 126);
                Dim = Color.FromArgb(138, 150, 168);
                Warn = Color.FromArgb(180, 105, 6);
                Danger = Color.FromArgb(203, 62, 62);
                Ok = Color.FromArgb(5, 150, 105);
                Sunken = Color.FromArgb(241, 244, 249);
                BadgeBg = Color.FromArgb(228, 234, 244);
                HeaderTop = Color.FromArgb(232, 238, 249);
                Ghost = Color.FromArgb(232, 236, 243);
            }
            else
            {
                BgTop = Color.FromArgb(13, 18, 30);
                BgBottom = Color.FromArgb(20, 27, 43);
                Card = Color.FromArgb(23, 30, 46);
                CardHover = Color.FromArgb(31, 41, 62);
                Line = Color.FromArgb(42, 53, 76);
                Accent = Color.FromArgb(76, 141, 255);
                Accent2 = Color.FromArgb(34, 211, 238);
                Text = Color.FromArgb(230, 237, 247);
                Muted = Color.FromArgb(138, 151, 173);
                Dim = Color.FromArgb(96, 108, 130);
                Warn = Color.FromArgb(245, 158, 11);
                Danger = Color.FromArgb(239, 92, 92);
                Ok = Color.FromArgb(52, 211, 153);
                Sunken = Color.FromArgb(19, 25, 38);
                BadgeBg = Color.FromArgb(30, 39, 56);
                HeaderTop = Color.FromArgb(18, 26, 42);
                Ghost = Color.FromArgb(30, 38, 54);
            }

            if (needFonts)
            {
                Title = new Font("Microsoft YaHei UI", 17f * scale, FontStyle.Bold);
                H1 = new Font("Microsoft YaHei UI", 14f * scale, FontStyle.Bold);
                H2 = new Font("Microsoft YaHei UI", 10.5f * scale, FontStyle.Bold);
                Body = new Font("Microsoft YaHei UI", 9.5f * scale);
                Small = new Font("Microsoft YaHei UI", 8.5f * scale);
                Mono = new Font("Consolas", 9.5f * scale);
                MonoSmall = new Font("Consolas", 8.5f * scale);
            }
        }

        public static Color Lerp(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Rectangle r, int radius, Color c)
        {
            using (GraphicsPath p = RoundRect(r, radius))
            using (SolidBrush b = new SolidBrush(c))
            {
                g.FillPath(b, p);
            }
        }

        public static void StrokeRound(Graphics g, Rectangle r, int radius, Color c, float width)
        {
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
        }
    }

    internal class UButton : Control
    {
        public bool Primary { get; set; }
        public bool Hover { get; private set; }
        public bool Danger { get; set; }

        public UButton(string text)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = text;
            Font = Theme.H2;
            Cursor = Cursors.Hand;
            Size = new Size(Theme.S(132), Theme.S(38));
            ForeColor = Theme.Text;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            Hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            Hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color baseColor;
            if (Primary)
                baseColor = Danger ? Theme.Danger : Theme.Accent;
            else
                baseColor = Theme.Card;

            Color fill = Enabled ? baseColor : Theme.Ghost;
            if (Hover && Enabled)
                fill = Theme.Lerp(baseColor, Theme.Light ? Color.Black : Color.White, Primary ? 0.12f : 0.07f);

            int radius = Theme.S(9);
            Theme.FillRound(g, r, radius, fill);
            if (!Primary)
                Theme.StrokeRound(g, r, radius,
                    Hover && Enabled ? Theme.Lerp(Theme.Line, Theme.Accent, 0.45f) : Theme.Line, 1f);

            Color fg = Enabled ? (Primary ? Color.White : Theme.Text) : Theme.Dim;
            TextRenderer.DrawText(g, Text, Font, r, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    internal class AnimatedCard : Control
    {
        public float HoverProgress;
        public bool HoverTarget;
        public bool Selected;
        public bool IsHeader;
        public string SubText = "";
        public string RightText = "";
        public Color AccentColor = Theme.Accent;
        public int Index = -1;

        public AnimatedCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Height = Theme.S(52);
            Font = Theme.Body;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            HoverTarget = true;
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            HoverTarget = false;
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            Rectangle r = new Rectangle(0, Theme.S(1), Width - 1, Height - Theme.S(3));
            int radius = Theme.S(9);

            if (IsHeader)
            {
                TextRenderer.DrawText(g, Text, Theme.Small,
                    new Rectangle(Theme.S(6), 0, Width - Theme.S(12), Height),
                    Theme.Dim, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return;
            }

            Color fill = Theme.Lerp(Theme.Card, Theme.CardHover, HoverProgress);
            if (Selected) fill = Theme.Lerp(Theme.Card, Theme.Accent, Theme.Light ? 0.10f : 0.16f);
            Theme.FillRound(g, r, radius, fill);

            if (Selected)
                Theme.StrokeRound(g, r, radius, Theme.Lerp(Theme.Line, AccentColor, 0.8f), Theme.SM(1.4f));
            else if (HoverProgress > 0.01f)
                Theme.StrokeRound(g, r, radius, Theme.Lerp(Theme.Card, Theme.Line, HoverProgress), 1f);
            else
                Theme.StrokeRound(g, r, radius, Theme.Line, 1f);

            Rectangle bar = new Rectangle(r.X, r.Y + Theme.S(10), Theme.S(3), r.Height - Theme.S(20));
            if (Selected)
            {
                using (GraphicsPath p = Theme.RoundRect(bar, Theme.S(2)))
                using (SolidBrush b = new SolidBrush(AccentColor))
                {
                    g.FillPath(b, p);
                }
            }

            int left = Theme.S(16);
            TextRenderer.DrawText(g, Text, Selected ? Theme.H2 : Theme.Body,
                new Rectangle(left, Theme.S(6), Width - Theme.S(200), Theme.S(22)), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

            if (SubText.Length > 0)
                TextRenderer.DrawText(g, SubText, Theme.MonoSmall,
                    new Rectangle(left, Theme.S(27), Width - Theme.S(200), Theme.S(18)), Theme.Muted,
                    TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

            if (RightText.Length > 0)
                TextRenderer.DrawText(g, RightText, Theme.MonoSmall,
                    new Rectangle(left, Theme.S(6), Width - left - Theme.S(12), Height - Theme.S(10)),
                    Selected ? Theme.Accent2 : Theme.Dim,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    internal class Spinner
    {
        public float Angle;
        public bool Active { get; set; }

        public void Draw(Graphics g, Rectangle r, Color c)
        {
            if (!Active) return;
            Theme.EnableSmooth(g);
            using (Pen p = new Pen(c, Theme.SM(2.2f)))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                g.DrawArc(p, r, Angle, 100f);
            }
            using (Pen p2 = new Pen(Color.FromArgb(60, c), Theme.SM(2.2f)))
            {
                g.DrawArc(p2, r, Angle + 140f, 200f);
            }
        }
    }
}
