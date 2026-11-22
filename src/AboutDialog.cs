using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UpgradeTool
{
    internal class AboutDialog : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public AboutDialog()
        {
            Text = "关于作者";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(540), Theme.S(412));
            BackColor = Theme.BgTop;
            Font = Theme.Body;
            DoubleBuffered = true;
            KeyPreview = true;
            ShowInTaskbar = false;
            try
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
            }

            Label title = new Label();
            title.AutoSize = false;
            title.Location = new Point(Theme.S(24), Theme.S(18));
            title.Size = new Size(Theme.S(300), Theme.S(28));
            title.Font = Theme.H1;
            title.ForeColor = Theme.Text;
            title.BackColor = Color.Transparent;
            title.Text = "关于作者";
            Controls.Add(title);

            Label app = new Label();
            app.AutoSize = false;
            app.Location = new Point(Theme.S(24), Theme.S(54));
            app.Size = new Size(Theme.S(480), Theme.S(26));
            app.Font = Theme.H2;
            app.ForeColor = Theme.Text;
            app.BackColor = Color.Transparent;
            app.Text = Data.AppName + "  " + Data.Version;
            Controls.Add(app);

            Label ver = new Label();
            ver.AutoSize = false;
            ver.Location = new Point(Theme.S(24), Theme.S(82));
            ver.Size = new Size(Theme.S(480), Theme.S(20));
            ver.Font = Theme.Small;
            ver.ForeColor = Theme.Muted;
            ver.BackColor = Color.Transparent;
            ver.Text = "Windows 10 / 11 / Server 就地升级与版本切换工具 · 2026 年 11 月";
            Controls.Add(ver);

            Label author = new Label();
            author.AutoSize = false;
            author.Location = new Point(Theme.S(24), Theme.S(126));
            author.Size = new Size(Theme.S(480), Theme.S(22));
            author.Font = Theme.Body;
            author.ForeColor = Theme.Text;
            author.BackColor = Color.Transparent;
            author.Text = "作者：ZHCOOL";
            Controls.Add(author);

            LinkLabel site = Link("https://zhcool.xyz", 150);
            site.Click += delegate { OpenUrl(Data.Website); };
            Controls.Add(site);

            Label feedback = new Label();
            feedback.AutoSize = false;
            feedback.Location = new Point(Theme.S(24), Theme.S(204));
            feedback.Size = new Size(Theme.S(492), Theme.S(22));
            feedback.Font = Theme.H2;
            feedback.ForeColor = Theme.Text;
            feedback.BackColor = Color.Transparent;
            feedback.Text = "问题反馈";
            Controls.Add(feedback);

            Label hint = new Label();
            hint.AutoSize = false;
            hint.Location = new Point(Theme.S(24), Theme.S(232));
            hint.Size = new Size(Theme.S(492), Theme.S(44));
            hint.Font = Theme.Small;
            hint.ForeColor = Theme.Muted;
            hint.BackColor = Color.Transparent;
            hint.Text = "优先在 GitHub 仓库提交 Issue。如果网络不好打不开，可以改用下面的 QQ 邮箱反馈。";
            Controls.Add(hint);

            LinkLabel repo = Link(Data.RepoUrl, 278);
            repo.Click += delegate { OpenUrl(Data.RepoUrl + "/issues"); };
            Controls.Add(repo);

            string mail = Data.QqMail == null ? "" : Data.QqMail.Trim();
            Label mailCaption = new Label();
            mailCaption.AutoSize = false;
            mailCaption.Location = new Point(Theme.S(24), Theme.S(316));
            mailCaption.Size = new Size(Theme.S(120), Theme.S(22));
            mailCaption.Font = Theme.Body;
            mailCaption.ForeColor = Theme.Muted;
            mailCaption.BackColor = Color.Transparent;
            mailCaption.Text = "QQ 邮箱";
            Controls.Add(mailCaption);

            Label mailValue = new Label();
            mailValue.AutoSize = false;
            mailValue.Location = new Point(Theme.S(112), Theme.S(316));
            mailValue.Size = new Size(Theme.S(300), Theme.S(22));
            mailValue.Font = Theme.Mono;
            mailValue.ForeColor = mail.Length > 0 ? Theme.Accent2 : Theme.Dim;
            mailValue.BackColor = Color.Transparent;
            mailValue.Text = mail.Length > 0 ? mail : "zhcool520@qq.com";
            Controls.Add(mailValue);

            if (mail.Length > 0)
            {
                UButton copy = new UButton("复制");
                copy.Location = new Point(Theme.S(420), Theme.S(310));
                copy.Size = new Size(Theme.S(96), Theme.S(32));
                copy.Click += delegate
                {
                    try
                    {
                        Clipboard.SetText(mail);
                        mailValue.Text = mail + "（已复制）";
                    }
                    catch
                    {
                    }
                };
                Controls.Add(copy);
            }

            UButton btnSite = new UButton("打开作者网站");
            btnSite.Primary = true;
            btnSite.Location = new Point(ClientSize.Width - Theme.S(24) - Theme.S(148), Theme.S(358));
            btnSite.Size = new Size(Theme.S(148), Theme.S(38));
            btnSite.Click += delegate { OpenUrl(Data.Website); };
            Controls.Add(btnSite);

            UButton btnClose = new UButton("关闭");
            btnClose.Location = new Point(ClientSize.Width - Theme.S(24) - Theme.S(148) - Theme.S(118),
                Theme.S(358));
            btnClose.Size = new Size(Theme.S(108), Theme.S(38));
            btnClose.Click += delegate { Close(); };
            Controls.Add(btnClose);

            WindowButton close = new WindowButton();
            close.IsClose = true;
            close.Location = new Point(ClientSize.Width - Theme.S(42), Theme.S(12));
            close.Click += delegate { Close(); };
            Controls.Add(close);

            Load += delegate
            {
                using (GraphicsPath p = Theme.RoundRect(new Rectangle(0, 0, Width, Height), Theme.S(12)))
                {
                    Region = new Region(p);
                }
            };
            MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, 0xA1, 0x2, 0);
                }
            };
        }

        private LinkLabel Link(string text, int y)
        {
            LinkLabel l = new LinkLabel();
            l.AutoSize = false;
            l.Location = new Point(Theme.S(24), Theme.S(y));
            l.Size = new Size(Theme.S(492), Theme.S(22));
            l.Font = Theme.Body;
            l.LinkColor = Theme.Accent;
            l.ActiveLinkColor = Theme.Accent2;
            l.VisitedLinkColor = Theme.Accent;
            l.LinkBehavior = LinkBehavior.HoverUnderline;
            l.BackColor = Color.Transparent;
            l.Text = text;
            l.Cursor = Cursors.Hand;
            return l;
        }

        private static void OpenUrl(string url)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(url);
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
            catch
            {
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            Theme.StrokeRound(g, new Rectangle(0, 0, Width - 1, Height - 1), Theme.S(12), Theme.Line, 1f);
            using (Pen p = new Pen(Theme.Line, 1f))
            {
                g.DrawLine(p, Theme.S(24), Theme.S(112), Width - Theme.S(24), Theme.S(112));
                g.DrawLine(p, Theme.S(24), Theme.S(186), Width - Theme.S(24), Theme.S(186));
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape) Close();
        }
    }
}
