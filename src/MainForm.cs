using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UpgradeTool
{
    internal partial class MainForm : Form
    {
        private const int WinW = 1000;
        private const int WinH = 680;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private int step;
        private GradientPanel stepSource, stepEdition, stepMode;
        private Panel sourceList, editionList;
        private Label lblSourceHint, lblEditionHint;
        private UButton btnRescan, btnFolder, btnIso;
        private UButton btnBack, btnNext, btnStart;
        private Label lblFooter;
        private Panel logCard;
        private Label lblSourceTitle, lblSourceDesc, lblEditionTitle, lblModeTitle;
        private Panel targetCard;
        private readonly List<string> logLines = new List<string>();
        private WindowButton btnClose, btnMin, btnAbout;
        private ThemeButton btnTheme;

        private readonly List<MediaSource> sources = new List<MediaSource>();
        private readonly List<AnimatedCard> allCards = new List<AnimatedCard>();
        private readonly List<ModeCard> modeCards = new List<ModeCard>();
        private MediaSource currentSource;
        private Edition currentEdition;
        private RunMode currentMode = RunMode.Pkey;
        private Timer anim;
        private readonly Spinner spinner = new Spinner();
        private Panel slidePanel;
        private int slideTicks;
        private float fadeTicks;
        private bool busy;

        public MainForm()
        {
            SuspendLayout();
            Text = Data.AppName + " " + Data.Version;
            try
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
            }
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Theme.S(WinW), Theme.S(WinH));
            MinimumSize = new Size(Theme.S(990), Theme.S(676));
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            if (Width > work.Width) ClientSize = new Size(work.Width - Theme.S(24), ClientSize.Height);
            if (Height > work.Height) ClientSize = new Size(ClientSize.Width, work.Height - Theme.S(24));
            if (MinimumSize.Width > ClientSize.Width) MinimumSize = new Size(ClientSize.Width, MinimumSize.Height);
            if (MinimumSize.Height > ClientSize.Height) MinimumSize = new Size(MinimumSize.Width, ClientSize.Height);
            BackColor = Theme.BgTop;
            Font = Theme.Body;
            DoubleBuffered = true;
            KeyPreview = true;
            Opacity = 0;

            BuildHeader();
            BuildFooter();
            BuildSteps();
            Controls.Add(stepSource);
            Controls.Add(stepEdition);
            Controls.Add(stepMode);
            Controls.Add(lblFooter);
            Controls.Add(btnBack);
            Controls.Add(btnNext);
            Controls.Add(btnStart);

            ResumeLayout(false);

            anim = new Timer();
            anim.Interval = 16;
            anim.Tick += AnimTick;
            anim.Start();

            Load += delegate
            {
                UpdateRegion();
                LayoutAll();
                PopulateEditions();
                GoStep(0);
                ScanSources(true);
            };
            MouseDown += FormMouseDown;
            MouseMove += FormMouseMove;
            MouseUp += FormMouseUp;
            MouseLeave += FormMouseLeave;
        }

        private void UpdateRegion()
        {
            using (GraphicsPath p = Theme.RoundRect(new Rectangle(0, 0, Width, Height), Theme.S(14)))
            {
                Region = new Region(p);
            }
        }

        private void ApplyTheme()
        {
            BackColor = Theme.BgTop;
            stepSource.BackColor = Theme.BgTop;
            stepEdition.BackColor = Theme.BgTop;
            stepMode.BackColor = Theme.BgTop;
            sourceList.BackColor = Theme.BgTop;
            editionList.BackColor = Theme.BgTop;
            logCard.BackColor = Theme.BgTop;
            targetCard.BackColor = Theme.BgTop;
            lblSourceTitle.ForeColor = Theme.Text;
            lblEditionTitle.ForeColor = Theme.Text;
            lblModeTitle.ForeColor = Theme.Text;
            lblSourceDesc.ForeColor = Theme.Muted;
            lblEditionHint.ForeColor = Theme.Muted;
            lblSourceHint.ForeColor = Theme.Dim;

            DarkPanel sp = sourceList as DarkPanel;
            if (sp != null) sp.RefreshTheme();
            DarkPanel ep = editionList as DarkPanel;
            if (ep != null) ep.RefreshTheme();

            RefreshSourceSelection();
            PopulateEditions();
            HighlightEdition();
            UpdateFooter();
            Invalidate(true);
        }

        private void ToggleTheme()
        {
            Theme.Init(!Theme.Light, Theme.Scale);
            Program.SaveTheme(Theme.Light);
            ApplyTheme();
        }

        private void ShowAbout()
        {
            using (AboutDialog dlg = new AboutDialog())
            {
                dlg.ShowDialog(this);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (stepSource == null) return;
            LayoutAll();
            UpdateRegion();
            Invalidate();
        }

        private void LayoutAll()
        {
            int W = ClientSize.Width;
            int H = ClientSize.Height;
            int contentW = W - Theme.S(56);
            int contentTop = Theme.S(126);
            int footerY = H - Theme.S(78);
            int contentH = footerY - Theme.S(14) - contentTop;
            if (contentH < Theme.S(300)) contentH = Theme.S(300);

            stepSource.SetBounds(Theme.S(28), contentTop, contentW, contentH);
            stepEdition.SetBounds(Theme.S(28), contentTop, contentW, contentH);
            stepMode.SetBounds(Theme.S(28), contentTop, contentW, contentH);

            lblSourceTitle.Width = contentW;
            lblSourceDesc.Width = contentW;
            sourceList.SetBounds(0, Theme.S(72), contentW, contentH - Theme.S(72) - Theme.S(68));
            int by = contentH - Theme.S(56);
            btnRescan.SetBounds(0, by, Theme.S(132), Theme.S(38));
            btnFolder.SetBounds(Theme.S(144), by, Theme.S(132), Theme.S(38));
            btnIso.SetBounds(Theme.S(288), by, Theme.S(132), Theme.S(38));
            lblSourceHint.SetBounds(Theme.S(436), by + Theme.S(5), Math.Max(Theme.S(120), contentW - Theme.S(436)), Theme.S(26));

            lblEditionTitle.Width = contentW;
            lblEditionHint.Width = contentW;
            editionList.SetBounds(0, Theme.S(68), contentW, contentH - Theme.S(68));

            lblModeTitle.Width = contentW;
            targetCard.SetBounds(0, Theme.S(44), contentW, Theme.S(80));
            int gap = Theme.S(24);
            int cardW = (contentW - gap) / 2;
            for (int i = 0; i < modeCards.Count; i++)
            {
                modeCards[i].SetBounds((i % 2) * (cardW + gap), Theme.S(142) + (i / 2) * Theme.S(100),
                    cardW, Theme.S(92));
            }
            logCard.SetBounds(0, Theme.S(352), contentW, Math.Max(Theme.S(84), contentH - Theme.S(356)));

            lblFooter.SetBounds(Theme.S(28), H - Theme.S(64),
                Math.Max(Theme.S(200), W - Theme.S(56) - Theme.S(420)), Theme.S(24));
            int bw = Theme.S(154);
            int bx = W - Theme.S(28) - bw;
            btnStart.SetBounds(bx, footerY, bw, Theme.S(40));
            btnNext.SetBounds(bx, footerY, bw, Theme.S(40));
            btnBack.SetBounds(bx - Theme.S(126), footerY, Theme.S(112), Theme.S(40));
            btnAbout.Location = new Point(W - Theme.S(156), Theme.S(14));
            btnTheme.Location = new Point(W - Theme.S(120), Theme.S(14));
            btnMin.Location = new Point(W - Theme.S(84), Theme.S(14));
            btnClose.Location = new Point(W - Theme.S(48), Theme.S(14));
        }

        private int resizeEdge;
        private Point resizeStart;
        private Rectangle resizeRect;

        private int HitEdge(Point p)
        {
            int b = Theme.S(6);
            int edge = 0;
            if (p.X <= b) edge |= 1;
            if (p.X >= ClientSize.Width - b) edge |= 2;
            if (p.Y <= b) edge |= 4;
            if (p.Y >= ClientSize.Height - b) edge |= 8;
            return edge;
        }

        private static Cursor CursorForEdge(int edge)
        {
            if ((edge & 1) != 0 && (edge & 4) != 0) return Cursors.SizeNWSE;
            if ((edge & 2) != 0 && (edge & 4) != 0) return Cursors.SizeNESW;
            if ((edge & 1) != 0 && (edge & 8) != 0) return Cursors.SizeNESW;
            if ((edge & 2) != 0 && (edge & 8) != 0) return Cursors.SizeNWSE;
            if ((edge & 1) != 0 || (edge & 2) != 0) return Cursors.SizeWE;
            if ((edge & 4) != 0 || (edge & 8) != 0) return Cursors.SizeNS;
            return Cursors.Default;
        }


        private void BuildHeader()
        {
            btnClose = new WindowButton();
            btnClose.IsClose = true;
            btnClose.Location = new Point(Theme.S(WinW) - Theme.S(48), Theme.S(14));
            btnClose.Click += delegate { Close(); };
            Controls.Add(btnClose);

            btnMin = new WindowButton();
            btnMin.Location = new Point(Theme.S(WinW) - Theme.S(84), Theme.S(14));
            btnMin.Click += delegate { WindowState = FormWindowState.Minimized; };
            Controls.Add(btnMin);

            btnTheme = new ThemeButton();
            btnTheme.Location = new Point(Theme.S(WinW) - Theme.S(120), Theme.S(14));
            btnTheme.Click += delegate { ToggleTheme(); };
            Controls.Add(btnTheme);

            btnAbout = new WindowButton();
            btnAbout.IsAbout = true;
            btnAbout.Location = new Point(Theme.S(WinW) - Theme.S(156), Theme.S(14));
            btnAbout.Click += delegate { ShowAbout(); };
            Controls.Add(btnAbout);
        }

        private void BuildFooter()
        {
            lblFooter = new Label();
            lblFooter.AutoSize = false;
            lblFooter.Location = new Point(28, 608);
            lblFooter.Size = new Size(600, 22);
            lblFooter.Font = Theme.Small;
            lblFooter.ForeColor = Theme.Muted;
            lblFooter.BackColor = Color.Transparent;

            btnBack = new UButton("上一步");
            btnBack.Location = new Point(672, 600);
            btnBack.Size = new Size(112, 40);
            btnBack.Click += delegate { GoStep(step - 1); };

            btnNext = new UButton("下一步");
            btnNext.Primary = true;
            btnNext.Location = new Point(798, 600);
            btnNext.Size = new Size(154, 40);
            btnNext.Click += delegate { GoStep(step + 1); };

            btnStart = new UButton("开始执行");
            btnStart.Primary = true;
            btnStart.Location = new Point(798, 600);
            btnStart.Size = new Size(154, 40);
            btnStart.Click += delegate { StartRun(); };
        }

        private void BuildSteps()
        {
            stepSource = new GradientPanel();
            stepSource.Location = new Point(28, 126);
            stepSource.Size = new Size(924, 460);
            stepSource.BackColor = Theme.BgTop;

            lblSourceTitle = SectionTitle("选择安装介质", stepSource);
            lblSourceDesc = new Label();
            lblSourceDesc.AutoSize = false;
            lblSourceDesc.Location = new Point(0, Theme.S(38));
            lblSourceDesc.Size = new Size(Theme.S(924), Theme.S(20));
            lblSourceDesc.Font = Theme.Small;
            lblSourceDesc.ForeColor = Theme.Muted;
            lblSourceDesc.BackColor = Color.Transparent;
            lblSourceDesc.Text = "需要包含 setup.exe 和 sources 文件夹的目录，或者直接选择 ISO 镜像自动挂载。";
            stepSource.Controls.Add(lblSourceDesc);

            sourceList = new DarkPanel();
            sourceList.Location = new Point(0, 72);
            sourceList.Size = new Size(924, 296);
            sourceList.AutoScroll = true;
            sourceList.BackColor = Theme.BgTop;
            sourceList.Resize += delegate { LayoutSourceRows(); };
            stepSource.Controls.Add(sourceList);

            btnRescan = new UButton("重新扫描");
            btnRescan.Location = new Point(0, 384);
            btnRescan.Size = new Size(132, 38);
            btnRescan.Click += delegate { ScanSources(true); };
            stepSource.Controls.Add(btnRescan);

            btnFolder = new UButton("选择文件夹");
            btnFolder.Location = new Point(144, 384);
            btnFolder.Size = new Size(132, 38);
            btnFolder.Click += delegate { PickFolder(); };
            stepSource.Controls.Add(btnFolder);

            btnIso = new UButton("选择 ISO");
            btnIso.Location = new Point(288, 384);
            btnIso.Size = new Size(132, 38);
            btnIso.Click += delegate { PickIso(); };
            stepSource.Controls.Add(btnIso);

            lblSourceHint = new Label();
            lblSourceHint.AutoSize = false;
            lblSourceHint.Location = new Point(436, 390);
            lblSourceHint.Size = new Size(488, 26);
            lblSourceHint.Font = Theme.Small;
            lblSourceHint.ForeColor = Theme.Dim;
            lblSourceHint.BackColor = Color.Transparent;
            lblSourceHint.Text = "";
            stepSource.Controls.Add(lblSourceHint);

            stepEdition = new GradientPanel();
            stepEdition.Location = new Point(28, 126);
            stepEdition.Size = new Size(924, 460);
            stepEdition.BackColor = Theme.BgTop;
            lblEditionTitle = SectionTitle("选择目标版本", stepEdition);

            lblEditionHint = new Label();
            lblEditionHint.AutoSize = false;
            lblEditionHint.Location = new Point(0, Theme.S(38));
            lblEditionHint.Size = new Size(Theme.S(924), Theme.S(20));
            lblEditionHint.Font = Theme.Small;
            lblEditionHint.ForeColor = Theme.Muted;
            lblEditionHint.BackColor = Color.Transparent;
            lblEditionHint.Text = "常规版本可以直接升级；LTSC 和 Server 需要对应的独立安装介质。";
            stepEdition.Controls.Add(lblEditionHint);

            editionList = new DarkPanel();
            editionList.Location = new Point(0, 68);
            editionList.Size = new Size(924, 364);
            editionList.AutoScroll = true;
            editionList.BackColor = Theme.BgTop;
            editionList.Resize += delegate { LayoutEditionRows(); };
            stepEdition.Controls.Add(editionList);

            stepMode = new GradientPanel();
            stepMode.Location = new Point(28, 126);
            stepMode.Size = new Size(924, 460);
            stepMode.BackColor = Theme.BgTop;
            lblModeTitle = SectionTitle("选择执行方式", stepMode);

            targetCard = new Panel();
            targetCard.Location = new Point(0, 44);
            targetCard.Size = new Size(924, 80);
            targetCard.BackColor = Theme.BgTop;
            targetCard.Paint += PaintTargetCard;
            stepMode.Controls.Add(targetCard);

            string[] titles = { "仅更换密钥", "普通升级", "指定版本升级", "强制升级" };
            string[] descs =
            {
                "用 slmgr 安装所选密钥，只切换版本，不做就地升级。",
                "不指定版本，由安装程序自行判断，等同普通就地升级。",
                "就地升级到所选版本，自动传入对应的预安装密钥。",
                "改写注册表伪装版本，可跨过官方升级路径限制，需二次确认。"
            };
            string[] keys = { "K", "S", "U", "F" };
            RunMode[] modes = { RunMode.KeyOnly, RunMode.Normal, RunMode.Pkey, RunMode.Force };

            for (int i = 0; i < 4; i++)
            {
                ModeCard c = new ModeCard();
                c.Text = titles[i];
                c.Description = descs[i];
                c.Shortcut = keys[i];
                c.Mode = modes[i];
                c.Location = new Point((i % 2) * 468, 142 + (i / 2) * 100);
                c.Click += ModeCardClick;
                c.MouseEnter += delegate { };
                stepMode.Controls.Add(c);
                modeCards.Add(c);
            }

            logCard = new Panel();
            logCard.Location = new Point(0, 352);
            logCard.Size = new Size(924, 108);
            logCard.BackColor = Theme.BgTop;
            logCard.Paint += PaintLogCard;
            stepMode.Controls.Add(logCard);
        }

        private void PaintLogCard(object sender, PaintEventArgs e)
        {
            Panel p = (Panel)sender;
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            Rectangle r = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
            Theme.FillRound(g, r, Theme.S(10), Theme.Sunken);
            Theme.StrokeRound(g, r, Theme.S(10), Theme.Line, 1f);
            TextRenderer.DrawText(g, "运行日志", Theme.Small, new Point(Theme.S(17), Theme.S(9)), Theme.Dim,
                TextFormatFlags.NoPrefix);

            if (logLines.Count == 0)
            {
                TextRenderer.DrawText(g, "选择执行方式后点「开始执行」，过程信息会记录在这里。",
                    Theme.MonoSmall, new Point(Theme.S(17), Theme.S(34)), Theme.Dim, TextFormatFlags.NoPrefix);
                return;
            }

            int lineH = Theme.S(17);
            int maxLines = Math.Max(1, (r.Height - Theme.S(42)) / lineH);
            int shown = Math.Min(maxLines, logLines.Count);
            int startY = Theme.S(34);
            for (int i = 0; i < shown; i++)
            {
                string line = logLines[logLines.Count - shown + i];
                bool newest = i == shown - 1;
                TextRenderer.DrawText(g, line, Theme.MonoSmall,
                    new Rectangle(Theme.S(17), startY + i * lineH, r.Width - Theme.S(34), lineH),
                    newest ? Theme.Text : Theme.Muted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        private Label SectionTitle(string text, Control parent)
        {
            Label l = new Label();
            l.AutoSize = false;
            l.Location = new Point(0, 0);
            l.Size = new Size(Theme.S(924), Theme.S(30));
            l.Font = Theme.H1;
            l.ForeColor = Theme.Text;
            l.BackColor = Color.Transparent;
            l.Text = text;
            parent.Controls.Add(l);
            return l;
        }

        private void FormMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            int edge = HitEdge(e.Location);
            if (edge != 0)
            {
                resizeEdge = edge;
                resizeStart = Cursor.Position;
                resizeRect = Bounds;
                Capture = true;
                return;
            }
            if (e.Y < Theme.S(112))
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }

        private void FormMouseMove(object sender, MouseEventArgs e)
        {
            if (resizeEdge == 0)
            {
                Cursor = CursorForEdge(HitEdge(e.Location));
                return;
            }

            Point p = Cursor.Position;
            int dx = p.X - resizeStart.X;
            int dy = p.Y - resizeStart.Y;
            Rectangle b = resizeRect;
            if ((resizeEdge & 1) != 0) { b.X += dx; b.Width -= dx; }
            if ((resizeEdge & 2) != 0) b.Width += dx;
            if ((resizeEdge & 4) != 0) { b.Y += dy; b.Height -= dy; }
            if ((resizeEdge & 8) != 0) b.Height += dy;

            if (b.Width < MinimumSize.Width)
            {
                if ((resizeEdge & 1) != 0) b.X = resizeRect.Right - MinimumSize.Width;
                b.Width = MinimumSize.Width;
            }
            if (b.Height < MinimumSize.Height)
            {
                if ((resizeEdge & 4) != 0) b.Y = resizeRect.Bottom - MinimumSize.Height;
                b.Height = MinimumSize.Height;
            }
            Bounds = b;
        }

        private void FormMouseUp(object sender, MouseEventArgs e)
        {
            if (resizeEdge == 0) return;
            resizeEdge = 0;
            Capture = false;
            Cursor = Cursors.Default;
        }

        private void FormMouseLeave(object sender, EventArgs e)
        {
            if (resizeEdge == 0) Cursor = Cursors.Default;
        }

        private void GoStep(int target)
        {
            if (busy) return;
            if (target < 0 || target > 2) return;
            if (target == 1 && currentSource == null) return;
            if (target == 2 && currentEdition == null) return;

            step = target;
            stepSource.Visible = step == 0;
            stepEdition.Visible = step == 1;
            stepMode.Visible = step == 2;

            btnBack.Visible = step > 0;
            btnNext.Visible = step < 2;
            btnStart.Visible = step == 2;

            if (step == 1) HighlightEdition();
            if (step == 2)
            {
                RefreshModeCards();
                RefreshTargetCard();
            }
            UpdateFooter();
            StartSlide();
            Invalidate();
        }

        private void StartSlide()
        {
            slidePanel = step == 0 ? stepSource : (step == 1 ? stepEdition : stepMode);
            slideTicks = 1;
            slidePanel.Left = Theme.S(44);
        }

        private void AnimTick(object sender, EventArgs e)
        {
            if (fadeTicks < 1f)
            {
                fadeTicks += 0.08f;
                if (fadeTicks > 1f) fadeTicks = 1f;
                Opacity = fadeTicks;
            }

            if (slideTicks > 0 && slidePanel != null)
            {
                slideTicks++;
                float t = Math.Min(1f, slideTicks / 8f);
                float ease = 1f - (1f - t) * (1f - t);
                slidePanel.Left = Theme.S(28) + (int)(Theme.S(16) * (1f - ease));
                if (t >= 1f) slideTicks = 0;
            }

            if (spinner.Active)
            {
                spinner.Angle = (spinner.Angle + 10f) % 360f;
                Invalidate(new Rectangle(0, ClientSize.Height - Theme.S(90), ClientSize.Width, Theme.S(90)));
            }

            foreach (AnimatedCard c in allCards)
            {
                float old = c.HoverProgress;
                float delta = c.HoverTarget ? 0.14f : -0.14f;
                c.HoverProgress = Math.Max(0f, Math.Min(1f, c.HoverProgress + delta));
                if (Math.Abs(old - c.HoverProgress) > 0.001f) c.Invalidate();
            }
            foreach (ModeCard c in modeCards)
            {
                float old = c.HoverProgress;
                float delta = c.HoverTarget ? 0.18f : -0.18f;
                c.HoverProgress = Math.Max(0f, Math.Min(1f, c.HoverProgress + delta));
                if (Math.Abs(old - c.HoverProgress) > 0.001f) c.Invalidate();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            int W = ClientSize.Width;
            int gh = Theme.S(150);
            e.Graphics.Clear(Theme.BgTop);
            using (LinearGradientBrush b = new LinearGradientBrush(
                new Rectangle(0, 0, W, gh), Theme.HeaderTop, Theme.BgTop, 90f))
            {
                e.Graphics.FillRectangle(b, 0, 0, W, gh);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);

            TextRenderer.DrawText(g, Data.AppName, Theme.Title,
                new Point(Theme.S(28), Theme.S(18)), Theme.Text, TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, "Windows 10 / 11 / Server   ·   " + Data.Version + "   ·   2026",
                Theme.Small, new Point(Theme.S(29), Theme.S(52)), Theme.Muted, TextFormatFlags.NoPrefix);

            string[] names = { "安装源", "目标版本", "执行方式" };
            int W = ClientSize.Width;
            int stepBase = W - Theme.S(653);
            int[] xs = { stepBase, stepBase + Theme.S(172), stepBase + Theme.S(344) };
            int circle = Theme.S(24);
            int circleY = Theme.S(26);
            int lineY = Theme.S(38);
            for (int i = 0; i < 3; i++)
            {
                if (i < 2)
                {
                    using (Pen p = new Pen(i < step ? Theme.Accent : Theme.Line, Theme.SM(1.2f)))
                    {
                        g.DrawLine(p, xs[i] + Theme.S(26), lineY, xs[i + 1] - Theme.S(10), lineY);
                    }
                }
                bool active = i == step;
                bool done = i < step;
                Rectangle cr = new Rectangle(xs[i], circleY, circle, circle);
                if (active)
                {
                    using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillEllipse(b, cr);
                }
                else
                {
                    using (Pen p = new Pen(done ? Theme.Accent2 : Theme.Line, Theme.SM(1.4f))) g.DrawEllipse(p, cr);
                }
                if (active)
                {
                    using (Pen glow = new Pen(Color.FromArgb(70, Theme.Accent), Theme.SM(1.2f)))
                        g.DrawEllipse(glow, cr.X - Theme.S(4), cr.Y - Theme.S(4),
                            cr.Width + Theme.S(8), cr.Height + Theme.S(8));
                    using (Pen glow2 = new Pen(Color.FromArgb(30, Theme.Accent), 1f))
                        g.DrawEllipse(glow2, cr.X - Theme.S(7), cr.Y - Theme.S(7),
                            cr.Width + Theme.S(14), cr.Height + Theme.S(14));
                }
                string num = (i + 1).ToString();
                TextRenderer.DrawText(g, num, Theme.MonoSmall, cr,
                    active ? Color.White : (done ? Theme.Accent2 : Theme.Dim),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, names[i], Theme.Small,
                    new Point(xs[i] + Theme.S(32), Theme.S(30)),
                    active || done ? Theme.Text : Theme.Dim, TextFormatFlags.NoPrefix);
            }

            using (Pen p = new Pen(Theme.Line, 1f))
            {
                g.DrawLine(p, Theme.S(28), Theme.S(112), W - Theme.S(28), Theme.S(112));
            }

            if (spinner.Active)
            {
                Rectangle sp = new Rectangle(btnBack.Left - Theme.S(26), ClientSize.Height - Theme.S(64),
                    Theme.S(16), Theme.S(16));
                spinner.Draw(g, sp, Theme.Accent2);
            }
        }

        private void UpdateFooter()
        {
            if (step == 0)
            {
                if (currentSource != null)
                {
                    lblFooter.ForeColor = Theme.Muted;
                    lblFooter.Text = "已选择安装源：" + currentSource.Path;
                }
                else
                {
                    lblFooter.ForeColor = Theme.Warn;
                    lblFooter.Text = "正在扫描安装介质…也可以手动选择文件夹或 ISO。";
                }
                btnNext.Enabled = currentSource != null;
            }
            else if (step == 1)
            {
                if (currentEdition != null)
                {
                    lblFooter.ForeColor = Theme.Muted;
                    lblFooter.Text = "已选择：" + currentEdition.Name +
                                     (string.IsNullOrEmpty(currentEdition.EditionId)
                                         ? "   ·   EditionID 未指定"
                                         : "   ·   " + currentEdition.EditionId);
                }
                else
                {
                    lblFooter.ForeColor = Theme.Warn;
                    lblFooter.Text = "请选择一个目标版本。";
                }
                btnNext.Enabled = currentEdition != null;
            }
            else
            {
                lblFooter.ForeColor = Theme.Muted;
                lblFooter.Text = "目标：" + (currentEdition == null ? "-" : currentEdition.Name) +
                                 "   ·   方式：" + ModeName(currentMode);
                btnStart.Enabled = !busy && currentEdition != null && currentSource != null;
            }
        }

        private static string ModeName(RunMode m)
        {
            if (m == RunMode.KeyOnly) return "仅更换密钥";
            if (m == RunMode.Normal) return "普通升级";
            if (m == RunMode.Pkey) return "指定版本升级";
            return "强制升级";
        }

        private void SetBusy(bool value)
        {
            busy = value;
            btnRescan.Enabled = !value;
            btnFolder.Enabled = !value;
            btnIso.Enabled = !value;
            btnBack.Enabled = !value;
            btnNext.Enabled = !value && (step == 0 ? currentSource != null : currentEdition != null);
            btnStart.Enabled = !value;
            spinner.Active = value;
            Cursor = value ? Cursors.AppStarting : Cursors.Default;
            UpdateFooter();
        }
    }
}
