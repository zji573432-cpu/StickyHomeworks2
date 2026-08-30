# StickyHomeworks2 — KnotLink 节点

支持富文本的桌面作业贴工具，适用于班级一体机上展示作业的情景。通过 KnotLink 协议（本地 TCP broker）暴露作业管理与窗口控制接口，可被教师端或其他 KnotLink 节点远程调用。

- **AppID**: `com.github.stickyhomeworks2.stickyhomeworks2`
- **类型**: 独立式（standalone）
- **下载**: https://github.com/StickyHomeworks2/StickyHomeworks2/releases/latest

## 接口列表（openSocket）

### `homework` — 作业管理

| 功能条目 | 操作（action） | 说明 |
|---|---|---|
| ping-homework | `ping` | 心跳检测 |
| list-homeworks | `list-homeworks` | 获取所有作业列表 |
| add-homework | `add-homework` | 添加作业（subject/content/dueDate/tags） |
| edit-homework | `edit-homework` | 修改作业（按 GUID，仅更新传入字段） |
| delete-homework | `delete-homework` | 删除作业（按 GUID） |
| list-subjects | `list-subjects` | 获取科目列表 |
| manage-subjects | `manage-subjects` | 增/改/删科目（op=add/edit/delete） |

### `control` — 窗口控制

| 功能条目 | 操作（action） | 说明 |
|---|---|---|
| ping-control | `ping` | 心跳检测 |
| hide-window | `hide-window` | 隐藏主窗口 |
| show-window | `show-window` | 显示并激活主窗口 |
| set-topmost | `set-topmost` | 设置置顶 |
| set-position | `set-position` | 设置窗口位置（x/y） |
| set-size | `set-size` | 设置窗口大小（width/height） |
| set-title | `set-title` | 修改窗口标题 |
| get-window-state | `get-window-state` | 获取窗口状态 |

## 信号（signal）

暂无。

## 通信约定

- 协议格式：KLKVMap（`key=value;key=value`）
- 请求经 OpenSocketQuerier（端口 6376）发送，自动路由到对应 Responser
- 响应统一格式：`status=ok;...` 或 `status=err;message=<错误描述>`
- 详细参数与响应字段见 [sh2-knotlink-api.md](https://github.com/StickyHomeworks2/StickyHomeworks2/blob/master/sh2-knotlink-api.md)

## 开发调试

将本目录注册到 KnotHub 进行开发调试：

```bat
reg-dev.bat C
```

发布时先同步到 release、再升版本号：

```bat
sync-to-release.bat
version.bat
```
