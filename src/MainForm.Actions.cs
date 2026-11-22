using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace UpgradeTool
{
    internal partial class MainForm
    {
        private readonly List<AnimatedCard> sourceCards = new List<AnimatedCard>();
        private readonly List<AnimatedCard> editionCards = new List<AnimatedCard>();
        private AnimatedCard customCard;
        private Label sourceEmpty;

        private void ScanSources(bool reset)
        {
            sources.Clear();
            sources.AddRange(Program.DetectSources());
            RebuildSourceRows();

            if (reset || currentSource == null || !sources.Exists(delegate(MediaSource m) { return m.Path == currentSource.Path; }))
                currentSource = sources.Count > 0 ? sources[0] : null;

            foreach (AnimatedCard c in sourceCards)
            {
                MediaSource m = c.Tag as MediaSource;
                c.Selected = m != null && currentSource != null && m.Path == currentSource.Path;
                c.Invalidate();
            }
            if (sources.Count > 0)
                lblSourceHint.Text = "自动扫描到 " + sources.Count + " 个可用安装源，选中后点「下一步」。";
            else
                lblSourceHint.Text = "没有扫描到安装介质，请手动选择文件夹或 ISO。";
            UpdateFooter();
        }

        private void RebuildSourceRows()
        {
            sourceList.SuspendLayout();
            sourceList.Controls.Clear();
            foreach (AnimatedCard c in sourceCards) allCards.Remove(c);
            sourceCards.Clear();
            sourceEmpty = null;

            if (sources.Count == 0)
            {
                sourceEmpty = new Label();
                sourceEmpty.AutoSize = false;
                sourceEmpty.Size = new Size(Theme.S(880), Theme.S(40));
                sourceEmpty.Location = new Point(Theme.S(6), Theme.S(10));
                sourceEmpty.Font = Theme.Small;
                sourceEmpty.ForeColor = Theme.Dim;
                sourceEmpty.BackColor = Theme.BgTop;
                sourceEmpty.Text = "未找到安装介质。可以把脚本放到安装介质根目录，或选择已挂载的 ISO / 解压后的文件夹。";
                sourceList.Controls.Add(sourceEmpty);
            }
            else
            {
                for (int i = 0; i < sources.Count; i++)
                {
                    MediaSource m = sources[i];
                    AnimatedCard c = new AnimatedCard();
                    c.Text = m.Label;
                    c.SubText = m.Path;
                    c.RightText = "可用";
                    c.Height = Theme.S(62);
                    c.Tag = m;
                    c.Click += SourceCardClick;
                    sourceList.Controls.Add(c);
                    sourceCards.Add(c);
                    allCards.Add(c);
                }
            }
            sourceList.ResumeLayout(false);
            LayoutSourceRows();
        }

        private void RefreshSourceSelection()
        {
            foreach (AnimatedCard c in sourceCards)
            {
                MediaSource m = c.Tag as MediaSource;
                c.Selected = m != null && currentSource != null && m.Path == currentSource.Path;
                c.Invalidate();
            }
        }

        private void LayoutSourceRows()
        {
            int y = 0;
            int w = sourceList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4;
            if (sourceEmpty != null)
            {
                sourceEmpty.Width = Math.Max(Theme.S(400), w);
                sourceEmpty.Left = Theme.S(6);
                sourceList.PerformLayout();
                return;
            }
            foreach (AnimatedCard c in sourceCards)
            {
                c.Left = 0;
                c.Top = y;
                c.Width = Math.Max(Theme.S(400), w);
                y += c.Height + Theme.S(8);
            }
            sourceList.PerformLayout();
        }

        private void SourceCardClick(object sender, EventArgs e)
        {
            AnimatedCard c = sender as AnimatedCard;
            if (c == null || busy) return;
            MediaSource m = c.Tag as MediaSource;
            if (m == null) return;
            currentSource = m;
            foreach (AnimatedCard card in sourceCards)
            {
                card.Selected = card == c;
                card.Invalidate();
            }
            lblSourceHint.Text = "已选择：" + m.Path;
            UpdateFooter();
        }

        private void PickFolder()
        {
            if (busy) return;
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "选择包含 setup.exe 和 sources 文件夹的安装介质目录";
                dlg.ShowNewFolderButton = false;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                if (!Program.IsMediaRoot(dlg.SelectedPath))
                {
                    MessageBox.Show(this, "该目录下没有 setup.exe 和 sources 文件夹。", Data.AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                AddAndSelect(dlg.SelectedPath);
            }
        }

        private void AddAndSelect(string path)
        {
            MediaSource m = null;
            foreach (MediaSource s in sources)
            {
                if (string.Equals(s.Path, path, StringComparison.OrdinalIgnoreCase)) { m = s; break; }
            }
            if (m == null)
            {
                m = new MediaSource(path, "手动选择  " + path);
                sources.Insert(0, m);
                RebuildSourceRows();
            }
            currentSource = m;
            foreach (AnimatedCard c in sourceCards)
            {
                c.Selected = c.Tag == m;
                c.Invalidate();
            }
            lblSourceHint.Text = "已选择：" + path;
            UpdateFooter();
            GoStep(1);
        }

        private void PickIso()
        {
            if (busy) return;
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "选择 Windows 安装 ISO";
                dlg.Filter = "ISO 镜像|*.iso|所有文件|*.*";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                MountIsoAsync(dlg.FileName);
            }
        }

        private void MountIsoAsync(string isoPath)
        {
            SetBusy(true);
            Log("正在挂载 ISO：" + isoPath);
            Task.Factory.StartNew(delegate
            {
                string drive;
                string error;
                Program.MountIso(isoPath, out drive, out error);
                return new string[] { drive, error };
            }).ContinueWith(delegate(Task<string[]> t)
            {
                if (IsDisposed) return;
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        SetBusy(false);
                        string drive = t.Result[0];
                        string error = t.Result[1];
                        if (string.IsNullOrEmpty(drive))
                        {
                            Log("挂载失败：" + (string.IsNullOrEmpty(error) ? "未知错误" : error));
                            MessageBox.Show(this, "ISO 挂载失败：\r\n" + error, Data.AppName,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        string root = drive + ":\\";
                        if (!Program.IsMediaRoot(root))
                        {
                            Log("已挂载到 " + drive + " 盘，但缺少 setup.exe 或 sources。");
                            MessageBox.Show(this, "镜像已挂载到 " + drive + " 盘，但里面没有安装文件。", Data.AppName,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        Log("挂载成功：" + root);
                        AddAndSelect(root);
                    });
                }
                catch
                {
                }
            });
        }

        private void PopulateEditions()
        {
            foreach (AnimatedCard c in allCards.ToArray())
            {
                if (c.Parent == editionList) allCards.Remove(c);
            }
            editionList.SuspendLayout();
            editionList.Controls.Clear();
            editionCards.Clear();
            customCard = null;
            string lastGroup = null;
            int y = 0;
            int w = editionList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4;

            foreach (Edition ed in Data.Editions)
            {
                if (ed.Group != lastGroup)
                {
                    lastGroup = ed.Group;
                    AnimatedCard h = new AnimatedCard();
                    h.IsHeader = true;
                    h.Text = lastGroup;
                    h.Height = Theme.S(32);
                    h.Cursor = Cursors.Default;
                    h.Left = 0;
                    h.Top = y;
                    h.Width = Math.Max(Theme.S(400), w);
                    editionList.Controls.Add(h);
                    y += Theme.S(36);
                }

                AnimatedCard c = new AnimatedCard();
                c.Text = ed.Index + ". " + ed.Name;
                c.SubText = ed.EditionId;
                c.RightText = ed.Key;
                c.Height = Theme.S(50);
                c.Tag = ed;
                c.Left = 0;
                c.Top = y;
                c.Width = Math.Max(Theme.S(400), w);
                c.Click += EditionCardClick;
                editionList.Controls.Add(c);
                editionCards.Add(c);
                allCards.Add(c);
                y += Theme.S(56);
            }

            y += Theme.S(8);
            AnimatedCard custom = new AnimatedCard();
            custom.Text = "28. 自定义版本（自己提供预安装密钥）";
            custom.SubText = "适用于列表里没有的版本，点击后填写密钥";
            custom.RightText = "自定义";
            custom.Height = Theme.S(56);
            custom.Left = 0;
            custom.Top = y;
            custom.Width = Math.Max(Theme.S(400), w);
            custom.Click += CustomCardClick;
            editionList.Controls.Add(custom);
            allCards.Add(custom);
            customCard = custom;

            editionList.ResumeLayout(false);
        }

        private void LayoutEditionRows()
        {
            int w = editionList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4;
            foreach (Control c in editionList.Controls)
            {
                c.Left = 0;
                c.Width = Math.Max(Theme.S(400), w);
            }
            editionList.PerformLayout();
        }

        private void EditionCardClick(object sender, EventArgs e)
        {
            AnimatedCard c = sender as AnimatedCard;
            if (c == null || busy) return;
            Edition ed = c.Tag as Edition;
            if (ed == null) return;
            currentEdition = ed;
            HighlightEdition();
            lblEditionHint.Text = "已选择 " + ed.Name + "，EditionID：" + ed.EditionId;
            UpdateFooter();
        }

        private void CustomCardClick(object sender, EventArgs e)
        {
            if (busy) return;
            using (CustomDialog dlg = new CustomDialog())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Result == null) return;
                currentEdition = dlg.Result;
                HighlightEdition();
                lblEditionHint.Text = "已选择自定义版本，密钥：" + dlg.Result.Key;
                UpdateFooter();
                GoStep(2);
            }
        }

        private void HighlightEdition()
        {
            foreach (AnimatedCard c in editionCards)
            {
                Edition ed = c.Tag as Edition;
                c.Selected = ed != null && currentEdition != null && ed.Index == currentEdition.Index;
                if (c.Selected) editionList.ScrollControlIntoView(c);
                c.Invalidate();
            }
            if (customCard != null)
            {
                customCard.Selected = currentEdition != null && currentEdition.Index == 28;
                customCard.Invalidate();
            }
        }

        private void ModeCardClick(object sender, EventArgs e)
        {
            ModeCard c = sender as ModeCard;
            if (c == null || busy) return;
            currentMode = c.Mode;
            RefreshModeCards();
            UpdateFooter();
        }

        private void RefreshModeCards()
        {
            foreach (ModeCard c in modeCards)
            {
                c.Selected = c.Mode == currentMode;
                c.Invalidate();
            }
        }

        private void RefreshTargetCard()
        {
            if (targetCard != null) targetCard.Invalidate();
        }

        private void PaintTargetCard(object sender, PaintEventArgs e)
        {
            Panel p = (Panel)sender;
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            Rectangle r = new Rectangle(0, 0, p.Width - 1, p.Height - Theme.S(3));
            Theme.FillRound(g, r, Theme.S(10), Theme.Card);
            Theme.StrokeRound(g, r, Theme.S(10), Theme.Line, 1f);

            TextRenderer.DrawText(g, "当前目标", Theme.Small, new Point(Theme.S(18), Theme.S(12)), Theme.Dim,
                TextFormatFlags.NoPrefix);
            string name = currentEdition == null ? "尚未选择版本" : currentEdition.Name;
            TextRenderer.DrawText(g, name, Theme.H2, new Point(Theme.S(18), Theme.S(32)), Theme.Text,
                TextFormatFlags.NoPrefix);

            if (currentEdition != null)
            {
                TextRenderer.DrawText(g, "EditionID  " +
                    (string.IsNullOrEmpty(currentEdition.EditionId) ? "未指定（强制升级不可用）" : currentEdition.EditionId),
                    Theme.MonoSmall, new Point(Theme.S(18), Theme.S(55)), Theme.Muted, TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, currentEdition.Key, Theme.Mono,
                    new Rectangle(r.Right - Theme.S(330), 0, Theme.S(312), r.Height), Theme.Accent2,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }

        private void StartRun()
        {
            if (busy || currentEdition == null) return;
            if (currentSource == null || !Program.IsMediaRoot(currentSource.Path))
            {
                MessageBox.Show(this, "当前安装源不可用，请重新选择。", Data.AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                GoStep(0);
                return;
            }

            Edition ed = currentEdition;

            if (currentMode == RunMode.Force && string.IsNullOrEmpty(ed.EditionId))
            {
                MessageBox.Show(this, "自定义版本做强制升级需要填写 EditionID，请回到版本列表重新填写。",
                    Data.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                GoStep(1);
                return;
            }

            if (currentMode == RunMode.KeyOnly)
            {
                if (MessageBox.Show(this, "将安装密钥：\r\n" + ed.Key + "\r\n\r\n只切换版本，不做就地升级，继续吗？",
                        Data.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                RunKeyChange(ed);
                return;
            }

            if (currentMode == RunMode.Force)
            {
                DialogResult r = MessageBox.Show(this,
                    "强制升级会改写注册表中的 ProductName、EditionID 和 CompositionEditionID，" +
                    "把系统伪装成目标版本。\r\n\r\n此方法不受微软支持，请先备份系统和重要数据。\r\n\r\n确定继续吗？",
                    "强制升级确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) return;
                if (!WriteRegistry(ed)) return;
            }

            string args = currentMode == RunMode.Normal
                ? Data.ArgsNormal
                : Data.ArgsForce + ed.Key;

            Log("启动安装程序：" + Path.Combine(currentSource.Path, "setup.exe"));
            Log("参数：" + args);
            if (Program.LaunchSetup(currentSource.Path, args))
            {
                Log("安装程序已启动，本窗口可以关闭。");
                MessageBox.Show(this, "安装程序已启动，接下来会由 Windows 安装程序接管。\r\n本窗口可以关闭。",
                    Data.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnStart.Enabled = false;
            }
        }

        private void RunKeyChange(Edition ed)
        {
            SetBusy(true);
            Log("安装密钥：" + ed.Key);
            Task.Factory.StartNew(delegate
            {
                string output;
                int code = Program.RunHidden("slmgr.vbs", "/ipk " + ed.Key, out output);
                return new object[] { code, output };
            }).ContinueWith(delegate(Task<object[]> t)
            {
                if (IsDisposed) return;
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        SetBusy(false);
                        int code = (int)t.Result[0];
                        string output = (string)t.Result[1];
                        Log(output.Replace("\r", "").Replace("\n", " ").Trim());
                        Log("slmgr 返回代码：" + code);
                    });
                }
                catch
                {
                }
            });
        }

        private bool WriteRegistry(Edition ed)
        {
            try
            {
                RegistryView[] views = { RegistryView.Registry64, RegistryView.Registry32 };
                foreach (RegistryView view in views)
                {
                    using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (RegistryKey k = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", true))
                    {
                        if (k == null) continue;
                        k.SetValue("EditionID", ed.EditionId, RegistryValueKind.String);
                        k.SetValue("ProductName", ed.ProductName, RegistryValueKind.String);
                        k.SetValue("CompositionEditionID",
                            string.IsNullOrEmpty(ed.Composition) ? ed.EditionId : ed.Composition,
                            RegistryValueKind.String);
                    }
                }
                Log("注册表已写入：" + ed.EditionId + " / " + ed.ProductName);
                return true;
            }
            catch (Exception ex)
            {
                Log("写入注册表失败：" + ex.Message);
                MessageBox.Show(this, "写入注册表失败：\r\n" + ex.Message, Data.AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void Log(string text)
        {
            if (logCard == null) return;
            logLines.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + text);
            while (logLines.Count > 100) logLines.RemoveAt(0);
            logCard.Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (busy) return;
            if (e.KeyCode == Keys.Enter)
            {
                if (step == 2) StartRun();
                else GoStep(step + 1);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
            else if (step == 2)
            {
                if (e.KeyCode == Keys.K) SelectMode(RunMode.KeyOnly);
                else if (e.KeyCode == Keys.S) SelectMode(RunMode.Normal);
                else if (e.KeyCode == Keys.U) SelectMode(RunMode.Pkey);
                else if (e.KeyCode == Keys.F) SelectMode(RunMode.Force);
            }
        }

        private void SelectMode(RunMode mode)
        {
            currentMode = mode;
            RefreshModeCards();
            UpdateFooter();
        }
    }
}
