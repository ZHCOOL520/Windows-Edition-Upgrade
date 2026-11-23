# 无损跨版本升级Windows · 详细说明

> 这是 [Readme.md](Readme.md) 的配套文档，面向想了解项目结构、源码职责和内部实现的人。使用方法和风险提示请先看主 README。

## 一、项目结构

```
无损跨版本升级Windows/
├─ Readme.md                         主 README（简介、两种改注册表功能、使用方法）
├─ Readme.Details.md                 本文档（项目结构与内部实现）
├─ release/                          发行版目录（EXE、BAT、分发的说明文档）
├─ publish/win-x64/                  发布产物（已加入 .gitignore）
└─ src/
   ├─ UpgradeTool.csproj             EXE 版工程文件（当前唯一参与构建的工程）
   ├─ App.xaml / App.xaml.cs         WinUI 3 应用入口
   ├─ MainWindow.xaml                主窗口界面（NavigationView 三步流程）
   ├─ MainWindow.xaml.cs             主窗口逻辑（扫描、选择、执行、主题、日志）
   ├─ UpgradeServices.cs             服务层（注册表、介质识别、ISO 挂载、密钥、启动安装）
   ├─ Data.cs                        数据模型与常量（版本表、执行方式枚举、命令行参数）
   ├─ AssemblyInfo.cs                程序集元数据
   ├─ app.manifest                   清单（要求管理员权限、DPI 感知、兼容性声明）
   ├─ app.ico                        应用图标
   │
   └─ 旧 WinForms 源码（不参与编译，仅作迁移参考）
      ├─ MainForm.cs / MainForm.Actions.cs
      ├─ Program.cs
      ├─ Controls.cs / Theme.cs
      ├─ CustomDialog.cs / AboutDialog.cs
```

### 当前编译范围

`UpgradeTool.csproj` 设置了 `EnableDefaultCompileItems=false`，只显式编译：

- `App.xaml.cs`（依赖 `App.xaml`）
- `MainWindow.xaml.cs`（依赖 `MainWindow.xaml`）
- `Data.cs`
- `UpgradeServices.cs`
- 内容文件 `app.ico`

因此 `MainForm*.cs`、`Program.cs`、`Controls.cs`、`Theme.cs`、`CustomDialog.cs`、`AboutDialog.cs` 都是**旧 WinForms 界面**的遗留源码，不参与 EXE 版构建。BAT 版已冻结，也不再从这里生成。

## 二、各文件职责

### App.xaml / App.xaml.cs

最小的 WinUI 3 启动入口：`OnLaunched` 里创建 `MainWindow` 并激活。没有单实例逻辑，也没有自定义标题栏。

### MainWindow.xaml

- 顶层 `Grid x:Name="Root"`，挂 `Loaded`、`KeyDown`。
- `NavigationView`：三个步骤页（安装源 / 目标版本 / 执行方式），`PaneDisplayMode="Auto"`，折叠后只显示图标。
- 底部 `FooterMenuItems`：`关于`（`Tag="about"`）和主题切换项 `ThemeNav`（`Tag="theme"`，太阳/月亮图标，折叠状态下依然可见）。
- 三步内容用 `StackPanel` 切换可见性，公共头部包含步骤标签、标题、描述。
- 执行方式页：`RadioButtons`（5 个选项，`Tag` 与 `RunMode` 枚举同名）、`ComboBox x:Name="SpoofPicker"`（伪装目标，仅实验性降级时可见）、`InfoBar x:Name="ModeHint"`、运行日志 `TextBox` 和导出按钮。
- 底部工具栏：状态文字、`ProgressRing`、上一步 / 下一步（最后一步变为「开始执行」）。

### MainWindow.xaml.cs

| 区域 | 说明 |
| --- | --- |
| 构造 | 恢复窗口尺寸、设置图标；支持 Mica 时使用 `MicaBackdrop` 并让根背景透明；绑定版本列表分组；恢复主题；默认选中第一步 |
| `Root_Loaded` | 检查管理员权限，未提权时禁用「下一步」；然后异步扫描安装源 |
| `ApplyTheme` / `UpdateThemeButton` / `ToggleTheme` | 主题三态（系统 / 浅色 / 深色），同步导航项图标与提示文字，并同步窗口标题栏的 `PreferredTheme` |
| `ShowStep` | 切换步骤、更新标题描述、同步导航选中项、回到页首 |
| `ScanSourcesAsync` | 后台调用 `UpgradeServices.DetectSources`，保留手动选择的子目录 |
| `ValidationMessage` | 集中校验：管理员、安装源、目标版本、`EditionID` |
| `UpdateState` | 刷新摘要、按钮可用性、状态文字和 `ModeHint`（含实验性降级的档位提示） |
| `StartRunAsync` | 五种模式的执行入口：确认框 → 必要备份/改注册表 → `LaunchSetup` |
| `Root_KeyDown` | `K/S/U/F/D` 切换执行方式，`Enter` 下一步/执行，`Esc` 关闭；输入框、列表、下拉框获得焦点时不拦截 |

