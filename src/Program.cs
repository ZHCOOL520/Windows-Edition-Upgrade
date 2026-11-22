using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace UpgradeTool
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--selftest")
            {
                RunSelfTest();
                return;
            }

            if (args.Length > 0 && args[0] == "--uitest")
            {
                RunUiTest();
                return;
            }

            if (!IsAdmin())
            {
                RelaunchElevated();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Theme.Init(ChooseLight(args), ChooseScale(args));
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e)
            {
                MessageBox.Show(e.Exception.Message, Data.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            Application.Run(new MainForm());
        }

        private static void RunSelfTest()
        {
            string report = Path.Combine(Path.GetTempPath(), "upgradetool_selftest.txt");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("editions=" + Data.Editions.Count);
            sb.AppendLine("version=" + Data.Version);
            foreach (Edition e in Data.Editions) sb.AppendLine(e.Index + "|" + e.Name + "|" + e.EditionId);
            List<MediaSource> found = DetectSources();
            sb.AppendLine("sources=" + found.Count);
            foreach (MediaSource m in found) sb.AppendLine("src|" + m.Path);
            File.WriteAllText(report, sb.ToString(), Encoding.UTF8);
        }

        private static void RunUiTest()
        {
            string report = Path.Combine(Path.GetTempPath(), "upgradetool_uitest.txt");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Theme.Init(ChooseLight(new string[0]), ChooseScale(new string[0]));
            try
            {
                using (MainForm f = new MainForm())
                {
                    Timer t = new Timer();
                    t.Interval = 1200;
                    t.Tick += delegate
                    {
                        t.Stop();
                        t.Dispose();
                        f.Close();
                    };
                    f.Shown += delegate { t.Start(); };
                    Application.Run(f);
                }
                File.WriteAllText(report, "ok", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                File.WriteAllText(report, "ERR " + ex, Encoding.UTF8);
            }
        }

        private const string ThemeRegPath = @"Software\ZHCOOL\WindowsEditionUpgrade";

        public static string ReadSavedTheme()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(ThemeRegPath))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("Theme");
                        if (v != null) return v.ToString();
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        public static void SaveTheme(bool light)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(ThemeRegPath))
                {
                    if (k != null)
                        k.SetValue("Theme", light ? "light" : "dark", RegistryValueKind.String);
                }
            }
            catch
            {
            }
        }

        private static float ChooseScale(string[] args)
        {
            foreach (string a in args)
            {
                if (a.StartsWith("--scale=", StringComparison.OrdinalIgnoreCase))
                {
                    float f;
                    if (float.TryParse(a.Substring(8), NumberStyles.Float, CultureInfo.InvariantCulture, out f)
                        && f >= 0.75f && f <= 3f) return f;
                }
            }
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    return g.DpiX / 96f;
                }
            }
            catch
            {
                return 1f;
            }
        }

        private static bool ChooseLight(string[] args)
        {
            bool light;
            string saved = ReadSavedTheme();
            if (saved == "light") light = true;
            else if (saved == "dark") light = false;
            else light = IsLightTheme();
            foreach (string a in args)
            {
                if (string.Equals(a, "--light", StringComparison.OrdinalIgnoreCase)) light = true;
                else if (string.Equals(a, "--dark", StringComparison.OrdinalIgnoreCase)) light = false;
            }
            return light;
        }

        public static bool IsLightTheme()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("AppsUseLightTheme");
                        if (v is int) return (int)v != 0;
                    }
                }
            }
            catch
            {
            }
            return true;
        }

        public static string CurrentProductName()
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("ProductName");
                        if (v != null && v.ToString().Length > 0) return v.ToString();
                    }
                }
            }
            catch
            {
            }
            return "Windows 10 Pro";
        }

        public static bool IsAdmin()
        {
            try
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                WindowsPrincipal p = new WindowsPrincipal(id);
                return p.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static void RelaunchElevated()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = Application.ExecutablePath;
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                Process.Start(psi);
            }
            catch
            {
                MessageBox.Show("需要管理员权限才能运行本工具。", Data.AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public static bool IsMediaRoot(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                return File.Exists(Path.Combine(path, "setup.exe")) &&
                       Directory.Exists(Path.Combine(path, "sources"));
            }
            catch
            {
                return false;
            }
        }

        public static List<MediaSource> DetectSources()
        {
            List<MediaSource> list = new List<MediaSource>();
            List<string> seen = new List<string>();

            AddSource(list, seen, Environment.CurrentDirectory);
            AddSource(list, seen, AppDomain.CurrentDomain.BaseDirectory);

            try
            {
                foreach (DriveInfo d in DriveInfo.GetDrives())
                {
                    if (d.DriveType != DriveType.Fixed && d.DriveType != DriveType.Removable &&
                        d.DriveType != DriveType.CDRom) continue;
                    try
                    {
                        if (!d.IsReady) continue;
                    }
                    catch { continue; }
                    AddSource(list, seen, d.RootDirectory.FullName);
                }
            }
            catch
            {
            }
            return list;
        }

        private static void AddSource(List<MediaSource> list, List<string> seen, string path)
        {
            if (!IsMediaRoot(path)) return;
            string full;
            try
            {
                full = Path.GetFullPath(path);
            }
            catch
            {
                return;
            }
            string norm = full.TrimEnd('\\').ToLowerInvariant();
            foreach (string s in seen)
            {
                if (s == norm) return;
            }
            seen.Add(norm);

            string label;
            try
            {
                DriveInfo d = new DriveInfo(Path.GetPathRoot(full));
                string letter = d.Name.TrimEnd('\\');
                label = letter + "  " + (string.IsNullOrEmpty(d.VolumeLabel) ? "本地磁盘" : d.VolumeLabel);
                if (d.DriveType == DriveType.CDRom) label = letter + "  ISO / 光盘";
                if (d.DriveType == DriveType.Removable) label = letter + "  可移动磁盘";
            }
            catch
            {
                label = full;
            }
            list.Add(new MediaSource(full, label));
        }

        public static void MountIso(string isoPath, out string driveLetter, out string error)
        {
            driveLetter = null;
            error = null;
            if (!File.Exists(isoPath))
            {
                error = "找不到该 ISO 文件。";
                return;
            }

            string script =
                "$ErrorActionPreference='Stop';" +
                "$img = Mount-DiskImage -ImagePath '" + isoPath.Replace("'", "''") + "' -PassThru;" +
                "Start-Sleep -Milliseconds 800;" +
                "$v = $img | Get-Volume; if ($v) { $v.DriveLetter }";

            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "powershell.exe";
            psi.Arguments = "-NoProfile -NonInteractive -EncodedCommand " + encoded;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            try
            {
                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    string err = p.StandardError.ReadToEnd();
                    p.WaitForExit(20000);

                    Match m = Regex.Match(output, @"(?m)^\s*([A-Z])\s*$");
                    if (m.Success)
                    {
                        driveLetter = m.Groups[1].Value;
                        return;
                    }
                    error = string.IsNullOrEmpty(err) ? "未能获取挂载后的盘符。" : err.Trim();
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
        }

        public static int RunHidden(string file, string arguments, out string output)
        {
            output = "";
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = file;
            psi.Arguments = arguments;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            try
            {
                psi.StandardOutputEncoding = Encoding.GetEncoding(936);
                psi.StandardErrorEncoding = Encoding.GetEncoding(936);
            }
            catch
            {
            }
            try
            {
                using (Process p = Process.Start(psi))
                {
                    string o = p.StandardOutput.ReadToEnd();
                    string e = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    output = (o + e).Trim();
                    return p.ExitCode;
                }
            }
            catch (Exception ex)
            {
                output = ex.Message;
                return -1;
            }
        }

        public static bool LaunchSetup(string sourceRoot, string arguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = Path.Combine(sourceRoot, "setup.exe");
                psi.Arguments = arguments;
                psi.WorkingDirectory = sourceRoot;
                psi.UseShellExecute = true;
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动安装程序失败：" + ex.Message, Data.AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
