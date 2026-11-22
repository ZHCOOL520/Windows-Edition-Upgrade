using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace UpgradeTool
{
    internal enum GlyphKind
    {
        Minimize,
        Close,
        Info,
        Sun,
        Moon,
        Disc,
        Grid,
        Play,
        Key,
        ArrowUp,
        Target,
        Warning,
        Refresh,
        Download,
        Check,
        ChevronLeft,
        ChevronRight
    }

    // 全部图标使用矢量绘制，避免依赖 Segoe MDL2 / Fluent Icons 字体
    internal static class Glyphs
    {
        public static void Draw(Graphics g, GlyphKind kind, Rectangle r, Color c, float w)
        {
            Theme.EnableSmooth(g);
            int s = Math.Min(r.Width, r.Height);
            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;
            using (Pen p = new Pen(c, w))
            using (SolidBrush b = new SolidBrush(c))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;
                switch (kind)
                {
                    case GlyphKind.Minimize:
                        g.DrawLine(p, cx - s * 0.22f, cy, cx + s * 0.22f, cy);
                        break;
                    case GlyphKind.Close:
                        g.DrawLine(p, cx - s * 0.20f, cy - s * 0.20f, cx + s * 0.20f, cy + s * 0.20f);
                        g.DrawLine(p, cx + s * 0.20f, cy - s * 0.20f, cx - s * 0.20f, cy + s * 0.20f);
                        break;
                    case GlyphKind.Info:
                        {
                            float rad = s * 0.36f;
                            g.DrawEllipse(p, cx - rad, cy - rad, rad * 2, rad * 2);
                            g.FillEllipse(b, cx - w, cy - rad + s * 0.10f, w * 2f, w * 2f);
                            g.DrawLine(p, cx, cy - s * 0.04f, cx, cy + rad - s * 0.12f);
                        }
                        break;
                    case GlyphKind.Sun:
                        {
                            float rad = s * 0.19f;
                            g.DrawEllipse(p, cx - rad, cy - rad, rad * 2, rad * 2);
                            float inner = s * 0.30f, outer = s * 0.42f;
                            for (int i = 0; i < 8; i++)
                            {
                                double a = Math.PI / 4 * i;
                                g.DrawLine(p,
                                    cx + (float)(Math.Cos(a) * inner), cy + (float)(Math.Sin(a) * inner),
                                    cx + (float)(Math.Cos(a) * outer), cy + (float)(Math.Sin(a) * outer));
                            }
                        }
                        break;
                    case GlyphKind.Moon:
                        {
                            float rad = s * 0.34f;
                            using (GraphicsPath full = new GraphicsPath())
                            using (GraphicsPath cut = new GraphicsPath())
                            using (Region reg = new Region())
                            {
                                full.AddEllipse(cx - rad, cy - rad, rad * 2, rad * 2);
                                cut.AddEllipse(cx - rad + s * 0.22f, cy - rad - s * 0.10f, rad * 2, rad * 2);
                                reg.MakeEmpty();
                                reg.Union(full);
                                reg.Exclude(cut);
                                g.FillRegion(b, reg);
                            }
                        }
                        break;
                    case GlyphKind.Disc:
                        {
                            float rad = s * 0.34f;
                            g.DrawEllipse(p, cx - rad, cy - rad, rad * 2, rad * 2);
                            float dot = s * 0.075f;
                            g.FillEllipse(b, cx - dot, cy - dot, dot * 2, dot * 2);
                            g.DrawArc(p, cx - rad * 0.62f, cy - rad * 0.62f, rad * 1.24f, rad * 1.24f, 200f, 140f);
                        }
                        break;
                    case GlyphKind.Grid:
                        {
                            float q = s * 0.31f;
                            float gap = s * 0.11f;
                            float x0 = cx - q - gap / 2f;
                            float y0 = cy - q - gap / 2f;
                            float rad2 = Math.Max(1f, s * 0.05f);
                            DrawSquare(g, p, x0, y0, q, rad2);
                            DrawSquare(g, p, x0 + q + gap, y0, q, rad2);
                            DrawSquare(g, p, x0, y0 + q + gap, q, rad2);
                            DrawSquare(g, p, x0 + q + gap, y0 + q + gap, q, rad2);
                        }
                        break;
                    case GlyphKind.Play:
                        {
                            float rad = s * 0.34f;
                            g.DrawEllipse(p, cx - rad, cy - rad, rad * 2, rad * 2);
                            using (GraphicsPath t = new GraphicsPath())
                            {
                                t.AddPolygon(new PointF[]
                                {
                                    new PointF(cx - s * 0.06f, cy - s * 0.14f),
                                    new PointF(cx + s * 0.16f, cy),
                                    new PointF(cx - s * 0.06f, cy + s * 0.14f)
                                });
                                g.FillPath(b, t);
                            }
                        }
                        break;
                    case GlyphKind.Key:
                        {
                            float ring = s * 0.15f;
                            float rx = cx - s * 0.16f;
                            g.DrawEllipse(p, rx - ring, cy - ring, ring * 2, ring * 2);
                            g.DrawLine(p, rx + ring, cy, cx + s * 0.34f, cy);
                            g.DrawLine(p, cx + s * 0.18f, cy, cx + s * 0.18f, cy + s * 0.14f);
                            g.DrawLine(p, cx + s * 0.31f, cy, cx + s * 0.31f, cy + s * 0.17f);
                        }
                        break;
                    case GlyphKind.ArrowUp:
                        g.DrawLine(p, cx, cy + s * 0.30f, cx, cy - s * 0.28f);
                        g.DrawLine(p, cx - s * 0.19f, cy - s * 0.08f, cx, cy - s * 0.28f);
                        g.DrawLine(p, cx + s * 0.19f, cy - s * 0.08f, cx, cy - s * 0.28f);
                        break;
                    case GlyphKind.Target:
                        {
                            float rad = s * 0.34f;
                            g.DrawEllipse(p, cx - rad, cy - rad, rad * 2, rad * 2);
                            float dot = s * 0.10f;
                            g.FillEllipse(b, cx - dot, cy - dot, dot * 2, dot * 2);
                        }
                        break;
                    case GlyphKind.Warning:
                        {
                            using (GraphicsPath t = new GraphicsPath())
                            {
                                t.AddPolygon(new PointF[]
                                {
                                    new PointF(cx, cy - s * 0.34f),
                                    new PointF(cx + s * 0.36f, cy + s * 0.28f),
                                    new PointF(cx - s * 0.36f, cy + s * 0.28f)
                                });
                                g.DrawPath(p, t);
                            }
                            g.DrawLine(p, cx, cy - s * 0.10f, cx, cy + s * 0.06f);
                            g.FillEllipse(b, cx - w * 0.6f, cy + s * 0.15f, w * 1.2f, w * 1.2f);
                        }
                        break;
                    case GlyphKind.Refresh:
                        g.DrawArc(p, cx - s * 0.28f, cy - s * 0.28f, s * 0.56f, s * 0.56f, 40f, 280f);
                        g.DrawLine(p, cx + s * 0.19f, cy - s * 0.31f, cx + s * 0.28f, cy - s * 0.16f);
                        g.DrawLine(p, cx + s * 0.19f, cy - s * 0.31f, cx + s * 0.05f, cy - s * 0.25f);
                        break;
                    case GlyphKind.Download:
                        g.DrawLine(p, cx, cy - s * 0.28f, cx, cy + s * 0.12f);
                        g.DrawLine(p, cx - s * 0.17f, cy - s * 0.05f, cx, cy + s * 0.13f);
                        g.DrawLine(p, cx + s * 0.17f, cy - s * 0.05f, cx, cy + s * 0.13f);
                        g.DrawLine(p, cx - s * 0.26f, cy + s * 0.30f, cx + s * 0.26f, cy + s * 0.30f);
                        break;
                    case GlyphKind.Check:
                        g.DrawLine(p, cx - s * 0.24f, cy + s * 0.02f, cx - s * 0.05f, cy + s * 0.20f);
                        g.DrawLine(p, cx - s * 0.05f, cy + s * 0.20f, cx + s * 0.26f, cy - s * 0.20f);
                        break;
                    case GlyphKind.ChevronLeft:
                        g.DrawLine(p, cx + s * 0.09f, cy - s * 0.18f, cx - s * 0.10f, cy);
                        g.DrawLine(p, cx - s * 0.10f, cy, cx + s * 0.09f, cy + s * 0.18f);
                        break;
                    case GlyphKind.ChevronRight:
                        g.DrawLine(p, cx - s * 0.09f, cy - s * 0.18f, cx + s * 0.10f, cy);
                        g.DrawLine(p, cx + s * 0.10f, cy, cx - s * 0.09f, cy + s * 0.18f);
                        break;
                }
            }
        }

        private static void DrawSquare(Graphics g, Pen p, float x, float y, float q, float rad)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                float d = rad * 2;
                path.AddArc(x, y, d, d, 180, 90);
                path.AddArc(x + q - d, y, d, d, 270, 90);
                path.AddArc(x + q - d, y + q - d, d, d, 0, 90);
                path.AddArc(x, y + q - d, d, d, 90, 90);
                path.CloseFigure();
                g.DrawPath(p, path);
            }
        }
    }

    internal interface IAnimatable
    {
        void StepAnim();
    }

    internal static class AnimBus
    {
        private static readonly List<IAnimatable> items = new List<IAnimatable>();
        private static Timer timer;

        public static void Add(IAnimatable item)
        {
            if (item == null || items.Contains(item)) return;
            items.Add(item);
            if (timer == null)
            {
                timer = new Timer();
                timer.Interval = 16;
                timer.Tick += delegate
                {
                    for (int i = items.Count - 1; i >= 0; i--)
                    {
                        Control c = items[i] as Control;
                        if (c != null && c.IsDisposed)
                        {
                            items.RemoveAt(i);
                            continue;
                        }
                        items[i].StepAnim();
                    }
                };
                timer.Start();
            }
        }

        public static void Remove(IAnimatable item)
        {
            items.Remove(item);
        }
    }

    internal static class AnimUtil
    {
        public static bool Approach(ref float value, float target, float k)
        {
            if (Math.Abs(value - target) < 0.004f)
            {
                if (value != target) { value = target; return true; }
                return false;
            }
            value += (target - value) * k;
            return true;
        }
    }

    // WinUI 风格按钮：Accent / 标准 / 浅色（Subtle）
    internal class UButton : Control, IAnimatable
    {
        public bool Primary { get; set; }
        public bool Danger { get; set; }
        public bool Subtle { get; set; }
        public bool Hover { get; private set; }
        public bool HoverTarget;
        public float HoverProgress;
        public bool Pressed { get; private set; }

        public UButton() : this("")
        {
        }

        public UButton(string text)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Text = text;
            Font = Theme.H2;
            Cursor = Cursors.Hand;
            Size = new Size(Theme.S(120), Theme.S(34));
            AnimBus.Add(this);
        }

        public void StepAnim()
        {
            float target = HoverTarget && Enabled ? 1f : 0f;
            if (AnimUtil.Approach(ref HoverProgress, target, 0.22f)) Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            AnimBus.Remove(this);
            base.Dispose(disposing);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            Hover = true;
            HoverTarget = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            Hover = false;
            HoverTarget = false;
            Pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Enabled)
            {
                Pressed = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            Pressed = false;
            Invalidate();
            base.OnMouseUp(e);
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
            if (Pressed)
            {
                g.TranslateTransform(Width / 2f, Height / 2f);
                g.ScaleTransform(0.97f, 0.97f);
                g.TranslateTransform(-Width / 2f, -Height / 2f);
            }

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = Theme.S(4);
            float hp = HoverProgress;
            Color fill;
            Color border;
            Color fg;

            if (!Enabled)
            {
                fill = Theme.Ghost;
                border = Theme.ControlStroke;
                fg = Theme.Dim;
            }
            else if (Subtle)
            {
                fill = Theme.Lerp(Theme.Subtle, Theme.SubtleHover, hp);
                border = Color.Transparent;
                fg = Danger ? Theme.Danger : Theme.Text;
            }
            else if (Primary)
            {
                Color accent = Danger ? Theme.Danger : Theme.Accent;
                fill = Danger
                    ? Theme.Lerp(Theme.Danger, Color.Black, hp * 0.1f)
                    : Theme.Lerp(accent, Theme.AccentHover, hp);
                if (Pressed)
                    fill = Danger ? Theme.Lerp(Theme.Danger, Color.Black, 0.2f) : Theme.AccentPressed;
                border = Color.Transparent;
                fg = Danger ? Theme.TextOn(Theme.Danger) : Theme.OnAccent;
            }
            else
            {
                fill = Theme.Lerp(Theme.ControlFill, Theme.CardHover, hp);
                if (Pressed) fill = Theme.Lerp(fill, Theme.CardPressed, 0.7f);
                border = Theme.Lerp(Theme.ControlStroke, Theme.Accent, hp * 0.45f);
                fg = Danger ? Theme.Danger : Theme.Text;
            }

            Theme.FillRound(g, r, radius, fill);
            if (border.A != 0) Theme.StrokeRound(g, r, radius, border, 1f);

            TextRenderer.DrawText(g, Text, Font, r, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    // 无边框窗口的最小化 / 关闭按钮
    internal class WindowButton : Control, IAnimatable
    {
        public bool IsClose;
        public GlyphKind Kind = GlyphKind.Minimize;
        public bool Hover;
        public bool HoverTarget;
        public float HoverProgress;

        public WindowButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(Theme.S(34), Theme.S(30));
            Cursor = Cursors.Hand;
            AnimBus.Add(this);
        }

        public void StepAnim()
        {
            float target = HoverTarget && Enabled ? 1f : 0f;
            if (AnimUtil.Approach(ref HoverProgress, target, 0.28f)) Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            AnimBus.Remove(this);
            base.Dispose(disposing);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            Hover = true;
            HoverTarget = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            Hover = false;
            HoverTarget = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            bool close = IsClose || Kind == GlyphKind.Close;
            float hp = HoverProgress;
            if (hp > 0.01f)
            {
                Color bg = close
                    ? Color.FromArgb((int)(196 * hp), 43, 28)
                    : Color.FromArgb((int)(20 * hp), Theme.Text);
                Theme.FillRound(g, new Rectangle(0, 0, Width - 1, Height - 1), 0, bg);
            }

            Color fg = close && hp > 0.55f ? Color.White : Theme.Muted;
            Rectangle icon = new Rectangle(0, 0, Width, Height);
            Glyphs.Draw(g, close ? GlyphKind.Close : Kind, icon, fg, Theme.SM(1.4f));
        }
    }

    // 左侧导航项
    internal class NavItem : Control, IAnimatable
    {
        public GlyphKind Glyph = GlyphKind.Grid;
        public bool Selected;
        public bool Compact;
        public bool HoverTarget;
        public float HoverProgress;

        public NavItem()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Theme.BodyStrong;
            Cursor = Cursors.Hand;
            Size = new Size(Theme.S(200), Theme.S(40));
            AnimBus.Add(this);
        }

        public void StepAnim()
        {
            float target = HoverTarget ? 1f : 0f;
            if (AnimUtil.Approach(ref HoverProgress, target, 0.22f)) Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            AnimBus.Remove(this);
            base.Dispose(disposing);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            HoverTarget = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            HoverTarget = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            float hp = HoverProgress;
            if (Selected)
                Theme.FillRound(g, r, Theme.S(4), Theme.AccentSoft);
            else if (hp > 0.01f)
                Theme.FillRound(g, r, Theme.S(4), Theme.Lerp(Theme.Subtle, Theme.SubtleHover, hp));

            int iconSize = Theme.S(18);
            int iconX = Compact ? (Width - iconSize) / 2 : Theme.S(12);
            Rectangle iconRect = new Rectangle(iconX, (Height - iconSize) / 2, iconSize, iconSize);
            Color iconColor = Selected ? Theme.Accent : (hp > 0.5f ? Theme.Text : Theme.Muted);
            Glyphs.Draw(g, Glyph, iconRect, iconColor, Theme.SM(1.4f));

            if (!Compact)
            {
                Rectangle textRect = new Rectangle(Theme.S(40), 0, Width - Theme.S(46), Height);
                Color fg = Selected ? Theme.Text : Theme.Lerp(Theme.Muted, Theme.Text, hp);
                TextRenderer.DrawText(g, Text, Font, textRect, fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }
    }

    // 深色模式开关
    internal class ThemeSwitch : Control, IAnimatable
    {
        public bool On;
        public bool Compact;
        public bool HoverTarget;
        public float HoverProgress;
        public float Knob;

        public ThemeSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(Theme.S(200), Theme.S(32));
            AnimBus.Add(this);
        }

        public void StepAnim()
        {
            bool dirty = false;
            if (AnimUtil.Approach(ref HoverProgress, HoverTarget ? 1f : 0f, 0.22f)) dirty = true;
            if (AnimUtil.Approach(ref Knob, On ? 1f : 0f, 0.26f)) dirty = true;
            if (dirty) Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            AnimBus.Remove(this);
            base.Dispose(disposing);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            HoverTarget = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            HoverTarget = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            float hp = HoverProgress;

            if (Compact)
            {
                if (hp > 0.01f)
                    Theme.FillRound(g, new Rectangle(0, Theme.S(2), Width - 1, Height - Theme.S(5)),
                        Theme.S(4), Theme.Lerp(Theme.Subtle, Theme.SubtleHover, hp));
                int iconSize = Theme.S(18);
                Rectangle iconRect = new Rectangle((Width - iconSize) / 2, (Height - iconSize) / 2, iconSize, iconSize);
                Glyphs.Draw(g, On ? GlyphKind.Moon : GlyphKind.Sun, iconRect,
                    On ? Theme.Accent : Theme.Muted, Theme.SM(1.4f));
                return;
            }

            if (hp > 0.01f)
                Theme.FillRound(g, new Rectangle(0, 0, Width - 1, Height - 1), Theme.S(4),
                    Theme.Lerp(Theme.Subtle, Theme.SubtleHover, hp * 0.7f));

            int iconSz = Theme.S(16);
            Rectangle icon = new Rectangle(Theme.S(10), (Height - iconSz) / 2, iconSz, iconSz);
            Glyphs.Draw(g, On ? GlyphKind.Moon : GlyphKind.Sun, icon,
                On ? Theme.Accent : Theme.Muted, Theme.SM(1.3f));
            TextRenderer.DrawText(g, "深色模式", Theme.Small,
                new Rectangle(Theme.S(34), 0, Width - Theme.S(34) - Theme.S(62), Height),
                Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            int tw = Theme.S(40), th = Theme.S(20);
            int tx = Width - Theme.S(10) - tw;
            int ty = (Height - th) / 2;
            Rectangle track = new Rectangle(tx, ty, tw - 1, th - 1);
            if (On)
            {
                Theme.FillRound(g, track, Theme.S(10), Theme.Accent);
            }
            else
            {
                Theme.FillRound(g, track, Theme.S(10), Theme.ControlFill);
                Theme.StrokeRound(g, track, Theme.S(10), Theme.ControlStroke, 1f);
            }
            int knob = Theme.S(12);
            int kx = tx + Theme.S(4) + (int)((tw - Theme.S(20)) * Knob);
            int ky = ty + (th - knob) / 2;
            Theme.FillRound(g, new Rectangle(kx, ky, knob, knob), knob / 2,
                On ? Color.White : Theme.Dim);
        }
    }

    // 左侧导航容器：绘制应用信息、分隔线与滑动指示条
    internal class NavPanel : Panel
    {
        public bool Compact;
        public float IndicatorY;
        public Bitmap AppIcon;
        public bool ShowIndicator = true;

        public NavPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.BgTop;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Theme.BgTop);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);

            using (Pen p = new Pen(Theme.Divider, 1f))
            {
                g.DrawLine(p, Width - 1, 0, Width - 1, Height);
            }

            int iconSize = Theme.S(28);
            int iconX = Compact ? (Width - iconSize) / 2 : Theme.S(14);
            if (AppIcon != null)
            {
                g.DrawImage(AppIcon, new Rectangle(iconX, Theme.S(16), iconSize, iconSize));
            }
            if (!Compact)
            {
                TextRenderer.DrawText(g, "无损跨版本升级", Theme.BodyStrong,
                    new Point(Theme.S(50), Theme.S(12)), Theme.Text, TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, "Windows  ·  " + Data.Version, Theme.Caption,
                    new Point(Theme.S(51), Theme.S(33)), Theme.Dim, TextFormatFlags.NoPrefix);
            }

            if (ShowIndicator)
            {
                Theme.FillRound(g, new Rectangle(Theme.S(2), (int)IndicatorY, Theme.S(3), Theme.S(16)),
                    Theme.S(2), Theme.Accent);
            }

            if (!Compact)
            {
                TextRenderer.DrawText(g, "ZHCOOL · 版本切换", Theme.Caption,
                    new Rectangle(Theme.S(14), Height - Theme.S(26), Width - Theme.S(22), Theme.S(16)),
                    Theme.Dim, TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            }
        }
    }

    // 步骤页容器：绘制页面标题与副标题
    internal class PagePanel : Panel
    {
        public string PageTitle = "";
        public string PageSubtitle = "";

        public PagePanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.BgTop;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Theme.BgTop);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            TextRenderer.DrawText(g, PageTitle, Theme.H1,
                new Rectangle(0, 0, Width, Theme.S(36)), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, PageSubtitle, Theme.Small,
                new Rectangle(0, Theme.S(40), Width, Theme.S(22)), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }
    }

    // 卡片容器：自绘圆角卡片 + 自定义滚动（滚轮 / 细滚动条）
    internal class ScrollCard : Panel
    {
        public int Pad = Theme.S(8);
        public string EmptyText = "";
        public int ContentHeight;

        private int offset;
        private readonly List<Control> rows = new List<Control>();
        private readonly Dictionary<Control, int> baseTops = new Dictionary<Control, int>();

        public ScrollCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.BgTop;
            MouseWheel += delegate(object sender, MouseEventArgs e) { ScrollBy(e.Delta); };
        }

        private int MaxOffset
        {
            get
            {
                int m = ContentHeight + Pad * 2 - ClientSize.Height;
                return m > 0 ? m : 0;
            }
        }

        public void ClearRows()
        {
            Control[] old = new Control[Controls.Count];
            Controls.CopyTo(old, 0);
            Controls.Clear();
            foreach (Control c in old) c.Dispose();
            rows.Clear();
            baseTops.Clear();
            ContentHeight = 0;
            offset = 0;
            Invalidate();
        }

        public void AddRow(Control c, int top)
        {
            rows.Add(c);
            baseTops[c] = top;
            Controls.Add(c);
            HookWheel(c);
            Apply();
        }

        public void HookWheel(Control c)
        {
            c.MouseWheel += delegate(object sender, MouseEventArgs e) { ScrollBy(e.Delta); };
        }

        public void Finish(int contentHeight)
        {
            ContentHeight = contentHeight;
            ClampOffset();
            Apply();
            Invalidate();
        }

        public void ScrollBy(int delta)
        {
            offset -= delta;
            ClampOffset();
            Apply();
            Invalidate();
        }

        private void ClampOffset()
        {
            if (offset > MaxOffset) offset = MaxOffset;
            if (offset < 0) offset = 0;
        }

        public void Apply()
        {
            int rowW = ClientSize.Width - Pad * 2 - Theme.S(4);
            if (rowW < Theme.S(160)) rowW = Theme.S(160);
            foreach (Control c in rows)
            {
                if (c.IsDisposed) continue;
                int top;
                if (!baseTops.TryGetValue(c, out top)) continue;
                c.Left = Pad;
                c.Width = rowW;
                c.Top = Pad - offset + top;
            }
        }

        public void EnsureVisible(Control c)
        {
            int top;
            if (!baseTops.TryGetValue(c, out top)) return;
            int viewH = ClientSize.Height - Pad * 2;
            if (top < offset) offset = top;
            else if (top + c.Height > offset + viewH) offset = top + c.Height - viewH;
            ClampOffset();
            Apply();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ClampOffset();
            Apply();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Theme.FillRound(g, r, Theme.S(8), Theme.Card);
            Theme.StrokeRound(g, r, Theme.S(8), Theme.CardStroke, 1f);

            if (rows.Count == 0 && EmptyText.Length > 0)
            {
                int size = Theme.S(26);
                int iconY = Height / 2 - Theme.S(58);
                if (iconY < Pad) iconY = Pad;
                Glyphs.Draw(g, GlyphKind.Disc,
                    new Rectangle((Width - size) / 2, iconY, size, size), Theme.Dim, Theme.SM(1.4f));
                TextRenderer.DrawText(g, EmptyText, Theme.Small,
                    new Rectangle(Pad + Theme.S(24), iconY + Theme.S(40),
                        Width - Pad * 2 - Theme.S(48), Theme.S(60)),
                    Theme.Dim, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top |
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            }

            if (MaxOffset > 0)
            {
                int trackH = Height - Pad * 2;
                int thumbH = (int)((float)trackH * (Height - Pad * 2) / (ContentHeight + Pad * 2));
                if (thumbH < Theme.S(24)) thumbH = Theme.S(24);
                int thumbY = Pad + (int)((trackH - thumbH) * (float)offset / MaxOffset);
                Color thumb = Color.FromArgb(110, Theme.Dim);
                Theme.FillRound(g, new Rectangle(Width - Theme.S(6), thumbY, Theme.S(3), thumbH),
                    Theme.S(2), thumb);
            }
        }
    }

    // 列表行（介质 / 版本）
    internal class AnimatedCard : Control, IAnimatable
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
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Height = Theme.S(56);
            Font = Theme.Body;
            AnimBus.Add(this);
        }

        public void StepAnim()
        {
            if (AnimUtil.Approach(ref HoverProgress, HoverTarget ? 1f : 0f, 0.22f)) Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            AnimBus.Remove(this);
            base.Dispose(disposing);
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
            Rectangle r = new Rectangle(0, 1, Width - 1, Height - 3);
            int radius = Theme.S(4);

            if (IsHeader)
            {
                TextRenderer.DrawText(g, Text, Theme.Caption,
                    new Rectangle(Theme.S(12), 0, Width - Theme.S(24), Height),
                    Theme.Dim, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                return;
            }

            float hp = HoverProgress;
            if (Selected)
                Theme.FillRound(g, r, radius, Theme.AccentSoft);
            else if (hp > 0.01f)
                Theme.FillRound(g, r, radius, Theme.Lerp(Theme.Subtle, Theme.SubtleHover, hp));

            if (Selected)
            {
                Rectangle bar = new Rectangle(r.X + Theme.S(2), (Height - Theme.S(22)) / 2, Theme.S(3), Theme.S(22));
                Theme.FillRound(g, bar, Theme.S(2), AccentColor);
            }

            int left = Theme.S(16);
            bool twoLine = SubText.Length > 0;
            if (twoLine)
            {
                TextRenderer.DrawText(g, Text, Selected ? Theme.H2 : Theme.Body,
                    new Rectangle(left, Theme.S(7), Width - Theme.S(190), Theme.S(21)), Theme.Text,
                    TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(g, SubText, Theme.MonoSmall,
                    new Rectangle(left, Theme.S(28), Width - Theme.S(150), Theme.S(17)),
                    Selected ? Theme.Muted : Theme.Dim,
                    TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            }
            else
            {
                TextRenderer.DrawText(g, Text, Selected ? Theme.H2 : Theme.Body,
                    new Rectangle(left, 0, Width - Theme.S(150), Height), Theme.Text,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            }

            if (RightText.Length > 0)
            {
                TextRenderer.DrawText(g, RightText, Theme.MonoSmall,
                    new Rectangle(left, 0, Width - left - Theme.S(14), Height),
                    Selected ? Theme.Accent : Theme.Dim,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            }
        }
    }

    // 执行方式磁贴
    internal class ModeCard : Control, IAnimatable
    {
        public string Description = "";
        public string Shortcut = "";
        public GlyphKind Glyph = GlyphKind.Play;
        public bool Selected;
        public float HoverProgress;
        public bool HoverTarget;
        public RunMode Mode;

        public ModeCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = Theme.Body;
            Size = new Size(Theme.S(240), Theme.S(84));
            AnimBus.Add(this);
        }

        public void StepAnim()
        {
            if (AnimUtil.Approach(ref HoverProgress, HoverTarget ? 1f : 0f, 0.22f)) Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            AnimBus.Remove(this);
            base.Dispose(disposing);
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
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = Theme.S(8);
            float hp = HoverProgress;

            Color fill = Selected ? Theme.AccentSoft : Theme.Lerp(Theme.Card, Theme.CardHover, hp);
            Theme.FillRound(g, r, radius, fill);
            Color stroke = Selected
                ? Color.FromArgb(150, Theme.Accent)
                : Theme.Lerp(Theme.CardStroke, Theme.Accent, hp * 0.45f);
            Theme.StrokeRound(g, r, radius, stroke, Selected ? Theme.SM(1.4f) : 1f);

            int iconSize = Theme.S(20);
            Rectangle iconRect = new Rectangle(Theme.S(14), Theme.S(14), iconSize, iconSize);
            Glyphs.Draw(g, Glyph, iconRect, Selected ? Theme.Accent : Theme.Muted, Theme.SM(1.4f));

            Rectangle titleRect = new Rectangle(Theme.S(44), Theme.S(12), Width - Theme.S(96), Theme.S(22));
            TextRenderer.DrawText(g, Text, Theme.BodyStrong, titleRect, Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

            Rectangle descRect = new Rectangle(Theme.S(44), Theme.S(35), Width - Theme.S(58), Height - Theme.S(40));
            TextRenderer.DrawText(g, Description, Theme.Small, descRect, Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak |
                TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

            int chip = Theme.S(20);
            Rectangle chipRect = new Rectangle(Width - Theme.S(14) - chip, Theme.S(13), chip, chip);
            if (Selected)
            {
                Theme.FillRound(g, chipRect, Theme.S(4), Theme.Accent);
            }
            else
            {
                Theme.FillRound(g, chipRect, Theme.S(4), Theme.BadgeBg);
            }
            TextRenderer.DrawText(g, Shortcut, Theme.MonoSmall, chipRect,
                Selected ? Theme.OnAccent : Theme.Dim,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
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
                p2.StartCap = LineCap.Round;
                p2.EndCap = LineCap.Round;
                g.DrawArc(p2, r, Angle + 140f, 200f);
            }
        }
    }
}
