# FoxVideoSCR

一个 Windows 视频屏幕保护程序。用 WPF 合成窗口播放本地视频，为旧式视频屏保的画面撕裂问题提供另一种播放方式。

## 首版功能

- 视频列表：多选添加、添加文件夹、去重、移除与调整顺序。
- 顺序循环、随机播放（避免紧邻重复）、单曲循环。
- 保持比例完整显示、裁切铺满、拉伸铺满。
- 音量、静音、0.5–2 倍播放速度。
- 主显示器或所有显示器播放；仅主显示器出声。
- 原生屏保入口：`/s` 全屏、`/c` 设置、`/p <HWND>` 系统小预览。
- 25 秒测试预览；按键、点击或移动鼠标退出。
- 视频打开失败后跳过；全部不可播放时显示提示，避免无限重试。
- 配置与滚动日志保存在 `%LOCALAPPDATA%\FoxVideoSCR`，没有联网功能。
- 首次启动时，如本机已有 ZD Soft Movie Screensaver 视频设置，会读取该视频作为初始列表；不导入授权信息。

## 环境与解码

Windows 10/11（64 位），.NET Framework 4.8（通常已随系统安装）。程序编译为 **x64（64 位）**，并启用 Per-Monitor DPI v2，适配多显示器不同缩放比例下的无缝全屏播放。

视频格式能否播放，取决于系统安装的 **64 位**分离器和解码器（例如 Windows 自带的 HEVC 视频扩展，或 [LAV Filters 官方发行版](https://github.com/Nevcairiel/LAVFilters/releases) 的 x64 Splitter / Video / Audio）。此项目不附带、不自动下载解码器。

合成输出在开发机的 HEVC 视频测试中消除了旧屏保的横向撕裂；不同显卡、显示器和远程桌面环境仍需实测。多屏独立播放，首版不保证逐帧同步。倍速也受底层媒体组件支持程度影响。

## 构建与使用

在 Windows PowerShell 中运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

构建脚本使用系统自带的 .NET Framework C# 编译器，无需 NuGet、Visual Studio 或额外 SDK。产物位于 `dist`。

直接打开 `dist\FoxVideoSCR.scr` 的“配置”入口，或用 PowerShell 指定参数：

```powershell
$start = New-Object Diagnostics.ProcessStartInfo
$start.FileName = (Resolve-Path .\dist\FoxVideoSCR.scr).Path
$start.Arguments = '/c' # 设置；改为 /test 可预览 25 秒
$start.UseShellExecute = $false
[Diagnostics.Process]::Start($start)
```

## 安装与卸载

普通用户可在 [Releases](https://github.com/Foxing365/FoxVideoSCR/releases) 下载 `FoxVideoSCR-v0.2.0-windows-x64.zip`，**先解压整个 ZIP**，双击其中的 `Install.cmd`，并确认 Windows 管理员权限提示。不要在压缩包内直接运行，也不需要下载 Source code。

安装后，在自动打开的“屏幕保护程序设置”中确认选中 **FoxVideoSCR**，点击“设置”添加视频、调整播放偏好和显示器设置、保存，再点“预览”。等待时间和恢复时登录选项由 Windows 管理。

如果不自动打开设置，可在 Windows“设置 → 个性化 → 锁屏界面 → 屏幕保护程序”中进入。HEVC 等格式无法播放时，安装下文所述的 64 位解码组件；本程序不附带解码器。

卸载时关闭屏保及预览，双击同一目录的 `Uninstall.cmd`，它会尝试恢复安装前的屏保，个人配置仍会保留。

也可以通过 PowerShell 手动安装。关闭已有的屏保预览，在解压目录（或开发者的 `dist` 目录）运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1
```

确认 Windows 的管理员权限提示后，安装器将程序复制到 System32 和 SysWOW64，并为当前用户选中 FoxVideoSCR。两份文件用于兼容 Windows 的 32 位路径重定向。等待时间和恢复登录选项不会被更改。原屏保路径会备份，用 `Uninstall.ps1` 可恢复。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Uninstall.ps1
```

个人配置保留，便于以后重新安装。若杀毒软件对本地构建的未签名 `.scr` 报警，请先检查源码与拦截记录，再决定是否允许该文件；无需关闭系统防护。

## 验证

`/selftest` 检查列表循环、失败跳过、随机不连播、单曲回退、配置范围与 JSON 往返。结果写入 `%LOCALAPPDATA%\FoxVideoSCR\selftest-result.txt`。GitHub Actions 会构建、运行这些检查并上传可下载产物。

正式使用前请在本机验证系统小预览、全屏播放、输入退出、音量与多显示器表现。屏保不接管 Windows 锁屏或身份验证；恢复时是否要求登录由 Windows 的屏保设置管理。

## 许可证

[MIT](LICENSE)。不附带视频、旧屏保程序或旧软件的注册数据。
