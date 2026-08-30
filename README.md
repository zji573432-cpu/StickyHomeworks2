<!-- AES : U2FsdGVkX19MZ5w1Jmtii8QYDB3UFkveWPyRyOS2R+WcNULjk7Mc8JDUj48V4sVs -->
> 憋几把催了，主开发移交ClassIsBand了
<div align="center">
  
# <img width="32" height="32" alt="AppLogo" src="https://github.com/user-attachments/assets/40d567c2-77bc-4c3e-bb1f-5215cc0bb376" /> StickyHomeworks2

<img width="1280" height="720" alt="sh2banner" src="https://github.com/user-attachments/assets/f5ff1571-9acd-4af1-8663-0bb65731e8a3" />

>
> **StickyHomeworks2是一款支持富文本的桌面作业贴工具**  
> **适用于班级一体机上展示作业的情景**

[![Stars](https://img.shields.io/github/stars/StickyHomeworks2/StickyHomeworks2?label=Stars)](https://github.com/StickyHomeworks2/StickyHomeworks2)
[![正式版 Release](https://img.shields.io/github/v/release/StickyHomeworks2/StickyHomeworks2?style=flat-square&color=%233fb950&label=正式版)](https://github.com/StickyHomeworks2/StickyHomeworks2/releases/latest)
[![测试版 Release](https://img.shields.io/github/v/release/StickyHomeworks2/StickyHomeworks2?include_prereleases&style=flat-square&label=测试版)](https://github.com/StickyHomeworks2/StickyHomeworks2/releases/)
[![下载量](https://img.shields.io/github/downloads/StickyHomeworks2/StickyHomeworks2/total?style=social&label=下载量&logo=github)](https://github.com/StickyHomeworks2/StickyHomeworks2/releases/latest)<br/>
![.NET 版本](https://img.shields.io/badge/.NET-8-512bd4?style=flat-square)
![GitHub Repo size](https://img.shields.io/github/repo-size/StickyHomeworks2/StickyHomeworks2?style=flat-square&color=3cb371)
[![GitHub Repo Languages](https://img.shields.io/github/languages/top/StickyHomeworks2/StickyHomeworks2?style=flat-square)](https://github.com/StickyHomeworks2/StickyHomeworks2/search?l=c%23)

#### **💬[点我加入 QQ 群](https://qm.qq.com/q/w6vcjZA3DO)**

</div>

## 功能

- [X] 登记与修改作业
- [X] 富文本支持
- [X] 按科目分组作业
- [X] 为作业添加标签
- [X] 主界面全局缩放
- [X] 自动清理过期作业
- [X] 导出作业截图
- [x] 时间机器
- [x] 托盘菜单
- [x] 插入与管理表情包
- [x] 插入图片
- [x] 插入链接
- [x] KnotLink 联动（远程管理作业与窗口）
- [ ] oobe

## KnotLink 联动

本应用支持 [KnotLink](https://knotlink.cn) 协议联动，可作为**独立式节点**接入 KnotLink，供教师端或其他节点远程调用：

> **前置依赖**：使用前请先安装 [KnotLinkService](https://github.com/KnotLink-Protocol/KnotLinkService)（KnotLink 本地服务，必需）与 [KnotHub](https://github.com/KnotLink-Protocol/KnotHub)（节点管理面板，推荐）。

- **AppID**：`com.github.stickyhomeworks2.stickyhomeworks2`
- **作业管理**（`homework`）：查询 / 添加 / 修改 / 删除作业，管理科目
- **窗口控制**（`control`）：显示 / 隐藏窗口、置顶、移动、缩放、改标题、查状态

启动时应用会自动把清单释放到 `%LOCALAPPDATA%\KnotLink\<AppID>\` 并注册到 KnotLink，退出时清理，KnotHub 无需手动安装即可发现本节点。接口清单见 [KnotLink-Output/pr/com.github.stickyhomeworks2.stickyhomeworks2/FuncList.json](KnotLink-Output/pr/com.github.stickyhomeworks2.stickyhomeworks2/FuncList.json)，详细调用文档见 [sh2-knotlink-api.md](sh2-knotlink-api.md)。

## 开始使用

### 1. 检查设备需求

首先，请确保您的设备满足以下推荐需求：
- Windows 10 及以上版本的系统，x64架构
- 已安装[.NET 8.0桌面运行时](https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/runtime-desktop-8.0.1-windows-x64-installer)

### 1.5（可选）安装 KnotLink 生态组件

如需使用 [KnotLink 联动](#knotlink-联动)（远程布置作业、Agent 调用、窗口控制），请先安装：

- [KnotLinkService](https://github.com/KnotLink-Protocol/KnotLinkService) — KnotLink 本地服务，**必需**
- [KnotHub](https://github.com/KnotLink-Protocol/KnotHub) — 节点管理与发现面板，**推荐**

### 2. 下载软件本体

对于普通用户，可以在以下渠道下载到本软件，请根据自身网络环境选择合适的渠道。

> 测试版包含最新的功能，但也可能包含未完善和不稳定的功能，请谨慎使用。

<table>
  <tr>
    <th>下载渠道/通道 </th>
    <th>🚀正式版 </th>
    <th>🚧测试版 </th>
  </tr>
  <tr>
    <td>GitHub</td>
    <td><a href="https://github.com/zji573432-cpu/StickyHomeworks2/releases/latest"> <b>GitHub下载</b></a></td>
    <td><a href="https://github.com/HelloWRC/StickyHomeworks/releases"> GitHub下载</a></td>
  </tr>
  <tr>
    <td>QQ群文件下载</td>
    <td colspan="2"><a href="https://qm.qq.com/q/w6vcjZA3DO"><b>加入QQ群</b></a></td>
  </tr>
</table>


### 3. 解压软件

下载完成后，将软件压缩包解压到一个您喜欢的地方，运行软件即可开始使用。

## 开发

本应用目前的开发状态：

- 正在[`dev1`](https://github.com/StickyHomeworks2/StickyHomeworks2/tree/dev1)分支上开发本应用。

要在本地编译应用，您需要安装以下负载和工具：
- [.NET 8.0 SDK](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)
- [Visual Studio](https://visualstudio.microsoft.com/)

对于Visual Studio，您需要在安装时勾选以下工作负载：
- .NET 桌面开发

我们欢迎想要为本应用实现新功能或进行改进的同学提交 [Pull Request](https://github.com/StickyHomeworks2/StickyHomeworks2/pulls)。

## 致谢

<!-- ALL-CONTRIBUTORS-BADGE:START - Do not remove or modify this section -->
[![All Contributors](https://img.shields.io/badge/all_contributors-3-orange.svg?style=flat-square)](#contributors-)
<!-- ALL-CONTRIBUTORS-BADGE:END -->

本项目由 [HelloWRC/StickyHomeworks](https://github.com/HelloWRC/StickyHomeworks) 的项目二次开发

感谢以下同学为本项目为本项目的开发提供支持（[✨](https://allcontributors.org/docs/zh-cn/emoji-key)）：
<table>
  <tbody>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/HelloWRC"><img src="https://avatars.githubusercontent.com/u/55006226?v=4?s=100" width="100px;" alt="HelloWRC"/><br /><sub><b>HelloWRC</b></sub></a><br /><a href="https://github.com/ClassIsland/ClassIsland/commits?author=HelloWRC" title="Code">💻</a> <a href="#design-HelloWRC" title="Design">🎨</a> <a href="#ideas-HelloWRC" title="Ideas, Planning, & Feedback">🤔</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/jizilin6732"><img src="https://avatars.githubusercontent.com/u/162853646?v=4?s=100" width="100px;" alt="jizilin6732"/><br /><sub><b>jizilin6732</b></sub></a><br /><a href="https://github.com/StickyHomeworks2/StickyHomeworks2/commits?author=zji573432-cpu" title="Code">💻</a> <a href="#design-jizilin6732" title="Design">🎨</a> <a href="#ideas-jizilin6732" title="Ideas, Planning, & Feedback">🤔</a> <a href="#question-jizilin6732" title="Answering Questions">💬</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/belugaQAQ"><img src="https://avatars.githubusercontent.com/u/157481292?v=4?s=100" width="100px;" alt="belugaQAQ"/><br /><sub><b>ClassIsBand</b></sub></a><br /><a href="https://github.com/StickyHomeworks2/StickyHomeworks2/commits?author=belugaQAQ" title="Code">💻</a><a href="#design-jizilin6732" title="Design">🎨</a> <a href="#question-ClassIsBand" title="Answering Questions">💬</a> <a href="https://github.com/ClassIsland/ClassIsland/commits?author=belugaQAQ" title="Documentation">📖</a></td>
    </tr>
  </tbody>
</table>

## 赞助商/Sponsor
感谢以下赞助商对本项目提供支持:

<table>
    <td>
      <img alt="苏州晔淞信息科技有限公司" src="https://eb48d3a3.xy.proaa.top/Ystron/icon-256x256.png" width="50" height="50"/>
    </td>
    <td>
    由<a href="https://www.yesongit.com">苏州晔淞信息科技有限公司</a>提供云计算支持。苏州晔淞信息科技有限公司以自主创新、灵活开放的理念，以行业领先的云计算平台助力云上业务创新。
    </td>
  </tr> 
</table>

## 许可证

本项目基于 [General Public License v3](LICENSE.txt) 获得许可。