关键状态字段：`ready`（初始化完成）、`busy`（有后台操作）、`dialogOpen`（对话框打开）、`setupStarted`（安装程序已启动，防止重复执行）。关闭窗口时如果正在执行或对话框未关，会阻止关闭。

### UpgradeServices.cs

全部与 Windows 交互的操作都集中在这里：

| 方法 | 作用 |
| --- | --- |
| `IsAdministrator` | 判断当前进程是否为管理员 |
| `ReadTheme` / `SaveTheme` | 主题偏好读写，值只能是 `system` / `light` / `dark` |
| `ReadWindowSize` / `SaveWindowSize` | 窗口尺寸记忆（800×600 ~ 8000×8000 之间才接受） |
| `OsDescription` / `CurrentProductName` | 读取当前系统版本描述与产品名 |
| `IsMediaRoot` | 判断目录是否同时包含 `setup.exe` 和 `sources` |
| `DetectSources` | 扫描所有盘符与程序目录，收集可用安装介质 |
| `MountIsoAsync` | 用 PowerShell `Mount-DiskImage` 挂载 ISO，并解析出介质根目录 |
| `InstallKeyAsync` | 调用 `cscript slmgr.vbs /ipk` 安装预安装密钥 |
| `LaunchSetup` | 用参数列表启动 `setup.exe`，正确处理带空格的路径 |
| `WriteEditionRegistry` | 强制升级：改写 `ProductName` / `EditionID` / `CompositionEditionID` |
| `BackupVersionRegistryAsync` | 实验性降级：用 `reg export` 备份整个版本注册表项 |
| `WriteVersionSpoofRegistry` | 实验性降级：按 `SpoofProfile` 写入伪装版本值 |

### Data.cs

- `Data` 常量：应用名、版本号 `V1.0.0`、作者网站、GitHub 地址、QQ 邮箱。
- 命令行参数：
  - `ArgsNormal`：`/eula accept /telemetry disable /priority normal /resizerecoverypartition enable`
  - `ArgsForce`：在上面基础上追加 `/pkey `，后接目标版本密钥。
- `Editions`：27 个预置版本（编号 1–27），加上界面里的「自定义版本」共 28 项；每项包含密钥、`EditionID`、`ProductName`、`CompositionEditionID` 和分组。
- `RunMode` 枚举：`KeyOnly` / `Normal` / `Pkey` / `Force` / `Downgrade`，顺序必须和界面单选项顺序一致（快捷键直接按索引选中）。
- `SpoofProfile` 枚举：`Win10` / `Win7`，对应实验性降级的两个档位。

## 三、注册表位置

| 位置 | 键值 | 用途 |
| --- | --- | --- |
| `HKCU\Software\ZHCOOL\WindowsEditionUpgrade` | `Theme`（字符串） | 主题偏好 |
| 同上 | `WinW` / `WinH`（DWORD） | 窗口尺寸 |
| `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion` | `ProductName`、`EditionID`、`CompositionEditionID` | 强制升级写入 |
| 同上 | `CurrentVersion`、`CurrentBuild`、`CurrentBuildNumber`、`CSDVersion`、`DisplayVersion`、`ReleaseId` | 实验性降级额外写入 |

强制升级只改前三个值；实验性降级在两档中都会改全部字段（Windows 10 档的 `ProductName` / `EditionID` 取自所选目标版本，Windows 7 档固定为 `Windows 7 Professional` / `Professional`）。

备份文件固定写到 `C:\ProgramData\无损跨版本升级Windows\版本注册表备份-yyyyMMdd-HHmmss.reg`，每执行一次生成一个，不会覆盖旧文件。

## 四、执行方式与代码路径对照

| 模式 | 前置条件 | 注册表 | 启动参数 |
| --- | --- | --- | --- |
| 仅更换密钥 | 已选目标版本 | 不改 | 走 `slmgr /ipk`，不启动安装程序 |
| 普通升级 | 有效安装源 | 不改 | `ArgsNormal` |
| 指定版本升级 | 有效安装源 + 目标版本 | 不改 | `ArgsForce + 密钥` |
| 强制升级 | 有效安装源 + 目标版本 + `EditionID` | 写三个版本字段 | `ArgsForce + 密钥` |
| 实验性降级 | 有效 **Windows 10** 介质 + 目标版本 + `EditionID` | 先备份，再写完整伪装字段 | `ArgsForce + 密钥` |

