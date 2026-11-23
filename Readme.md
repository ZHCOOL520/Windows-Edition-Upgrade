# 无损跨版本升级Windows

Windows 10 / 11 / Server 就地升级、版本切换，以及**实验性降级**工具（EXE 版为主，BAT 版已冻结）

> 作者个人网站：**https://zhcool.xyz**
>
> V1.0.0 · 2026 年 11 月
>
> 更详细的项目结构、源码职责和内部实现说明见 [Readme.Details.md](Readme.Details.md)。

---

## 一、这个工具能做什么

- 用微软官方预安装密钥（GLVK / OEM）在**就地升级**时直接选到想要的版本，不做全新安装，个人文件和应用都保留。
- 换一条官方升级路径上不存在的路：家庭版 → 专业工作站版、专业教育版 → 专业版、普通 ISO 装出企业版。
- 支持 LTSC 2021 / LTSC 2024、Server 2022 / Server 2025 等需要单独介质的版本。
- **实验性功能**：修改系统版本注册表，让安装程序把当前系统误判为旧版本，尝试把 Windows 11 就地降级回 Windows 10。

> 本工具**不是激活工具**。预安装密钥只用于安装时选择版本，不能激活系统；激活仍需要你自己的正版授权。

## 二、五种执行方式

| 快捷键 | 执行方式 | 改注册表 | 需要安装介质 | 说明 |
| --- | --- | --- | --- | --- |
| `K` | 仅更换密钥 | 否 | 否 | 用 `slmgr` 安装所选密钥，只切换版本，不做就地升级 |
| `S` | 普通升级 | 否 | 是 | 不指定版本，由安装程序自行判断，等同普通就地升级 |
| `U` | 指定版本升级 | 否 | 是 | 用目标版本的预安装密钥启动安装程序 |
| `F` | 强制升级 | 是（版本字段） | 是 | 改写 `ProductName` / `EditionID` / `CompositionEditionID`，跨过官方升级路径限制 |
| `D` | 实验性降级（伪装旧版本） | 是（含版本号） | 是，必须是 Windows 10 介质 | 把系统伪装成旧版 Windows，尝试就地降级，**未经测试** |

命令行窗口里按 `0` 可返回上一步；图形界面里对应 `K / S / U / F / D` 五个选项。

## 三、重点功能一：修改注册表强制升级（`F`）

Windows 安装程序在开始就地升级前，会读取当前系统的版本信息，并用「官方支持的升级路径」表决定哪些目标版本可装。比如家庭版不能直接升到企业版、专业教育版没有退回专业版的路径。

强制升级在启动安装程序**之前**，把下面三个值改写成目标版本的值：

| 注册表值 | 位置 | 作用 |
| --- | --- | --- |
| `ProductName` | `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion` | 让安装程序以为当前产品就是目标版本 |
| `EditionID` | 同上 | 版本标识，例如 `Core`、`Professional`、`EnterpriseS` |
| `CompositionEditionID` | 同上 | 组合版本标识，未提供时取 `EditionID` |

修改后照常启动安装程序，安装程序就会把「当前系统」当成目标版本，从而放行。

**适用与限制**

- 适合在**同一大版本内**切换版次（例如专业教育版 → 专业版、家庭版 → 专业版/企业版）。
- 需要先在目标版本页选择一个带 `EditionID` 的版本；自定义版本也可以，但 `EditionID` 必须填。
- **不能**用它把 Windows 11 变成 Windows 10 —— 这只改版次，不改系统内部版本号，安装程序仍会拒绝降级。
- 微软完全不支持这种做法。写入的值不会自动恢复，安装取消或失败时也会保留；写错了用正确版本再执行一次即可覆盖。

## 四、重点功能二：实验性降级（`D`）⚠️

> **未经验证的实验性功能。** 可能失败、可能被安装程序拒绝、可能导致系统无法正常启动。请先完整备份，准备恢复介质，并强烈建议先在虚拟机里试。

### 4.1 原理

安装程序判断「能不能升级」时，会读取当前系统的版本信息（产品名、版本标识、内部版本号等）。本模式在启动安装程序前，把这些值改写成**更老的系统**的样子，让安装程序认为当前系统比目标版本旧，从而不再以「目标版本比当前系统旧」为由拒绝，然后照常启动所选介质里的安装程序做就地升级。

这是纯注册表层面的「伪装」：真正的系统内核、驱动、组件仍然是当前的 Windows 11，只是让安装程序看到的值变了。

### 4.2 两档伪装目标

在「执行方式」页选择 **实验性降级** 后，会出现「伪装目标」下拉框：

| 档位 | 伪装成 | 实际写入的版本值 |
| --- | --- | --- |
| Windows 10 22H2 | Build 19045 | `CurrentVersion=6.3`、`CurrentBuild=19045`、`CurrentBuildNumber=19045`、`DisplayVersion=22H2`、`ReleaseId=2009`；`ProductName` / `EditionID` 取你选择的 Windows 10 目标版本 |
| Windows 7 SP1 | Build 7601 | `CurrentVersion=6.1`、`CurrentBuild=7601`、`CurrentBuildNumber=7601`、`CSDVersion=Service Pack 1`、`DisplayVersion=SP1`、`ReleaseId=6.1`；`ProductName=Windows 7 Professional`、`EditionID=Professional` |

