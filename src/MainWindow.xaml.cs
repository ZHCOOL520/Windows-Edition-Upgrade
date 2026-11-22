using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using Windows.System;

namespace UpgradeTool;

public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<SourceItem> sources = new();
    private readonly List<string> logLines = new();
    private Edition currentEdition;
    private SourceItem currentSource;
    private RunMode currentMode = RunMode.Normal;
    private int step;
    private bool ready;
    private bool busy;
    private bool dialogOpen;
    private bool setupStarted;

    public MainWindow()
    {
        InitializeComponent();
        Title = Data.AppName;
        var savedSize = UpgradeServices.ReadWindowSize();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(savedSize?.Width ?? 1120, savedSize?.Height ?? 820));
        string iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(iconPath)) AppWindow.SetIcon(iconPath);
        AppWindow.Closing += Window_Closing;
        if (MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop();
            Root.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }
        SourceList.ItemsSource = sources;
        EditionGroups.Source = Data.Editions.Select(e => new EditionItem(e)).GroupBy(e => e.Group).ToList();
        Root.ActualThemeChanged += Root_ActualThemeChanged;
        ApplyTheme(UpgradeServices.ReadTheme());
        Navigation.SelectedItem = SourceNav;
        ready = true;
        ShowStep(0);
    }

    private async void Root_Loaded(object sender, RoutedEventArgs e)
    {
        Root.Loaded -= Root_Loaded;
        if (!UpgradeServices.IsAdministrator())
        {
            SetNotice("需要管理员权限", "请以管理员身份重新启动本工具。", InfoBarSeverity.Error);
            NextButton.IsEnabled = false;
            return;
        }
        await ScanSourcesAsync();
    }

    private void ApplyTheme(string theme)
    {
        Root.RequestedTheme = theme == "light" ? ElementTheme.Light :
            theme == "dark" ? ElementTheme.Dark : ElementTheme.Default;
        UpdateThemeButton();
    }

    private void Root_ActualThemeChanged(FrameworkElement sender, object args) => UpdateThemeButton();

    private void UpdateThemeButton()
    {
        bool isDark = Root.ActualTheme == ElementTheme.Dark;
        AppWindow.TitleBar.PreferredTheme = isDark ? TitleBarTheme.Dark : TitleBarTheme.Light;
        ThemeNavIcon.Glyph = isDark ? "\uE708" : "\uE706";
        string action = isDark ? "切换到浅色主题" : "切换到深色主题";
        ThemeNav.Content = action;
        ToolTipService.SetToolTip(ThemeNav, action);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(ThemeNav, action);
    }

    private void ToggleTheme()
    {
        string theme = Root.ActualTheme == ElementTheme.Dark ? "light" : "dark";
        ApplyTheme(theme);
        UpgradeServices.SaveTheme(theme);
    }

    private void Window_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (busy || dialogOpen)
        {
            args.Cancel = true;
            StatusText.Text = "请先完成当前操作或关闭对话框。";
            return;
        }
        if (AppWindow.Presenter is OverlappedPresenter presenter && presenter.State == OverlappedPresenterState.Restored)
            UpgradeServices.SaveWindowSize(AppWindow.Size.Width, AppWindow.Size.Height);
    }

    private void Navigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (!ready || args.SelectedItem is not NavigationViewItem item) return;
        if (int.TryParse(item.Tag?.ToString(), out int target)) ShowStep(target);
    }

    private async void Navigation_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (busy || dialogOpen) return;
        string tag = args.InvokedItemContainer?.Tag?.ToString();
        if (tag == "about") await ShowAboutAsync();
        else if (tag == "theme") ToggleTheme();
    }

    private void ShowStep(int target)
    {
        if (!ready || busy || dialogOpen) return;
        step = Math.Clamp(target, 0, 2);
        SourcePage.Visibility = step == 0 ? Visibility.Visible : Visibility.Collapsed;
        EditionPage.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        RunPage.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
        StepLabel.Text = $"步骤 {step + 1} / 3";
        PageTitle.Text = new[] { "选择安装源", "选择目标版本", "选择执行方式" }[step];
        PageDescription.Text = new[]
        {
            "选择 Windows 安装介质，或挂载 ISO 镜像。仅更换密钥可跳过这一步。",
            "选择所需的 Windows 版本。普通升级可跳过这一步。",
            "核对本次操作，再启动 Windows 安装程序或更换密钥。"
        }[step];
        Navigation.SelectedItem = new[] { SourceNav, EditionNav, RunNav }[step];
        PageScroll.ChangeView(null, 0, null, true);
        UpdateState();
    }

    private void SetBusy(bool value, string status = null)
    {
        busy = value;
        BusyRing.IsActive = value;
        BusyRing.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        SourceNav.IsEnabled = EditionNav.IsEnabled = RunNav.IsEnabled = !value;
        FolderButton.IsEnabled = IsoButton.IsEnabled = RefreshButton.IsEnabled = !value;
        SourceList.IsEnabled = EditionList.IsEnabled = Modes.IsEnabled = !value;
        CustomButton.IsEnabled = ClearEditionButton.IsEnabled = !value;
        ThemeNav.IsEnabled = ExportButton.IsEnabled = !value;
        BackButton.IsEnabled = !value;
        UpdateState();
        if (status != null) StatusText.Text = status;
    }

    private string ValidationMessage()
    {
        if (!UpgradeServices.IsAdministrator()) return "需要管理员权限。";
        if (currentMode != RunMode.KeyOnly && (currentSource == null || !UpgradeServices.IsMediaRoot(currentSource.Path)))
            return "请先选择有效的安装源。";
        if (currentMode != RunMode.Normal && currentEdition == null) return "请先选择目标版本。";
        if (currentMode == RunMode.Force && string.IsNullOrWhiteSpace(currentEdition.EditionId))
            return "强制升级需要填写 EditionID，请编辑自定义版本。";
        return null;
    }

    private static string ModeName(RunMode mode) => mode switch
    {
        RunMode.KeyOnly => "仅更换密钥",
        RunMode.Normal => "普通升级",
        RunMode.Pkey => "指定版本升级",
        _ => "强制升级"
    };

    private void UpdateState()
    {
        if (!ready) return;
        string source = currentSource?.Path ?? "未选择";
        string edition = currentMode == RunMode.Normal ? "由 Windows 安装程序判断" : currentEdition?.Name ?? "未选择";
        SummaryText.Text = $"安装源：{(currentMode == RunMode.KeyOnly ? "无需安装源" : source)}\n目标版本：{edition}\n执行方式：{ModeName(currentMode)}";
        BackButton.Visibility = step > 0 ? Visibility.Visible : Visibility.Collapsed;
        NextButton.Content = step == 2 ? "开始执行" : "下一步";
        NextButton.IsEnabled = !busy && !setupStarted && UpgradeServices.IsAdministrator();
        string validation = ValidationMessage();
        if (!busy) StatusText.Text = setupStarted ? "安装程序已启动" : step == 2 ? validation ?? "准备就绪" : "准备就绪";
        ModeHint.Severity = currentMode == RunMode.Force ? InfoBarSeverity.Warning : InfoBarSeverity.Informational;
        ModeHint.Title = currentMode == RunMode.Force ? "此操作会修改系统版本注册表" : ModeName(currentMode);
        ModeHint.Message = currentMode switch
        {
            RunMode.KeyOnly => "只安装密钥，不执行就地升级；预安装密钥不能激活系统。",
            RunMode.Normal => "忽略目标版本选择，使用所选介质直接启动普通就地升级。",
            RunMode.Pkey => "安装介质需要包含目标版本，或支持从其基础版本生成目标版本。",
            _ => "该方法不受微软支持。请备份系统和重要数据，准备恢复介质；启动前还会再次确认。"
        };
    }

    private void SetNotice(string title, string message, InfoBarSeverity severity)
    {
        Notice.Title = title;
        Notice.Message = message;
        Notice.Severity = severity;
        Notice.IsOpen = true;
    }

    private void Log(string text)
    {
        logLines.Add($"{DateTime.Now:HH:mm:ss}  {text}");
        if (logLines.Count > 500) logLines.RemoveRange(0, logLines.Count - 500);
        LogText.Text = string.Join(Environment.NewLine, logLines);
        LogText.Select(LogText.Text.Length, 0);
    }

    private async Task ScanSourcesAsync()
    {
        if (busy || dialogOpen) return;
        SetBusy(true, "正在扫描安装介质…");
        try
        {
            string selectedPath = currentSource?.Path;
            var found = await Task.Run(UpgradeServices.DetectSources);
            // Manually selected subfolders are not rediscovered by a drive-root scan.
            if (selectedPath != null && UpgradeServices.IsMediaRoot(selectedPath) &&
                !found.Any(s => string.Equals(s.Path, selectedPath, StringComparison.OrdinalIgnoreCase)))
                found.Insert(0, new MediaSource(selectedPath, currentSource.Label));
            sources.Clear();
            foreach (var media in found) sources.Add(new SourceItem(media));
            SourceList.SelectedItem = sources.FirstOrDefault(s => string.Equals(s.Path, selectedPath, StringComparison.OrdinalIgnoreCase))
                ?? sources.FirstOrDefault();
            currentSource = SourceList.SelectedItem as SourceItem;
            SourceHint.Text = sources.Count == 0 ? "没有找到安装介质。请选择文件夹或 ISO 镜像。" : $"找到 {sources.Count} 个安装源，点击列表项即可选择。";
            Log($"扫描完成：{sources.Count} 个可用安装源。");
        }
        catch (Exception ex)
        {
            ReportError("扫描失败", ex);
        }
        finally { SetBusy(false); }
    }

    private void AddSource(string path)
    {
        if (!UpgradeServices.IsMediaRoot(path)) throw new InvalidOperationException("该目录缺少 setup.exe 或 sources 文件夹。");
        string full = Path.GetFullPath(path);
        var source = sources.FirstOrDefault(s => string.Equals(s.Path.TrimEnd('\\'), full.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
        if (source == null)
        {
            source = new SourceItem(new MediaSource(full, "手动选择的安装介质"));
            sources.Insert(0, source);
        }
        currentSource = source;
        SourceList.SelectedItem = source;
        SourceHint.Text = "已选择：" + full;
        Log("安装源：" + full);
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await ScanSourcesAsync();

    private async void Folder_Click(object sender, RoutedEventArgs e)
    {
        if (busy || dialogOpen) return;
        SetBusy(true, "请选择安装介质目录…");
        try
        {
            var picker = new FolderPicker(AppWindow.Id) { CommitButtonText = "选择安装目录" };
            var result = await picker.PickSingleFolderAsync();
            if (result != null) AddSource(result.Path);
        }
        catch (Exception ex) { ReportError("选择文件夹失败", ex); }
        finally { SetBusy(false); }
    }

    private async void Iso_Click(object sender, RoutedEventArgs e)
    {
        if (busy || dialogOpen) return;
        SetBusy(true, "请选择 ISO 镜像…");
        try
        {
            var picker = new FileOpenPicker(AppWindow.Id) { FileTypeFilter = { ".iso" } };
            var result = await picker.PickSingleFileAsync();
            if (result == null) return;
            StatusText.Text = "正在挂载 ISO…";
            Log("正在挂载 ISO：" + result.Path);
            string root = await UpgradeServices.MountIsoAsync(result.Path);
            AddSource(root);
            SetNotice("ISO 已挂载", root, InfoBarSeverity.Success);
        }
        catch (Exception ex) { ReportError("挂载失败", ex); }
        finally { SetBusy(false); }
    }

    private void SourceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        currentSource = SourceList.SelectedItem as SourceItem;
        if (currentSource != null) SourceHint.Text = "已选择：" + currentSource.Path;
        UpdateState();
    }

    private void EditionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EditionList.SelectedItem is EditionItem item) currentEdition = item.Edition;
        else if (currentEdition?.Index != 28) currentEdition = null;
        UpdateEditionDetail();
    }

    private void UpdateEditionDetail()
    {
        EditionDetail.Text = currentEdition == null ? "尚未选择版本；普通升级可直接继续。" :
            $"{currentEdition.Name}\nEditionID：{(string.IsNullOrWhiteSpace(currentEdition.EditionId) ? "未指定" : currentEdition.EditionId)}\n预安装密钥：{currentEdition.Key}";
        UpdateState();
    }

    private void ClearEdition_Click(object sender, RoutedEventArgs e)
    {
        currentEdition = null;
        EditionList.SelectedItem = null;
        UpdateEditionDetail();
    }

    private async void Custom_Click(object sender, RoutedEventArgs e)
    {
        if (busy || dialogOpen) return;
        var custom = currentEdition?.Index == 28 ? currentEdition : null;
        var key = new TextBox { Header = "25 位预安装密钥", Text = custom?.Key ?? "", PlaceholderText = "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX", MaxLength = 29 };
        var id = new TextBox { Header = "EditionID（强制升级必填）", Text = custom?.EditionId ?? "", PlaceholderText = "例如 Professional", MaxLength = 100 };
        var product = new TextBox { Header = "ProductName（可选）", Text = custom?.ProductName ?? "", PlaceholderText = "留空使用当前系统的 ProductName", MaxLength = 200 };
        var error = new InfoBar { Severity = InfoBarSeverity.Error, IsClosable = false, IsOpen = false };
        var panel = new StackPanel { Spacing = 16 };
        panel.Children.Add(new TextBlock { Text = "密钥仅用于选择版本，激活仍需正版授权。", TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(key);
        panel.Children.Add(id);
        panel.Children.Add(product);
        panel.Children.Add(error);
        var dialog = CreateDialog("自定义版本", new ScrollViewer { Content = panel, MaxHeight = 480 }, "使用此版本");
        dialog.PrimaryButtonClick += (_, args) =>
        {
            string normalized = Regex.Replace(key.Text, @"[\s-]", "").ToUpperInvariant();
            if (!Regex.IsMatch(normalized, "^[A-Z0-9]{25}$"))
            {
                args.Cancel = true;
                error.Message = "请输入 25 位字母或数字的预安装密钥。";
                error.IsOpen = true;
                key.Focus(FocusState.Programmatic);
            }
            else if (id.Text.Trim().Length > 0 && !Regex.IsMatch(id.Text.Trim(), "^[A-Za-z0-9]+$"))
            {
                args.Cancel = true;
                error.Message = "EditionID 只能包含英文字母和数字。";
                error.IsOpen = true;
                id.Focus(FocusState.Programmatic);
            }
        };
        try
        {
            if (await ShowDialogAsync(dialog) != ContentDialogResult.Primary) return;
            string normalized = Regex.Replace(key.Text, @"[\s-]", "").ToUpperInvariant();
            string formatted = string.Join("-", Enumerable.Range(0, 5).Select(i => normalized.Substring(i * 5, 5)));
            string editionId = id.Text.Trim();
            currentEdition = new Edition(28, "自定义版本", formatted, editionId,
                string.IsNullOrWhiteSpace(product.Text) ? UpgradeServices.CurrentProductName() : product.Text.Trim(), editionId, "自定义");
            EditionList.SelectedItem = null;
            UpdateEditionDetail();
        }
        catch (Exception ex) { ReportError("自定义版本失败", ex); }
    }

    private void Modes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready) return;
        if (Modes.SelectedItem is RadioButton item && Enum.TryParse(item.Tag?.ToString(), out RunMode mode))
            currentMode = mode;
        UpdateState();
    }

    private void Back_Click(object sender, RoutedEventArgs e) => ShowStep(step - 1);

    private async void Next_Click(object sender, RoutedEventArgs e)
    {
        if (busy || dialogOpen) return;
        if (step < 2) ShowStep(step + 1);
        else await StartRunAsync();
    }

    private async Task StartRunAsync()
    {
        if (busy || dialogOpen || setupStarted) return;
        string validation = ValidationMessage();
        if (validation != null)
        {
            SetNotice("尚未准备好", validation, InfoBarSeverity.Warning);
            return;
        }
        var edition = currentEdition;
        var mode = currentMode;
        string source = currentSource?.Path;
        SetBusy(true, "等待确认…");
        try
        {
            if (mode == RunMode.KeyOnly)
            {
                var confirm = CreateDialog("安装所选密钥？", new TextBlock
                {
                    Text = $"目标：{edition.Name}\n密钥：{edition.Key}\n\n仅更换密钥，不执行就地升级。预安装密钥不能激活系统。",
                    TextWrapping = TextWrapping.Wrap
                }, "安装密钥");
                if (await ShowDialogAsync(confirm) != ContentDialogResult.Primary) return;
                StatusText.Text = "正在安装密钥…";
                Log("开始安装所选版本的密钥：" + edition.Name);
                var result = await UpgradeServices.InstallKeyAsync(edition);
                Log(result.Output);
                Log("slmgr 返回代码：" + result.ExitCode);
                SetNotice(result.ExitCode == 0 ? "命令已完成" : "密钥安装失败", "请查看运行日志中的 Windows 返回结果。",
                    result.ExitCode == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Error);
                return;
            }

            string confirmation = mode == RunMode.Force
                ? $"将把 ProductName、EditionID 和 CompositionEditionID 改写为目标版本：{edition.Name}（{edition.EditionId}），然后启动安装程序。\n\n此方法不受微软支持。注册表修改不会自动恢复；安装取消或失败时修改仍会保留。请先备份系统和重要数据并准备恢复介质。"
                : $"安装源：{source}\n目标：{(mode == RunMode.Normal ? "由安装程序判断" : edition.Name)}\n\n将启动 Windows 安装程序，请在安装程序内核对保留个人文件和应用的选项。";
            var dialog = CreateDialog(mode == RunMode.Force ? "确认强制升级" : "启动 Windows 安装程序？",
                new TextBlock { Text = confirmation, TextWrapping = TextWrapping.Wrap }, mode == RunMode.Force ? "确认修改并启动" : "启动安装程序");
            if (await ShowDialogAsync(dialog) != ContentDialogResult.Primary) return;
            if (!UpgradeServices.IsMediaRoot(source)) throw new InvalidOperationException("安装源已不可用，请重新选择。");
            StatusText.Text = "正在启动安装程序…";
            if (mode == RunMode.Force)
            {
                UpgradeServices.WriteEditionRegistry(edition);
                Log("已修改系统版本注册表：" + edition.EditionId + " / " + edition.ProductName);
            }
            string arguments = mode == RunMode.Normal ? Data.ArgsNormal : Data.ArgsForce + edition.Key;
            Log("启动安装程序：" + Path.Combine(source, "setup.exe"));
            UpgradeServices.LaunchSetup(source, arguments);
            setupStarted = true;
            Log("安装程序已启动，后续由 Windows 安装程序接管。本窗口可以关闭。");
            SetNotice("安装程序已启动", "请在 Windows 安装程序中继续。本窗口可以关闭。", InfoBarSeverity.Success);
        }
        catch (Exception ex) { ReportError("执行失败", ex); }
        finally { SetBusy(false); }
    }

    private ContentDialog CreateDialog(string title, object content, string primary = null) => new()
    {
        XamlRoot = Root.XamlRoot,
        RequestedTheme = Root.RequestedTheme,
        Title = title,
        Content = content,
        PrimaryButtonText = primary ?? "",
        CloseButtonText = primary == null ? "关闭" : "取消",
        DefaultButton = ContentDialogButton.Close
    };

    private async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog)
    {
        if (dialogOpen) return ContentDialogResult.None;
        dialogOpen = true;
        try { return await dialog.ShowAsync(); }
        finally { dialogOpen = false; }
    }

    private async Task ShowAboutAsync()
    {
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = Data.AppName + " " + Data.Version, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Text = "原生 WinUI 3 界面 · Windows 版本切换与就地升级工具", TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new HyperlinkButton { Content = "作者网站", NavigateUri = new Uri(Data.Website) });
        panel.Children.Add(new HyperlinkButton { Content = "GitHub 问题反馈", NavigateUri = new Uri(Data.RepoUrl + "/issues") });
        panel.Children.Add(new TextBlock { Text = "QQ 邮箱：" + Data.QqMail, IsTextSelectionEnabled = true, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Text = UpgradeServices.OsDescription(), TextWrapping = TextWrapping.Wrap });
        try { await ShowDialogAsync(CreateDialog("关于", panel)); }
        catch (Exception ex) { ReportError("无法打开关于窗口", ex); }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (busy || dialogOpen) return;
        SetBusy(true, "请选择日志保存位置…");
        try
        {
            var picker = new FileSavePicker(AppWindow.Id)
            {
                SuggestedFileName = "无损跨版本升级Windows-日志-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                DefaultFileExtension = ".txt",
                FileTypeChoices = { { "文本文件", new List<string> { ".txt" } } }
            };
            var result = await picker.PickSaveFileAsync();
            if (result == null) return;
            var text = new StringBuilder();
            text.AppendLine(Data.AppName + " " + Data.Version + " 运行日志");
            text.AppendLine("导出时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            text.AppendLine("系统：" + UpgradeServices.OsDescription());
            text.AppendLine(SummaryText.Text);
            text.AppendLine(new string('-', 48));
            foreach (string line in logLines) text.AppendLine(line);
            await File.WriteAllTextAsync(result.Path, text.ToString(), new UTF8Encoding(true));
            Log("日志已导出：" + result.Path);
            SetNotice("日志已保存", result.Path, InfoBarSeverity.Success);
        }
        catch (Exception ex) { ReportError("日志导出失败", ex); }
        finally { SetBusy(false); }
    }

    private void ReportError(string title, Exception ex)
    {
        Log(title + "：" + ex.Message);
        SetNotice(title, ex.Message, InfoBarSeverity.Error);
    }

    private async void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (busy || dialogOpen || e.Handled) return;
        // Keep text editing and list navigation inside the native controls.
        DependencyObject focus = FocusManager.GetFocusedElement(Root.XamlRoot) as DependencyObject;
        bool buttonFocused = false;
        for (var element = focus; element != null; element = VisualTreeHelper.GetParent(element))
        {
            if (element is TextBox or PasswordBox or ComboBox or ListView or ListViewItem or NavigationViewItem) return;
            if (element is Microsoft.UI.Xaml.Controls.Primitives.ButtonBase) buttonFocused = true;
        }
        RunMode? mode = e.Key switch
        {
            VirtualKey.K => RunMode.KeyOnly,
            VirtualKey.S => RunMode.Normal,
            VirtualKey.U => RunMode.Pkey,
            VirtualKey.F => RunMode.Force,
            _ => null
        };
        if (step == 2 && mode.HasValue)
        {
            Modes.SelectedIndex = (int)mode.Value;
            e.Handled = true;
            return;
        }
        if (buttonFocused) return;
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            if (step < 2) ShowStep(step + 1);
            else await StartRunAsync();
            return;
        }
        if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            Close();
            return;
        }
    }
}

public sealed class SourceItem
{
    public string Path { get; }
    public string Label { get; }
    internal SourceItem(MediaSource source) { Path = source.Path; Label = source.Label; }
}

public sealed class EditionItem
{
    internal Edition Edition { get; }
    public string Name => Edition.Name;
    public string EditionId => Edition.EditionId;
    public string Group => Edition.Group;
    internal EditionItem(Edition edition) { Edition = edition; }
}