注：指定版本升级与强制升级使用同一组参数，区别只在于是否改写注册表。实验性降级目前也复用 `/pkey` 启动，伪装目标与所选版本需要你自己保持一致（例如选 Windows 10 专业版做目标）。

## 五、界面细节

- 三步流程：安装源 → 目标版本 → 执行方式，导航栏可直接跳转，`上一步` 返回。
- 执行方式页的 `RadioButtons` 与 `RunMode` 顺序绑定，快捷键 `K/S/U/F/D` 直接切到对应项。
- 主题切换是导航底部的图标项：深色显示太阳、浅色显示月亮，默认跟随系统；窗口标题栏同步黑白。
- 状态提示使用 `InfoBar`：普通模式为信息级，强制升级和实验性降级为警告级；实验性降级还会按档位显示更严重的文案。
- 运行日志最多保留 500 行，可选中复制，可导出为 UTF-8 文本。
- 关闭窗口时如果有操作进行中，会取消关闭并提示先完成。

## 六、构建与发布

工程要点（`src/UpgradeTool.csproj`）：

| 配置 | 值 |
| --- | --- |
| 目标框架 | `net10.0-windows10.0.22621.0` |
| 最低平台 | Windows 10 2004（19041） |
| Windows App SDK | `2.6.5` |
| 打包方式 | `WindowsPackageType=None`（unpackaged） |
| 平台 | 仅 `x64` |
| 发布 | 自包含 + 单文件 + 压缩（`SelfContained`、`WindowsAppSDKSelfContained`、`PublishSingleFile`、`EnableCompressionInSingleFile`） |
| 裁剪 | 关闭（`PublishTrimmed=false`，WinUI 不支持裁剪） |

构建命令：

```powershell
dotnet restore .\src\UpgradeTool.csproj
dotnet build .\src\UpgradeTool.csproj -c Release --no-restore
dotnet publish .\src\UpgradeTool.csproj -c Release -r win-x64 -o .\publish\win-x64 -p:Platform=x64
```

发布产物为单个 `无损跨版本升级Windows.exe`（约 96 MB，首次启动会解包到临时目录）。`src/bin`、`src/obj`、`publish` 已在 `.gitignore` 中排除，仓库只保留源码，EXE 通过 GitHub Releases 分发。

> 最低声明 Windows 10 2004 只是清单上的声明，不代表已在所有 Windows 10 / Server 版本上做过运行验证。

## 七、版本自动生成对照表

安装程序可以在线从基础版本生成附加版本（需要联网）：

| 附加版本 | 需要的基础版本 |
| --- | --- |
| Windows 家庭版 单语言版 | Windows 家庭版 |
| Windows 专业工作站版 | Windows 专业版 |
| Windows 专业教育版 | Windows 专业版 |
| Windows 教育版 | Windows 专业版 |
| Windows 企业版 | Windows 专业版 |
| Windows 企业版 多会话版 / 虚拟桌面 | Windows 专业版 |
| Windows IoT 企业版 | Windows 专业版 |
| Windows SE 云版 | Windows 专业版 |
| Windows 专业工作站版 N | Windows 专业版 N |
| Windows 专业教育版 N | Windows 专业版 N |
| Windows 教育版 N | Windows 专业版 N |
| Windows 企业版 N | Windows 专业版 N |
| Windows SE 云版 N | Windows 专业版 N |
| Windows 10 IoT 企业版 LTSC 2021（仅单独介质） | Windows 10 企业版 LTSC 2021 |
| Windows 11 IoT 企业版 LTSC 2024（仅单独介质） | Windows 11 企业版 LTSC 2024 |

也就是说，ISO 里只要有家庭版、家庭版 N、专业版、专业版 N 中的一种，通常就能装出所有非 LTSC 版本。LTSC 与 Server 必须使用各自的介质；Windows 11 企业版 N LTSC 2024 还需要 N 版介质。

## 八、预置版本列表