**Windows 7 档比 Windows 10 档更激进**：它连主版本号都改成了 6.1，依赖「系统版本」做判断的组件、驱动安装程序、激活和更新相关逻辑都可能出现异常，出问题的概率明显更高。除非你明确知道自己要做什么，否则先用 Windows 10 档。

### 4.3 执行流程

1. **校验**：必须以管理员运行；必须有有效的 Windows 10 安装介质；必须已选择目标版本且 `EditionID` 非空。
2. **二次确认**：弹出风险提示框，明确告知实验性、可能失败、不自动恢复，需要手动点「我已备份，继续」。
3. **备份**：用 `reg export` 把整个 `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion` 导出到
   `C:\ProgramData\无损跨版本升级Windows\版本注册表备份-yyyyMMdd-HHmmss.reg`，路径会写进运行日志。
4. **写入伪装值**：按上面的档位改写 64 位与 32 位注册表视图中的版本字段。
5. **启动安装程序**：用 `setup.exe /pkey <目标密钥>` 启动所选介质中的 Windows 安装程序，后续完全交给安装程序接管。

### 4.4 风险与已知限制

- **不保证成功**：安装程序可能仍然识别出真实版本并拒绝降级；这一步只能在虚拟机或测试机上验证。
- **只是伪装**：系统本质仍是 Windows 11，降级过程中可能出现组件、驱动、应用不兼容，甚至升级失败回滚。
- **不会自动恢复**：注册表修改在安装取消、失败或回滚后都会保留，需要手动恢复（见 4.5）。
- **数据风险**：降级不是常规升级路径，个人文件和应用不保证完整保留；请先做完整系统备份/镜像。
- **授权与支持**：微软不支持这种做法，降级后激活状态、更新通道可能需要重新处理。
- **Windows 10 已结束支持**：2025 年 10 月 14 日停止支持，消费级 ESU 只延续到 2027 年 10 月 12 日。降到 Windows 10 意味着放弃 Windows 11 的新功能和安全更新，请确认确有需要。

### 4.5 出问题后如何恢复

1. 找到日志里记录的备份文件，例如
   `C:\ProgramData\无损跨版本升级Windows\版本注册表备份-20261123-132900.reg`；
2. 双击导入（需要管理员确认），或执行 `reg import "备份文件路径"`；
3. 重启系统。

如果没有备份文件，也可以手动把 `ProductName`、`EditionID`、`CompositionEditionID`、`CurrentVersion`、`CurrentBuild`、`CurrentBuildNumber`、`DisplayVersion`、`ReleaseId`、`CSDVersion` 改回原值，或使用系统还原点/系统镜像恢复。

### 4.6 建议的使用姿势

1. 先在虚拟机里用同版本 Windows 11 + Windows 10 ISO 走一遍，确认安装程序是否放行；
2. 在真机上先创建完整系统镜像（不是只备份文件）；
3. 准备 Windows 10 安装 U 盘，作为降级失败后的退路；
4. 优先选 Windows 10 档，Windows 7 档仅在明确需要时使用。

## 五、开始使用

1. 以管理员身份运行 `无损跨版本升级Windows.exe`（清单要求管理员权限，取消授权则不会启动）。
2. **选择安装源**：自动扫描所有盘符；也可以手动选择文件夹或 ISO（ISO 会自动挂载）。有效介质必须同时包含 `setup.exe` 和 `sources` 文件夹。
3. **选择目标版本**：27 个预置版本，或「自定义版本」自己填密钥、`EditionID`、`ProductName`。普通升级可以跳过这一步。
4. **选择执行方式**：`K / S / U / F / D` 五选一，核对摘要和提示后点「开始执行」。改注册表的模式还会再弹一次确认。
5. 启动后由 Windows 安装程序接管；本工具窗口可以关闭。执行过程会记录在运行日志里，可导出为 txt。

其他：导航底部可切换 深浅色主题（跟随系统为默认），窗口尺寸会被记住；「关于」里有作者网站、GitHub 和 QQ 邮箱。

## 六、构建

需要 .NET 10 SDK、Windows SDK 和 Windows App SDK 构建工具，首次构建需访问 NuGet：

```powershell
dotnet restore .\src\UpgradeTool.csproj
dotnet build .\src\UpgradeTool.csproj -c Release --no-restore
dotnet publish .\src\UpgradeTool.csproj -c Release -r win-x64 -o .\publish\win-x64 -p:Platform=x64
```

发布结果为**单个自包含 EXE**（约 96 MB，内含 .NET 运行时、Windows App SDK 和界面资源），分发时不需要附带其他文件。

## 七、详细文档

项目结构、每个源码文件的职责、注册表字段、五种模式的代码实现对照、版本列表与测试记录，见 [Readme.Details.md](Readme.Details.md)。

## 八、问题反馈

- GitHub 仓库：<https://github.com/ZHCOOL520/Windows-Edition-Upgrade>
- 问题反馈：<https://github.com/ZHCOOL520/Windows-Edition-Upgrade/issues>
- QQ 邮箱：`zhcool520@qq.com`
- 反馈时请附上运行日志：在执行方式页点「导出日志」保存成 txt 即可。

---

作者个人网站：**https://zhcool.xyz**
