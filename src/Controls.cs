using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UpgradeTool
{
    internal class DarkPanel : Panel
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string subAppName, string subIdList);

        public DarkPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Theme.BgTop;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RefreshTheme();
        }

        public void RefreshTheme()
        {
            try
            {
                SetWindowTheme(Handle, Theme.Light ? "Explorer" : "DarkMode_Explorer", null);
            }
            catch
            {
            }
        }
    }

    internal class GradientPanel : Panel
    {
        public GradientPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.BgTop;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Theme.BgTop);
        }
    }

    internal class WindowButton : Control
    {
        public bool IsClose;
        public bool IsAbout;
        public bool Hover;

        public WindowButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(Theme.S(34), Theme.S(30));
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            Hover = true; Invalidate(); base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            Hover = false; Invalidate(); base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            if (Hover)
                Theme.FillRound(g, new Rectangle(0, 0, Width - 1, Height - 1), Theme.S(7),
                    IsClose ? Color.FromArgb(120, Theme.Danger) : Theme.CardHover);

            Color fg = Hover && IsClose ? Color.White : Theme.Muted;
            int cx = Width / 2, cy = Height / 2;
            using (Pen p = new Pen(fg, Theme.SM(1.6f)))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                if (IsClose)
                {
                    int d = Theme.S(5);
                    g.DrawLine(p, cx - d, cy - d, cx + d, cy + d);
                    g.DrawLine(p, cx + d, cy - d, cx - d, cy + d);
                }
                else if (IsAbout)
                {
                    int r2 = Theme.S(7);
                    g.DrawEllipse(p, cx - r2, cy - r2, r2 * 2, r2 * 2);
                    int dot = Theme.S(1);
                    using (SolidBrush b = new SolidBrush(fg))
                        g.FillEllipse(b, cx - dot, cy - Theme.S(4), dot * 2, dot * 2);
                    g.DrawLine(p, cx, cy - Theme.S(1), cx, cy + Theme.S(4));
                }
                else
                {
                    int d = Theme.S(6);
                    g.DrawLine(p, cx - d, cy, cx + d, cy);
                }
            }
        }
    }

    internal class ThemeButton : Control
    {
        public bool Hover;

        public ThemeButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(Theme.S(34), Theme.S(30));
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            Hover = true; Invalidate(); base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            Hover = false; Invalidate(); base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            if (Hover)
                Theme.FillRound(g, new Rectangle(0, 0, Width - 1, Height - 1), Theme.S(7), Theme.CardHover);

            int cx = Width / 2, cy = Height / 2;
            Color fg = Hover ? Theme.Text : Theme.Muted;
            if (Theme.Light)
            {
                int r2 = Theme.S(5);
                using (Pen p = new Pen(fg, Theme.SM(1.5f)))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    g.DrawEllipse(p, cx - r2, cy - r2, r2 * 2, r2 * 2);
                    int inner = Theme.S(8), outer = Theme.S(11);
                    for (int i = 0; i < 8; i++)
                    {
                        double a = Math.PI / 4 * i;
                        g.DrawLine(p,
                            cx + (int)(Math.Cos(a) * inner), cy + (int)(Math.Sin(a) * inner),
                            cx + (int)(Math.Cos(a) * outer), cy + (int)(Math.Sin(a) * outer));
                    }
                }
            }
            else
            {
                int r2 = Theme.S(7);
                using (GraphicsPath full = new GraphicsPath())
                using (GraphicsPath cut = new GraphicsPath())
                using (Region reg = new Region())
                {
                    full.AddEllipse(cx - r2, cy - r2, r2 * 2, r2 * 2);
                    cut.AddEllipse(cx - r2 + Theme.S(4), cy - r2 - Theme.S(2), r2 * 2, r2 * 2);
                    reg.MakeEmpty();
                    reg.Union(full);
                    reg.Exclude(cut);
                    using (SolidBrush b = new SolidBrush(fg))
                        g.FillRegion(b, reg);
                }
            }
        }
    }

    internal class ModeCard : Control
    {
        public string Description = "";
        public string Shortcut = "";
        public bool Selected;
        public float HoverProgress;
        public bool HoverTarget;
        public RunMode Mode;

        public ModeCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Font = Theme.Body;
            Size = new Size(Theme.S(438), Theme.S(92));
            ForeColor = Theme.Text;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            HoverTarget = true; base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            HoverTarget = false; base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            Rectangle r = new Rectangle(0, Theme.S(1), Width - 1, Height - Theme.S(3));
            int radius = Theme.S(10);

            Color fill = Theme.Lerp(Theme.Card, Theme.CardHover, HoverProgress);
            if (Selected) fill = Theme.Lerp(Theme.Card, Theme.Accent, 0.14f);
            Theme.FillRound(g, r, radius, fill);
            Theme.StrokeRound(g, r, radius,
                Selected ? Theme.Lerp(Theme.Accent, Theme.Light ? Color.Black : Color.White, 0.15f)
                         : Theme.Lerp(Theme.Line, Theme.Accent, HoverProgress * 0.5f),
                Selected ? Theme.SM(1.6f) : 1f);

            Rectangle badge = new Rectangle(r.X + Theme.S(8), r.Y + Theme.S(9), Theme.S(20), Theme.S(20));
            Theme.FillRound(g, badge, Theme.S(5), Selected ? Theme.Accent : Theme.BadgeBg);
            TextRenderer.DrawText(g, Shortcut, Theme.MonoSmall, badge,
                Selected ? Color.White : Theme.Dim,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(g, Text, Selected ? Theme.H2 : Theme.Body,
                new Rectangle(r.X + Theme.S(36), r.Y + Theme.S(9), r.Width - Theme.S(94), Theme.S(22)),
                Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, Description, Theme.Small,
                new Rectangle(r.X + Theme.S(36), r.Y + Theme.S(34), r.Width - Theme.S(56), Height - Theme.S(44)),
                Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }
    }
}
