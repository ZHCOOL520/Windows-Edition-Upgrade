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
        private const int WinW = 1080;
        private const int WinH = 720;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private int step;
        private NavPanel nav;
        private NavItem navSource, navEdition, navMode, navAbout;
        private ThemeSwitch themeSwitch;
        private PagePanel pageSource, pageEdition, pageMode;
        private ScrollCard sourceCard, editionCard;
        private Panel targetCard, logCard;
        private Label lblSourceHint, lblEditionHint;
        private UButton btnRescan, btnFolder, btnIso;
        private UButton btnBack, btnNext, btnStart, btnExportLog;
        private WindowButton btnClose, btnMin;

        private readonly List<string> logLines = new List<string>();
        private readonly List<MediaSource> sources = new List<MediaSource>();
        private readonly List<ModeCard> modeCards = new List<ModeCard>();
        private MediaSource currentSource;
        private Edition currentEdition;
        private RunMode currentMode = RunMode.Pkey;
        private Timer anim;
        private readonly Spinner spinner = new Spinner();
        private bool busy;

        private string footerText = "";
        private Color footerColor = Theme.Muted;
        private int footerLineY;

        private int contentLeft;
        private Control slidePage;
        private int slideTicks;
        private float navIndicatorY;
        private float navIndicatorTarget;
        private bool navIndicatorInit;

        private Palette palFrom, palTo;
        private float paletteT = 1f;
        private float fadeTicks;

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
            AutoScaleMode = AutoScaleMode.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Theme.S(WinW), Theme.S(WinH));
            MinimumSize = new Size(Theme.S(820), Theme.S(560));
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            if (Width > work.Width) ClientSize = new Size(work.Width - Theme.S(24), ClientSize.Height);
            if (Height > work.Height) ClientSize = new Size(ClientSize.Width, work.Height - Theme.S(24));
            if (MinimumSize.Width > ClientSize.Width) MinimumSize = new Size(ClientSize.Width, MinimumSize.Height);
            if (MinimumSize.Height > ClientSize.Height) MinimumSize = new Size(MinimumSize.Width, ClientSize.Height);

            Size? saved = Program.ReadWindowSize();
            if (saved.HasValue)
            {
                int sw = Math.Min(saved.Value.Width, work.Width - Theme.S(24));
                int sh = Math.Min(saved.Value.Height, work.Height - Theme.S(24));
                if (sw >= MinimumSize.Width && sh >= MinimumSize.Height)
                    ClientSize = new Size(sw, sh);
            }
            FormClosing += delegate
            {
                if (WindowState == FormWindowState.Normal)
                    Program.SaveWindowSize(ClientSize.Width, ClientSize.Height);
            };
            BackColor = Theme.BgTop;
            Font = Theme.Body;
            DoubleBuffered = true;
            KeyPreview = true;
            Opacity = 0;

            BuildUi();

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
            using (GraphicsPath p = Theme.RoundRect(new Rectangle(0, 0, Width, Height), Theme.S(8)))
            {
                Region = new Region(p);
            }
        }

        private void BuildUi()
        {
            nav = new NavPanel();
            nav.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, 0xA1, 0x2, 0);
                }
            };
            try
            {
                if (Icon != null) nav.AppIcon = Icon.ToBitmap();
            }
            catch
            {
            }
            Controls.Add(nav);

            navSource = MakeNav("安装源", GlyphKind.Disc, 0);
            navEdition = MakeNav("目标版本", GlyphKind.Grid, 1);
            navMode = MakeNav("执行方式", GlyphKind.Play, 2);

            navAbout = new NavItem();
            navAbout.Text = "关于";
            navAbout.Glyph = GlyphKind.Info;
            navAbout.Click += delegate { ShowAbout(); };
            nav.Controls.Add(navAbout);

            themeSwitch = new ThemeSwitch();
            themeSwitch.On = !Theme.Light;
            themeSwitch.Knob = themeSwitch.On ? 1f : 0f;
            themeSwitch.Click += delegate { ToggleTheme(); };
            nav.Controls.Add(themeSwitch);

            pageSource = new PagePanel();
            pageSource.PageTitle = "安装源";
            pageSource.PageSubtitle = "需要包含 setup.exe 和 sources 文件夹的目录，也可以直接选择 ISO 镜像自动挂载。";
            Controls.Add(pageSource);

            pageEdition = new PagePanel();
            pageEdition.PageTitle = "目标版本";
            pageEdition.PageSubtitle = "常规版本可以直接升级；LTSC 和 Server 需要对应的独立安装介质。";
            Controls.Add(pageEdition);

            pageMode = new PagePanel();
            pageMode.PageTitle = "执行方式";
            pageMode.PageSubtitle = "先确认目标版本，再选择一种执行方式并开始。";
            Controls.Add(pageMode);

            BuildSourcePage();
            BuildEditionPage();
            BuildModePage();
            BuildCommandBar();

            Controls.Add(pageSource);
            Controls.Add(pageEdition);
            Controls.Add(pageMode);
            Controls.Add(btnBack);
            Controls.Add(btnNext);
            Controls.Add(btnStart);
            Controls.Add(btnMin);
            Controls.Add(btnClose);
        }

        private NavItem MakeNav(string text, GlyphKind glyph, int target)
        {
            NavItem item = new NavItem();
            item.Text = text;
            item.Glyph = glyph;
            item.Click += delegate { GoStep(target); };
            nav.Controls.Add(item);
            return item;
        }

        private void BuildSourcePage()
        {
            sourceCard = new ScrollCard();
            sourceCard.EmptyText = "未找到安装介质。可以把脚本放到安装介质根目录，或选择已挂载的 ISO / 解压后的文件夹。";
            pageSource.Controls.Add(sourceCard);

            btnRescan = new UButton("重新扫描");
            btnRescan.Click += delegate { ScanSources(true); };
            pageSource.Controls.Add(btnRescan);

            btnFolder = new UButton("选择文件夹");
            btnFolder.Click += delegate { PickFolder(); };
            pageSource.Controls.Add(btnFolder);

            btnIso = new UButton("选择 ISO");
            btnIso.Click += delegate { PickIso(); };
            pageSource.Controls.Add(btnIso);

            lblSourceHint = new Label();
            lblSourceHint.AutoSize = false;
            lblSourceHint.Font = Theme.Small;
            lblSourceHint.ForeColor = Theme.Dim;
            lblSourceHint.BackColor = Theme.BgTop;
            lblSourceHint.Text = "";
            pageSource.Controls.Add(lblSourceHint);
        }

        private void BuildEditionPage()
        {
            editionCard = new ScrollCard();
            pageEdition.Controls.Add(editionCard);

            lblEditionHint = new Label();
            lblEditionHint.AutoSize = false;
            lblEditionHint.Font = Theme.Small;
            lblEditionHint.ForeColor = Theme.Muted;
            lblEditionHint.BackColor = Theme.BgTop;
            lblEditionHint.Text = "";
            pageEdition.Controls.Add(lblEditionHint);
        }

        private void BuildModePage()
        {
            targetCard = new Panel();
            targetCard.BackColor = Theme.BgTop;
            targetCard.Paint += PaintTargetCard;
            pageMode.Controls.Add(targetCard);

            string[] titles = { "仅更换密钥", "普通升级", "指定版本升级", "强制升级" };
            string[] descs =
            {
                "用 slmgr 安装所选密钥，只切换版本，不做就地升级。",
                "不指定版本，由安装程序自行判断。",
                "就地升级到所选版本，自动传入密钥。",
                "改写注册表伪装版本，可跨过升级限制，需二次确认。"
            };
            string[] keys = { "K", "S", "U", "F" };
            RunMode[] modes = { RunMode.KeyOnly, RunMode.Normal, RunMode.Pkey, RunMode.Force };
            GlyphKind[] glyphs = { GlyphKind.Key, GlyphKind.ArrowUp, GlyphKind.Target, GlyphKind.Warning };

            for (int i = 0; i < 4; i++)
            {
                ModeCard c = new ModeCard();
                c.Text = titles[i];
                c.Description = descs[i];
                c.Shortcut = keys[i];
                c.Mode = modes[i];
                c.Glyph = glyphs[i];
                c.Click += ModeCardClick;
                pageMode.Controls.Add(c);
                modeCards.Add(c);
            }

            logCard = new Panel();
            logCard.BackColor = Theme.BgTop;
            logCard.Paint += PaintLogCard;
            pageMode.Controls.Add(logCard);

            btnExportLog = new UButton("导出日志");
            btnExportLog.Subtle = true;
            btnExportLog.Click += delegate { ExportLog(); };
            logCard.Controls.Add(btnExportLog);
        }

        private void BuildCommandBar()
        {
            btnBack = new UButton("上一步");
            btnBack.Click += delegate { GoStep(step - 1); };

            btnNext = new UButton("下一步");
            btnNext.Primary = true;
            btnNext.Click += delegate { GoStep(step + 1); };

            btnStart = new UButton("开始执行");
            btnStart.Primary = true;
            btnStart.Click += delegate { StartRun(); };

            btnClose = new WindowButton();
            btnClose.Kind = GlyphKind.Close;
            btnClose.IsClose = true;
            btnClose.Click += delegate { Close(); };

            btnMin = new WindowButton();
            btnMin.Kind = GlyphKind.Minimize;
            btnMin.Click += delegate { WindowState = FormWindowState.Minimized; };
        }

        private void ToggleTheme()
        {
            bool light = !Theme.Light;
            palFrom = Theme.Capture();
            palTo = Theme.Target(light);
            Theme.Light = light;
            paletteT = 0f;
            themeSwitch.On = !light;
            Program.SaveTheme(light);
        }

        private void ShowAbout()
        {
            using (AboutDialog dlg = new AboutDialog())
            {
                dlg.ShowDialog(this);
            }
        }

        private void ApplyThemeSurfaces()
        {
            BackColor = Theme.BgTop;
            pageSource.BackColor = Theme.BgTop;
            pageEdition.BackColor = Theme.BgTop;
            pageMode.BackColor = Theme.BgTop;
            sourceCard.BackColor = Theme.BgTop;
            editionCard.BackColor = Theme.BgTop;
            logCard.BackColor = Theme.BgTop;
            targetCard.BackColor = Theme.BgTop;
            lblSourceHint.BackColor = Theme.BgTop;
            lblEditionHint.BackColor = Theme.BgTop;
            lblSourceHint.ForeColor = Theme.Dim;
            lblEditionHint.ForeColor = Theme.Muted;
            nav.BackColor = Theme.BgTop;
            InvalidateAll(this);
            nav.Invalidate();
        }

        private void InvalidateAll(Control c)
        {
            c.Invalidate();
            foreach (Control child in c.Controls) InvalidateAll(child);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (nav == null) return;
            LayoutAll();
            UpdateRegion();
            Invalidate();
        }

        private void LayoutAll()
        {
            if (nav == null) return;
            int W = ClientSize.Width;
            int H = ClientSize.Height;

            bool compact = W < Theme.S(940);
            int navW = compact ? Theme.S(64) : Theme.S(232);
            nav.SetBounds(Theme.S(5), 0, navW, H);
            nav.Compact = compact;

            int itemH = Theme.S(40);
            int itemX = compact ? (navW - Theme.S(40)) / 2 : Theme.S(11);
            int itemW = compact ? Theme.S(40) : navW - Theme.S(22);
            int itemY = Theme.S(86);
            navSource.SetBounds(itemX, itemY, itemW, itemH);
            navEdition.SetBounds(itemX, itemY + Theme.S(44), itemW, itemH);
            navMode.SetBounds(itemX, itemY + Theme.S(88), itemW, itemH);
            navSource.Compact = compact;
            navEdition.Compact = compact;
            navMode.Compact = compact;
            navAbout.Compact = compact;
            navAbout.SetBounds(itemX, H - Theme.S(96), itemW, itemH);

            themeSwitch.Compact = compact;
            themeSwitch.SetBounds(compact ? itemX : Theme.S(12), H - Theme.S(148),
                compact ? Theme.S(40) : navW - Theme.S(26), Theme.S(32));

            int cbW = Theme.S(46);
            int cbH = Theme.S(40);
            btnClose.SetBounds(W - cbW, 0, cbW, cbH);
            btnMin.SetBounds(W - cbW * 2, 0, cbW, cbH);

            contentLeft = navW + Theme.S(28);
            int contentW = Math.Max(Theme.S(380), W - contentLeft - Theme.S(28));
            int top = Theme.S(60);
            footerLineY = H - Theme.S(72);
            int pageH = Math.Max(Theme.S(300), footerLineY - Theme.S(10) - top);

            pageSource.AutoScroll = false;
            pageEdition.AutoScroll = false;
            pageMode.AutoScroll = false;
            int w = contentW;

            pageSource.SetBounds(contentLeft, top, contentW, pageH);
            pageEdition.SetBounds(contentLeft, top, contentW, pageH);
            pageMode.SetBounds(contentLeft, top, contentW, pageH);
            if (slidePage != null) slidePage.Left = contentLeft;

            sourceCard.SetBounds(0, Theme.S(76), w, Math.Max(Theme.S(100), pageH - Theme.S(76) - Theme.S(56)));
            int by = pageH - Theme.S(38);
            btnRescan.SetBounds(0, by, Theme.S(120), Theme.S(34));
            btnFolder.SetBounds(Theme.S(130), by, Theme.S(120), Theme.S(34));
            btnIso.SetBounds(Theme.S(260), by, Theme.S(120), Theme.S(34));
            lblSourceHint.SetBounds(Theme.S(394), by + Theme.S(7),
                Math.Max(Theme.S(120), w - Theme.S(400)), Theme.S(22));

            editionCard.SetBounds(0, Theme.S(76), w, Math.Max(Theme.S(100), pageH - Theme.S(76) - Theme.S(34)));
            lblEditionHint.SetBounds(0, pageH - Theme.S(26), w, Theme.S(20));

            targetCard.SetBounds(0, Theme.S(76), w, Theme.S(70));
            int gap = Theme.S(12);
            int cardW = (w - gap) / 2;
            int tileY = Theme.S(152);
            int tileH = Theme.S(84);
            for (int i = 0; i < modeCards.Count; i++)
            {
                modeCards[i].SetBounds((i % 2) * (cardW + gap),
                    tileY + (i / 2) * (tileH + Theme.S(8)), cardW, tileH);
            }
            int logY = tileY + 2 * tileH + Theme.S(16);
            logCard.SetBounds(0, logY, w, Math.Max(Theme.S(80), pageH - logY));
            btnExportLog.SetBounds(logCard.Width - Theme.S(100), Theme.S(10), Theme.S(88), Theme.S(28));

            int bh = Theme.S(36);
            int bw = Theme.S(140);
            int fy = footerLineY + (H - footerLineY - bh) / 2;
            btnStart.SetBounds(W - Theme.S(28) - bw, fy, bw, bh);
            btnNext.SetBounds(W - Theme.S(28) - bw, fy, bw, bh);
            btnBack.SetBounds(W - Theme.S(28) - bw - Theme.S(12) - Theme.S(112), fy, Theme.S(112), bh);

            if (!navIndicatorInit)
            {
                navIndicatorTarget = NavTop(SelectedNav());
                navIndicatorY = navIndicatorTarget;
                nav.IndicatorY = navIndicatorY;
                navIndicatorInit = true;
            }
        }

        private NavItem SelectedNav()
        {
            if (step == 0) return navSource;
            if (step == 1) return navEdition;
            return navMode;
        }

        private float NavTop(NavItem item)
        {
            return item.Top + (item.Height - Theme.S(16)) / 2f;
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
            if (e.Y < Theme.S(56))
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
            pageSource.Visible = step == 0;
            pageEdition.Visible = step == 1;
            pageMode.Visible = step == 2;

            navSource.Selected = step == 0;
            navEdition.Selected = step == 1;
            navMode.Selected = step == 2;
            navSource.Invalidate();
            navEdition.Invalidate();
            navMode.Invalidate();

            navIndicatorTarget = NavTop(SelectedNav());
            if (!navIndicatorInit)
            {
                navIndicatorY = navIndicatorTarget;
                nav.IndicatorY = navIndicatorY;
                navIndicatorInit = true;
            }

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
            slidePage = step == 0 ? pageSource : (step == 1 ? pageEdition : pageMode);
            slideTicks = 0;
            slidePage.Left = contentLeft + Theme.S(28);
        }

        private void AnimTick(object sender, EventArgs e)
        {
            if (fadeTicks < 1f)
            {
                fadeTicks += 0.08f;
                if (fadeTicks > 1f) fadeTicks = 1f;
                Opacity = fadeTicks;
            }

            if (paletteT < 1f)
            {
                paletteT += 0.1f;
                if (paletteT > 1f) paletteT = 1f;
                float t = 1f - (1f - paletteT) * (1f - paletteT);
                Theme.Blend(palFrom, palTo, t);
                ApplyThemeSurfaces();
            }

            if (slidePage != null)
            {
                slideTicks++;
                float t = Math.Min(1f, slideTicks / 14f);
                float ease = 1f - (float)Math.Pow(1f - t, 3);
                slidePage.Left = contentLeft + (int)(Theme.S(28) * (1f - ease));
                if (t >= 1f)
                {
                    slidePage.Left = contentLeft;
                    slidePage = null;
                }
            }

            if (navIndicatorInit && Math.Abs(navIndicatorY - navIndicatorTarget) > 0.15f)
            {
                navIndicatorY += (navIndicatorTarget - navIndicatorY) * 0.28f;
                if (Math.Abs(navIndicatorY - navIndicatorTarget) <= 0.15f)
                    navIndicatorY = navIndicatorTarget;
                nav.IndicatorY = navIndicatorY;
                nav.Invalidate(new Rectangle(0, Theme.S(70), Theme.S(12), ClientSize.Height - Theme.S(70)));
            }

            if (spinner.Active)
            {
                spinner.Angle = (spinner.Angle + 10f) % 360f;
                Invalidate(new Rectangle(0, footerLineY, ClientSize.Width, ClientSize.Height - footerLineY));
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Theme.BgTop);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (nav == null) return;
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);

            int left = nav.Left + nav.Width;
            using (Pen p = new Pen(Theme.Divider, 1f))
            {
                g.DrawLine(p, left, footerLineY, ClientSize.Width, footerLineY);
            }

            int textW = btnBack.Left - Theme.S(24) - contentLeft;
            if (textW > Theme.S(80))
            {
                TextRenderer.DrawText(g, footerText, Theme.Body,
                    new Rectangle(contentLeft, footerLineY, textW, ClientSize.Height - footerLineY),
                    footerColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            if (spinner.Active)
            {
                int sz = Theme.S(16);
                Rectangle sp = new Rectangle(btnBack.Left - Theme.S(30),
                    footerLineY + (ClientSize.Height - footerLineY - sz) / 2, sz, sz);
                spinner.Draw(g, sp, Theme.Accent);
            }
        }

        private void UpdateFooter()
        {
            if (step == 0)
            {
                if (currentSource != null)
                {
                    footerColor = Theme.Muted;
                    footerText = "已选择安装源：" + currentSource.Path;
                }
                else
                {
                    footerColor = Theme.Warn;
                    footerText = "正在扫描安装介质…也可以手动选择文件夹或 ISO。";
                }
                btnNext.Enabled = currentSource != null;
            }
            else if (step == 1)
            {
                if (currentEdition != null)
                {
                    footerColor = Theme.Muted;
                    footerText = "已选择：" + currentEdition.Name +
                                 (string.IsNullOrEmpty(currentEdition.EditionId)
                                     ? "   ·   EditionID 未指定"
                                     : "   ·   " + currentEdition.EditionId);
                }
                else
                {
                    footerColor = Theme.Warn;
                    footerText = "请选择一个目标版本。";
                }
                btnNext.Enabled = currentEdition != null;
            }
            else
            {
                footerColor = Theme.Muted;
                footerText = (currentEdition == null ? "-" : currentEdition.Name) +
                             "   ·   " + ModeName(currentMode);
                btnStart.Enabled = !busy && currentEdition != null && currentSource != null;
            }
            Invalidate(new Rectangle(0, footerLineY, ClientSize.Width, ClientSize.Height - footerLineY));
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

        // 供截图 / 自动化验证使用：跳转到指定步骤并停在动画结束态
        internal void ReflectStep(int target)
        {
            if (target >= 1 && currentSource == null)
            {
                currentSource = sources.Count > 0
                    ? sources[0]
                    : new MediaSource("D:\\", "D:  安装介质（预览）");
                RefreshSourceSelection();
            }
            if (target >= 2 && currentEdition == null)
            {
                currentEdition = Data.Editions[1];
                HighlightEdition();
            }
            GoStep(target);
            slidePage = null;
            pageSource.Left = contentLeft;
            pageEdition.Left = contentLeft;
            pageMode.Left = contentLeft;
            navIndicatorY = navIndicatorTarget;
            nav.IndicatorY = navIndicatorY;
            fadeTicks = 1f;
            Opacity = 1;
            Invalidate(true);
            Application.DoEvents();
        }
    }
}
