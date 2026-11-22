using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UpgradeTool
{
    internal class InputField : Panel
    {
        public readonly TextBox Box;
        private bool focused;

        public InputField()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.BgTop;

            Box = new TextBox();
            Box.BorderStyle = BorderStyle.None;
            Box.BackColor = Theme.Sunken;
            Box.ForeColor = Theme.Text;
            Box.Font = Theme.Body;
            Box.Location = new Point(Theme.S(12), Theme.S(10));
            Box.Width = 100;
            Box.GotFocus += delegate { focused = true; Invalidate(); };
            Box.LostFocus += delegate { focused = false; Invalidate(); };
            Controls.Add(Box);

            Height = Theme.S(38);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Box != null) Box.Width = Math.Max(0, Width - Theme.S(24));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Theme.FillRound(g, r, Theme.S(8), Theme.Sunken);
            Theme.StrokeRound(g, r, Theme.S(8), focused ? Theme.Accent : Theme.Line,
                focused ? Theme.SM(1.4f) : 1f);
        }
    }

    internal class CustomDialog : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private InputField keyField, idField, nameField;
        private Label lblError;
        public Edition Result;

        public CustomDialog()
        {
            Text = "自定义版本";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(540), Theme.S(352));
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
            title.Size = new Size(Theme.S(400), Theme.S(28));
            title.Font = Theme.H1;
            title.ForeColor = Theme.Text;
            title.BackColor = Color.Transparent;
            title.Text = "自定义版本";
            Controls.Add(title);

            Label sub = new Label();
            sub.AutoSize = false;
            sub.Location = new Point(Theme.S(24), Theme.S(50));
            sub.Size = new Size(Theme.S(490), Theme.S(20));
            sub.Font = Theme.Small;
            sub.ForeColor = Theme.Muted;
            sub.BackColor = Color.Transparent;
            sub.Text = "密钥只用于选择安装版本，不能激活系统。适用于版本列表里没有的版本。";
            Controls.Add(sub);

            Caption("预安装密钥（25 位，可带或不带连字符）", 84);
            keyField = new InputField();
            keyField.Location = new Point(Theme.S(24), Theme.S(102));
            keyField.Size = new Size(Theme.S(492), Theme.S(38));
            Controls.Add(keyField);

            Caption("EditionID（可选，强制升级需要）", 150);
            idField = new InputField();
            idField.Location = new Point(Theme.S(24), Theme.S(168));
            idField.Size = new Size(Theme.S(492), Theme.S(38));
            Controls.Add(idField);

            Caption("ProductName（可选，默认当前系统值）", 216);
            nameField = new InputField();
            nameField.Location = new Point(Theme.S(24), Theme.S(234));
            nameField.Size = new Size(Theme.S(492), Theme.S(38));
            nameField.Box.Text = Program.CurrentProductName();
            Controls.Add(nameField);

            lblError = new Label();
            lblError.AutoSize = false;
            lblError.Location = new Point(Theme.S(24), Theme.S(280));
            lblError.Size = new Size(Theme.S(492), Theme.S(20));
            lblError.Font = Theme.Small;
            lblError.ForeColor = Theme.Danger;
            lblError.BackColor = Color.Transparent;
            lblError.Text = "";
            Controls.Add(lblError);

            UButton btnCancel = new UButton("取消");
            btnCancel.Location = new Point(Theme.S(292), Theme.S(302));
            btnCancel.Size = new Size(Theme.S(110), Theme.S(38));
            btnCancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(btnCancel);

            UButton btnOk = new UButton("确定");
            btnOk.Primary = true;
            btnOk.Location = new Point(Theme.S(410), Theme.S(302));
            btnOk.Size = new Size(Theme.S(106), Theme.S(38));
            btnOk.Click += delegate { Accept(); };
            Controls.Add(btnOk);

            WindowButton close = new WindowButton();
            close.IsClose = true;
            close.Location = new Point(ClientSize.Width - Theme.S(42), Theme.S(12));
            close.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(close);

            Load += delegate
            {
                using (GraphicsPath p = Theme.RoundRect(new Rectangle(0, 0, Width, Height), Theme.S(12)))
                {
                    Region = new Region(p);
                }
                keyField.Box.Focus();
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

        private void Caption(string text, int y)
        {
            Label l = new Label();
            l.AutoSize = false;
            l.Location = new Point(Theme.S(24), Theme.S(y));
            l.Size = new Size(Theme.S(492), Theme.S(18));
            l.Font = Theme.Small;
            l.ForeColor = Theme.Muted;
            l.BackColor = Color.Transparent;
            l.Text = text;
            Controls.Add(l);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            Theme.StrokeRound(g, new Rectangle(0, 0, Width - 1, Height - 1), Theme.S(12), Theme.Line, 1f);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter) Accept();
            else if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
        }

        private void Accept()
        {
            string raw = keyField.Box.Text.Replace(" ", "").Replace("-", "").Trim();
            if (raw.Length != 25)
            {
                lblError.Text = "密钥应为 25 位，当前 " + raw.Length + " 位。";
                keyField.Box.Focus();
                return;
            }
            string formatted = raw.Substring(0, 5) + "-" + raw.Substring(5, 5) + "-" +
                               raw.Substring(10, 5) + "-" + raw.Substring(15, 5) + "-" +
                               raw.Substring(20, 5);

            string id = idField.Box.Text.Trim();
            string name = nameField.Box.Text.Trim();
            if (name.Length == 0) name = Program.CurrentProductName();

            Result = new Edition(28, "自定义版本", formatted, id, name, id, "自定义版本");
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
