using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
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
            sourceCard.ClearRows();
            sourceCards.Clear();

            if (sources.Count == 0)
            {
                sourceCard.EmptyText = "未找到安装介质。可以把脚本放到安装介质根目录，或选择已挂载的 ISO / 解压后的文件夹。";
                sourceCard.Finish(0);
                return;
            }

            sourceCard.EmptyText = "";
            int y = 0;
            for (int i = 0; i < sources.Count; i++)
            {
                MediaSource m = sources[i];
                AnimatedCard c = new AnimatedCard();
                c.Text = m.Label;
                c.SubText = m.Path;
                c.RightText = "可用";
                c.Height = Theme.S(56);
                c.Tag = m;
                c.Click += SourceCardClick;
                sourceCard.AddRow(c, y);
                sourceCards.Add(c);
                y += c.Height + Theme.S(6);
            }
            sourceCard.Finish(y);
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
            editionCard.ClearRows();
            editionCards.Clear();
            customCard = null;

            int y = 0;
            string lastGroup = null;
            foreach (Edition ed in Data.Editions)
            {
                if (ed.Group != lastGroup)
                {
                    lastGroup = ed.Group;
                    AnimatedCard h = new AnimatedCard();
                    h.IsHeader = true;
                    h.Text = lastGroup;
                    h.Height = Theme.S(30);
                    h.Cursor = Cursors.Default;
                    editionCard.AddRow(h, y);
                    y += Theme.S(34);
                }

                AnimatedCard c = new AnimatedCard();
                c.Text = ed.Index + ". " + ed.Name;
                c.SubText = ed.EditionId;
                c.RightText = ed.Key;
                c.Height = Theme.S(48);
                c.Tag = ed;
                c.Click += EditionCardClick;
                editionCard.AddRow(c, y);
                editionCards.Add(c);
                y += Theme.S(54);
            }

            y += Theme.S(6);
            AnimatedCard custom = new AnimatedCard();
            custom.Text = "28. 自定义版本（自己提供预安装密钥）";
            custom.SubText = "适用于列表里没有的版本，点击后填写密钥";
            custom.RightText = "自定义";
            custom.Height = Theme.S(52);
            custom.Click += CustomCardClick;
            editionCard.AddRow(custom, y);
            customCard = custom;
            y += custom.Height + Theme.S(6);

            editionCard.Finish(y);
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
                if (c.Selected) editionCard.EnsureVisible(c);
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

        private void PaintLogCard(object sender, PaintEventArgs e)
        {
            Panel p = (Panel)sender;
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            Rectangle r = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
            Theme.FillRound(g, r, Theme.S(8), Theme.Sunken);
            Theme.StrokeRound(g, r, Theme.S(8), Theme.CardStroke, 1f);
            TextRenderer.DrawText(g, "运行日志", Theme.Caption, new Point(Theme.S(16), Theme.S(11)), Theme.Dim,
                TextFormatFlags.NoPrefix);

            if (logLines.Count == 0)
            {
                TextRenderer.DrawText(g, "选择执行方式后点「开始执行」，过程信息会记录在这里。",
                    Theme.MonoSmall, new Point(Theme.S(16), Theme.S(36)), Theme.Dim, TextFormatFlags.NoPrefix);
                return;
            }

            int lineH = Theme.S(17);
            int maxLines = Math.Max(1, (r.Height - Theme.S(46)) / lineH);
            int shown = Math.Min(maxLines, logLines.Count);
            int startY = Theme.S(36);
            for (int i = 0; i < shown; i++)
            {
                string line = logLines[logLines.Count - shown + i];
                bool newest = i == shown - 1;
                TextRenderer.DrawText(g, line, Theme.MonoSmall,
                    new Rectangle(Theme.S(16), startY + i * lineH, r.Width - Theme.S(32), lineH),
                    newest ? Theme.Text : Theme.Muted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        private void PaintTargetCard(object sender, PaintEventArgs e)
        {
            Panel p = (Panel)sender;
            Graphics g = e.Graphics;
            Theme.EnableSmooth(g);
            Rectangle r = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
            Theme.FillRound(g, r, Theme.S(8), Theme.AccentSoft);
            Theme.StrokeRound(g, r, Theme.S(8), Color.FromArgb(Theme.Light ? 90 : 120, Theme.Accent), 1f);

            TextRenderer.DrawText(g, "当前目标", Theme.Caption,
                new Point(Theme.S(16), Theme.S(8)), Theme.Muted, TextFormatFlags.NoPrefix);
            string name = currentEdition == null ? "尚未选择版本" : currentEdition.Name;
            TextRenderer.DrawText(g, name, Theme.H2,
                new Point(Theme.S(16), Theme.S(24)), Theme.Text, TextFormatFlags.NoPrefix);

            if (currentEdition != null)
            {
                TextRenderer.DrawText(g, "EditionID  " +
                    (string.IsNullOrEmpty(currentEdition.EditionId) ? "未指定（强制升级不可用）" : currentEdition.EditionId),
                    Theme.MonoSmall, new Point(Theme.S(16), Theme.S(47)), Theme.Muted, TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, currentEdition.Key, Theme.Mono,
                    new Rectangle(r.Right - Theme.S(330), 0, Theme.S(314), r.Height), Theme.Accent,
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

        private void ExportLog()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Data.AppName + " " + Data.Version + "  运行日志");
            sb.AppendLine("导出时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("系统：" + OsDescription());
            sb.AppendLine("安装源：" + (currentSource == null ? "未选择" : currentSource.Path));
            sb.AppendLine("目标版本：" + (currentEdition == null
                ? "未选择"
                : currentEdition.Name + "（" +
                  (string.IsNullOrEmpty(currentEdition.EditionId) ? "EditionID 未指定" : currentEdition.EditionId) + "）"));
            sb.AppendLine("执行方式：" + ModeName(currentMode));
            sb.AppendLine(new string('-', 48));
            if (logLines.Count == 0) sb.AppendLine("（暂无运行日志）");
            else
            {
                foreach (string line in logLines) sb.AppendLine(line);
            }

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "导出运行日志";
                dlg.Filter = "文本文件|*.txt|所有文件|*.*";
                dlg.FileName = "无损跨版本升级Windows-日志-" +
                               DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
                    Log("日志已导出：" + dlg.FileName);
                    MessageBox.Show(this, "日志已保存：\r\n" + dlg.FileName, Data.AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Log("导出日志失败：" + ex.Message);
                    MessageBox.Show(this, "导出失败：\r\n" + ex.Message, Data.AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string OsDescription()
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (k != null)
                    {
                        string name = k.GetValue("ProductName") as string;
                        string ver = k.GetValue("DisplayVersion") as string;
                        string build = k.GetValue("CurrentBuild") as string;
                        object ubr = k.GetValue("UBR");
                        string arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                        return (name ?? "Windows") + " " + (ver ?? "") + " Build " + (build ?? "?") +
                               (ubr != null ? "." + ubr : "") + " " + arch;
                    }
                }
            }
            catch
            {
            }
            return Environment.OSVersion.VersionString;
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
