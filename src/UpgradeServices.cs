using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace UpgradeTool;

internal sealed record ProcessResult(int ExitCode, string Output);

internal static class UpgradeServices
{
    private const string SettingsPath = @"Software\ZHCOOL\WindowsEditionUpgrade";
    private const string VersionPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

    public static bool IsAdministrator()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (SecurityException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    public static string ReadTheme()
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsPath);
            string theme = key?.GetValue("Theme") as string;
            return theme == "light" || theme == "dark" ? theme : "system";
        }
        catch (Exception ex) when (IsSettingsError(ex)) { return "system"; }
    }

    public static void SaveTheme(string theme)
    {
        if (theme != "system" && theme != "light" && theme != "dark")
            throw new ArgumentException("主题必须是 system、light 或 dark。", nameof(theme));
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(SettingsPath);
            key?.SetValue("Theme", theme, RegistryValueKind.String);
        }
        catch (Exception ex) when (IsSettingsError(ex)) { }
    }

    public static (int Width, int Height)? ReadWindowSize()
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsPath);
            if (key?.GetValue("WinW") is int width && key.GetValue("WinH") is int height &&
                ValidWindowSize(width, height))
                return (width, height);
        }
        catch (Exception ex) when (IsSettingsError(ex)) { }
        return null;
    }

    public static void SaveWindowSize(int width, int height)
    {
        if (!ValidWindowSize(width, height)) return;
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(SettingsPath);
            key?.SetValue("WinW", width, RegistryValueKind.DWord);
            key?.SetValue("WinH", height, RegistryValueKind.DWord);
        }
        catch (Exception ex) when (IsSettingsError(ex)) { }
    }

    private static bool ValidWindowSize(int width, int height) =>
        width >= 800 && height >= 600 && width <= 8000 && height <= 8000;

    private static bool IsSettingsError(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or SecurityException;

    public static string OsDescription()
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            using RegistryKey key = baseKey.OpenSubKey(VersionPath);
            if (key != null)
            {
                string name = key.GetValue("ProductName") as string;
                string version = key.GetValue("DisplayVersion") as string;
                string build = key.GetValue("CurrentBuild") as string;
                object ubr = key.GetValue("UBR");
                string arch = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
                return (name ?? "Windows") + " " + (version ?? "") + " Build " + (build ?? "?") +
                    (ubr != null ? "." + ubr : "") + " " + arch;
            }
        }
        catch (Exception ex) when (IsSettingsError(ex)) { }
        return Environment.OSVersion.VersionString;
    }

    public static string CurrentProductName()
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            using RegistryKey key = baseKey.OpenSubKey(VersionPath);
            if (key?.GetValue("ProductName") is string name && name.Length > 0) return name;
        }
        catch (Exception ex) when (IsSettingsError(ex)) { }
        return "Windows 10 Pro";
    }

    public static bool IsMediaRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            return File.Exists(Path.Combine(path, "setup.exe")) &&
                Directory.Exists(Path.Combine(path, "sources"));
        }
        catch (ArgumentException) { return false; }
        catch (NotSupportedException) { return false; }
        catch (Exception ex) when (IsSettingsError(ex)) { return false; }
    }

    public static List<MediaSource> DetectSources()
    {
        List<MediaSource> sources = new();
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        AddSource(sources, seen, Environment.CurrentDirectory);
        AddSource(sources, seen, AppContext.BaseDirectory);
        try
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                try
                {
                    if ((drive.DriveType == DriveType.Fixed || drive.DriveType == DriveType.Removable ||
                        drive.DriveType == DriveType.CDRom) && drive.IsReady)
                        AddSource(sources, seen, drive.RootDirectory.FullName);
                }
                catch (Exception ex) when (IsSettingsError(ex)) { }
            }
        }
        catch (Exception ex) when (IsSettingsError(ex)) { }
        return sources;
    }

    private static void AddSource(List<MediaSource> sources, HashSet<string> seen, string path)
    {
        if (!IsMediaRoot(path)) return;
        string full = Path.GetFullPath(path);
        if (!seen.Add(full.TrimEnd('\\', '/'))) return;
        string label;
        try
        {
            DriveInfo drive = new(Path.GetPathRoot(full));
            string letter = drive.Name.TrimEnd('\\');
            label = drive.DriveType switch
            {
                DriveType.CDRom => letter + "  ISO / 光盘",
                DriveType.Removable => letter + "  可移动磁盘",
                _ => letter + "  " + (string.IsNullOrEmpty(drive.VolumeLabel) ? "本地磁盘" : drive.VolumeLabel)
            };
        }
        catch (Exception ex) when (ex is ArgumentException || IsSettingsError(ex)) { label = full; }
        sources.Add(new MediaSource(full, label));
    }

    public static async Task<string> MountIsoAsync(string isoPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoPath);
        string full = Path.GetFullPath(isoPath);
        if (!File.Exists(full)) throw new FileNotFoundException("找不到该 ISO 文件。", full);
        string script =
            "$ErrorActionPreference='Stop';" +
            "[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false);" +
            "$OutputEncoding=[Console]::OutputEncoding;" +
            "try {" +
            "$path='" + full.Replace("'", "''") + "';" +
            "$img=Mount-DiskImage -ImagePath $path -PassThru;" +
            "for($i=0;$i -lt 30;$i++){" +
            "$volumes=@($img | Get-Volume -ErrorAction SilentlyContinue);" +
            "foreach($v in $volumes){if($v.DriveLetter){" +
            "$root=([string]$v.DriveLetter)+':\\';" +
            "if((Test-Path -LiteralPath ($root+'setup.exe') -PathType Leaf) -and " +
            "(Test-Path -LiteralPath ($root+'sources') -PathType Container)){" +
            "[Console]::Out.WriteLine('UPGRADE_MEDIA_ROOT='+$root);exit 0}}}" +
            "Start-Sleep -Milliseconds 300;" +
            "$img=Get-DiskImage -ImagePath $path;" +
            "};throw '镜像已挂载，但未获取到包含 setup.exe 和 sources 的盘符。'" +
            "}catch{[Console]::Error.WriteLine($_.Exception.Message);exit 1}";
        ProcessStartInfo start = CreateRedirectedStart(
            SystemFile(@"WindowsPowerShell\v1.0\powershell.exe"), Encoding.UTF8);
        start.ArgumentList.Add("-NoLogo");
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-NonInteractive");
        start.ArgumentList.Add("-EncodedCommand");
        start.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
        ProcessResult result = await RunProcessAsync(start, TimeSpan.FromSeconds(20), "ISO 挂载").ConfigureAwait(false);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"ISO 挂载失败（退出代码 {result.ExitCode}）：{result.Output}");
        foreach (Match match in Regex.Matches(result.Output, @"(?m)^UPGRADE_MEDIA_ROOT=([A-Za-z]:\\)\r?$"))
        {
            string root = match.Groups[1].Value;
            if (IsMediaRoot(root)) return root;
        }
        throw new InvalidOperationException("ISO 挂载进程已退出，但未返回有效的 Windows 安装介质根目录。" +
            (result.Output.Length == 0 ? "" : Environment.NewLine + result.Output));
    }

    public static Task<ProcessResult> InstallKeyAsync(Edition edition)
    {
        ArgumentNullException.ThrowIfNull(edition);
        ArgumentException.ThrowIfNullOrWhiteSpace(edition.Key);
        ProcessStartInfo start = CreateRedirectedStart(SystemFile("cscript.exe"), Encoding.Unicode);
        start.ArgumentList.Add("//Nologo");
        start.ArgumentList.Add("//U");
        start.ArgumentList.Add(SystemFile("slmgr.vbs"));
        start.ArgumentList.Add("/ipk");
        start.ArgumentList.Add(edition.Key);
        return RunProcessAsync(start, TimeSpan.FromSeconds(120), "密钥安装");
    }

    private static string SystemFile(string relativePath)
    {
        string system = Environment.GetFolderPath(Environment.SpecialFolder.System);
        if (string.IsNullOrEmpty(system)) throw new InvalidOperationException("无法定位 Windows 系统目录。");
        string path = Path.Combine(system, relativePath);
        if (!File.Exists(path)) throw new FileNotFoundException("缺少所需的 Windows 系统文件。", path);
        return path;
    }

    private static ProcessStartInfo CreateRedirectedStart(string file, Encoding encoding) => new()
    {
        FileName = file,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        StandardOutputEncoding = encoding,
        StandardErrorEncoding = encoding
    };

    private static async Task<ProcessResult> RunProcessAsync(ProcessStartInfo start, TimeSpan timeout, string operation)
    {
        using Process process = new() { StartInfo = start };
        using CancellationTokenSource deadline = new(timeout);
        try
        {
            if (!process.Start()) throw new InvalidOperationException("系统未创建进程。");
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            throw new InvalidOperationException(operation + "无法启动：" + ex.Message, ex);
        }

        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        Task<string[]> output = Task.WhenAll(stdout, stderr);
        _ = output.ContinueWith(task => { _ = task.Exception; }, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        try
        {
            await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
            string[] text = await output.WaitAsync(deadline.Token).ConfigureAwait(false);
            return new ProcessResult(process.ExitCode,
                (text[0].TrimEnd('\r', '\n') + Environment.NewLine + text[1]).Replace("\0", "").Trim());
        }
        catch (OperationCanceledException ex) when (deadline.IsCancellationRequested)
        {
            string cleanup = await StopProcessAsync(process, output).ConfigureAwait(false);
            throw new TimeoutException($"{operation}超过 {timeout.TotalSeconds:0} 秒，已请求终止进程树。{cleanup}", ex);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Win32Exception)
        {
            string cleanup = await StopProcessAsync(process, output).ConfigureAwait(false);
            throw new InvalidOperationException(operation + "执行或读取输出失败：" + ex.Message + cleanup, ex);
        }
    }

    private static async Task<string> StopProcessAsync(Process process, Task output)
    {
        string error = "";
        try { process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
        catch (Exception ex) when (ex is Win32Exception or NotSupportedException or AggregateException)
        {
            error = " 终止进程树失败：" + ex.Message;
        }
        using CancellationTokenSource cleanup = new(TimeSpan.FromSeconds(2));
        try
        {
            await process.WaitForExitAsync(cleanup.Token).ConfigureAwait(false);
            await output.WaitAsync(cleanup.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { error += " 等待退出或输出关闭超过 2 秒，已停止等待。"; }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Win32Exception)
        {
            error += " 清理进程输出失败：" + ex.Message;
        }
        return error;
    }

    public static void LaunchSetup(string root, string arguments)
    {
        if (!IsMediaRoot(root)) throw new InvalidOperationException("安装源不可用，缺少 setup.exe 或 sources 文件夹。");
        string full = Path.GetFullPath(root);
        ProcessStartInfo start = new()
        {
            FileName = Path.Combine(full, "setup.exe"),
            WorkingDirectory = full,
            UseShellExecute = false
        };
        foreach (string argument in arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            start.ArgumentList.Add(argument);
        try
        {
            using Process process = Process.Start(start);
            if (process == null) throw new InvalidOperationException("系统未创建安装进程。");
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            throw new InvalidOperationException("启动 Windows 安装程序失败：" + ex.Message, ex);
        }
    }

    public static void WriteEditionRegistry(Edition edition)
    {
        ArgumentNullException.ThrowIfNull(edition);
        ArgumentException.ThrowIfNullOrWhiteSpace(edition.EditionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(edition.ProductName);
        int written = 0;
        foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using RegistryKey key = baseKey.OpenSubKey(VersionPath, writable: true);
                if (key == null) continue;
                key.SetValue("EditionID", edition.EditionId, RegistryValueKind.String);
                key.SetValue("ProductName", edition.ProductName, RegistryValueKind.String);
                key.SetValue("CompositionEditionID", string.IsNullOrEmpty(edition.Composition)
                    ? edition.EditionId : edition.Composition, RegistryValueKind.String);
                written++;
            }
            catch (Exception ex) when (IsSettingsError(ex))
            {
                throw new InvalidOperationException($"写入系统版本注册表失败（{view}，已完成 {written} 个视图；可能已有部分字段写入）：{ex.Message}", ex);
            }
        }
        if (written == 0) throw new InvalidOperationException("未找到可写的系统版本注册表键，未写入任何版本信息。");
    }
}