| 编号 | 版本 | 编号 | 版本 |
| --- | --- | --- | --- |
| 1 | Windows 家庭版 | 15 | Windows 教育版 N |
| 2 | Windows 专业版 | 16 | Windows 企业版 N |
| 3 | Windows 专业工作站版 | 17 | Windows 11 SE 云版 N |
| 4 | Windows 企业版 | 18 | Windows 10 企业版 LTSC 2021 |
| 5 | Windows 专业教育版 | 19 | Windows 10 IoT 企业版 LTSC 2021 |
| 6 | Windows 教育版 | 20 | Windows 10 企业版 N LTSC 2021 |
| 7 | Windows 企业版 多会话版 | 21 | Windows Server 2022 标准版 |
| 8 | Windows IoT 企业版 | 22 | Windows Server 2022 数据中心版 |
| 9 | Windows 家庭版 单语言版 | 23 | Windows 11 企业版 LTSC 2024 |
| 10 | Windows 11 SE 云版 | 24 | Windows 11 IoT 企业版 LTSC 2024 |
| 11 | Windows 家庭版 N | 25 | Windows 11 企业版 N LTSC 2024 |
| 12 | Windows 专业版 N | 26 | Windows Server 2025 标准版 |
| 13 | Windows 专业工作站版 N | 27 | Windows Server 2025 数据中心版 |
| 14 | Windows 专业教育版 N | 28 | 自定义版本（自己提供密钥） |

各版本密钥与 `EditionID` 按 2026 年 11 月的 Windows 11 26H2 核对；K 版、中国特供版等不在列表中，请用自定义版本自行填写。

**LTSC 密钥（2026 年核对，均为官方 KMS / GLVK 预安装密钥）**

- Windows 11 企业版 LTSC 2024：`M7XTQ-FN8P6-TTKYV-9D4CC-J462D`（`EnterpriseS`）
- Windows 11 企业版 N LTSC 2024：`92NFX-8DJQP-P6BBQ-THF9C-7CG2H`（`EnterpriseSN`）
- Windows 11 IoT 企业版 LTSC 2024：`CGK42-GYN6Y-VD22B-BX98W-J8JXD`（`IoTEnterpriseS`）
- Windows Server 2025 标准版：`TVRH6-WHNXV-R9WG3-9XRFY-MY832`
- Windows Server 2025 数据中心版：`D764K-2NDRG-47T6Q-P8T8W-YP6DF`

## 九、测试情况

- 曾用只含家庭版镜像的 WIM 测试：用专业版密钥启动，结果仍进入家庭版安装流程（符合预期）。
- WIM 中只保留专业版镜像后再用专业版密钥启动，正确进入专业版流程。
- 中断安装后改用专业工作站版密钥启动，安装程序从专业版自动生成专业工作站版。
- 消费者版 ISO（原本不含企业版）配合企业版密钥，成功自动生成并安装企业版。
- Windows 10 企业版 LTSC 2021（含 IoT / N）已用官方 LTSC 2021 ISO 测试通过。
- 以上测试均需联网在线生成版本。

**未验证项**

- EXE 版界面在真实环境中的完整交互（主题、DPI、窄窗口、选择器、快捷键、取消流程、日志导出）未逐项回归。
- 强制升级、实验性降级（两个档位）均未在真机执行验证；降级功能属实验性，风险自负。
- Windows 10 / Server 各版本上的兼容性未全面覆盖。

## 十、版本现状（2026 年 11 月核对）

- Windows 11 主线版本为 26H2（2026-09-29 发布）；26H1 仅随新设备提供。
- Windows 11 23H2 已结束支持；24H2 家庭版 / 专业版已停止更新，企业版到 2027-10-12；25H2 家庭版 / 专业版到 2027-10-12，企业版到 2028-10-10。
- Windows 10 已于 2025-10-14 结束支持，消费级 ESU 延续到 2027-10-12。
- Windows 11 SE 已于 2026 年 10 月结束支持。
- Windows Server 2025 于 2024-11-01 发布。
- Windows 11 企业版 LTSC 2024（基于 24H2）企业版支持到 2029-10-09，IoT 企业版 LTSC 2024 到 2034-10-10。

## 十一、文件编码约定

| 文件 | 编码 | 换行 |
| --- | --- | --- |
| `无损跨版本升级Windows-BAT版.bat` | GBK（代码页 936） | CRLF |
| `Readme.md` / `Readme.Details.md` | UTF-8 | 无强制要求 |
| 分发的纯文本说明 | UTF-8 BOM | CRLF |

修改这些文件时请保持原有编码，否则中文 Windows 下双击运行 BAT 会出现乱码。

## 十二、反馈

- GitHub 仓库：<https://github.com/ZHCOOL520/Windows-Edition-Upgrade>
- Issues：<https://github.com/ZHCOOL520/Windows-Edition-Upgrade/issues>
- QQ 邮箱：`zhcool520@qq.com`

---

作者个人网站：**https://zhcool.xyz**
