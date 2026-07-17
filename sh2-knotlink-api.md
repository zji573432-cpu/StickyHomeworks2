# SH2 KnotLink API 文档

## 概述

SH2 通过 KnotLink 协议（本地 TCP broker）暴露作业管理接口。

| 属性 | 值 |
|---|---|
| **appid** | `com.stickyhomeworks2` |
| **opensocketid** | `homework` |
| **角色** | OpenSocketResponser (端口 6378) |
| **协议格式** | KLKVMap (`key=value;key=value`) |
| **前置条件** | KnotLinkService.exe 必须已启动 |

## 通用约定

### 请求格式

```
action=<动作>;key1=value1;key2=value2
```

- 请求通过 OpenSocketQuerier (端口 6376) 发送，KnotLink 框架自动路由到对应 Responser。
- 无特殊字符转义，值中避免使用 `=` 和 `;`。

### 响应格式

```
status=ok;key1=value1;...
status=err;message=<错误描述>
```

- `homeworks` 的值是 JSON 数组字符串，调用方用 `JSON.parse()` 解析。

## 接口列表

### 1. ping — 心跳检测

| 项目 | 内容 |
|---|---|
| **描述** | 检查服务是否在线 |

**请求**:
```
action=ping
```

**响应**:
```
status=pong
```

---

### 2. list-homeworks — 获取全部作业

| 项目 | 内容 |
|---|---|
| **描述** | 读取 Profile.json，返回所有作业 |

**请求**:
```
action=list-homeworks
```

**响应**:
```
status=ok;count=2;homeworks=[{"id":"guid","subject":"语文","content":"背诵课文","dueDate":"2026-07-18","tags":["背诵","默写"]}]
```

**homeworks 数组元素**:

| 字段 | 类型 | 说明 |
|---|---|---|
| `id` | string (GUID) | 作业唯一标识 |
| `subject` | string | 科目 |
| `content` | string | 内容（纯文本） |
| `dueDate` | string | 截止日期 (yyyy-MM-dd) |
| `tags` | string[] | 标签列表 |

---

### 3. add-homework — 添加作业

| 项目 | 内容 |
|---|---|
| **描述** | 新建作业，写入 Profile.json |

**请求**:
```
action=add-homework;subject=语文;content=背诵课文;dueDate=2026-07-18;tags=背诵,默写
```

| 参数 | 必填 | 说明 |
|---|---|---|
| `subject` | 是 | 科目名称 |
| `content` | 否 | 作业内容（纯文本） |
| `dueDate` | 否 | 截止日期 (yyyy-MM-dd)，默认当天 |
| `tags` | 否 | 逗号分隔的标签列表 |

**响应**:
```
status=ok;id=3a732126-2073-45a8-a56f-54f0ccd434d3
```

---

### 4. edit-homework — 修改作业

| 项目 | 内容 |
|---|---|
| **描述** | 按 GUID 修改指定作业，仅更新传入的字段 |

**请求**:
```
action=edit-homework;id=3a732126-2073-45a8-a56f-54f0ccd434d3;content=更新后的内容;tags=新标签
```

| 参数 | 必填 | 说明 |
|---|---|---|
| `id` | 是 | 作业 GUID |
| `subject` | 否 | 新的科目名 |
| `content` | 否 | 新的内容 |
| `dueDate` | 否 | 新的截止日期 |
| `tags` | 否 | 新的标签列表（逗号分隔，覆盖） |

**响应**:
```
status=ok
```

---

### 5. delete-homework — 删除作业

| 项目 | 内容 |
|---|---|
| **描述** | 按 GUID 删除指定作业 |

**请求**:
```
action=delete-homework;id=3a732126-2073-45a8-a56f-54f0ccd434d3
```

| 参数 | 必填 | 说明 |
|---|---|---|
| `id` | 是 | 作业 GUID |

**响应**:
```
status=ok
```

---

### 6. list-subjects — 获取科目列表

| 项目 | 内容 |
|---|---|
| **描述** | 读取 Settings.json，返回所有科目 |

**请求**:

```
action=list-subjects
```

**响应**:
```
status=ok;subjects=语文,数学,英语,物理
```

---

### 7. manage-subjects — 管理科目

| 项目 | 内容 |
|---|---|
| **描述** | 增/改/删科目（操作 Settings.json） |

**添加**:
```
action=manage-subjects;op=add;name=生物
```

**重命名**:
```
action=manage-subjects;op=edit;name=生物;newname=化学
```

**删除**:
```
action=manage-subjects;op=delete;name=化学
```

| 参数 | add | edit | delete | 说明 |
|---|---|---|---|---|
| `op` | ● | ● | ● | 操作类型 |
| `name` | ● | ● | ● | 科目名 |
| `newname` | — | ● | — | 新名称（仅 edit） |

**响应**:
```
status=ok
```

---

## Python 调用示例

```python
from knotlink import OpenSocketQuerier, KLKVMap
import json

q = OpenSocketQuerier('com.stickyhomeworks2', 'homework')

# 心跳
print(q.query('action=ping'))  # status=pong

# 获取作业列表
resp = q.query('action=list-homeworks')
kv = KLKVMap(); kv.deserialize(resp)
homeworks = json.loads(kv.get('homeworks'))

# 添加作业
resp = q.query('action=add-homework;subject=语文;content=背诵课文;dueDate=2026-07-18;tags=背诵,默写')
kv = KLKVMap(); kv.deserialize(resp)
new_id = kv.get('id')

# 修改作业
q.query(f'action=edit-homework;id={new_id};content=修改后内容')

# 删除作业
q.query(f'action=delete-homework;id={new_id}')

# 管理科目
q.query('action=manage-subjects;op=add;name=新增科目')
q.query('action=manage-subjects;op=edit;name=新增科目;newname=改名后')
q.query('action=manage-subjects;op=delete;name=改名后')
```

## 错误响应

所有错误统一格式：

```
status=err;message=<错误描述>
```

常见错误：

| message | 原因 |
|---|---|
| `unknown action` | action 值不在支持列表中 |
| `subject is required` | add-homework 缺少 subject |
| `invalid id` | edit/delete 的 id 不是有效 GUID |
| `homework not found` | 指定 id 的作业不存在 |
| `op is required` | manage-subjects 缺少 op |
| `name is required` | manage-subjects 缺少 name |
| `newname is required for edit operation` | edit 操作缺少 newname |
| `unknown op: xxx` | op 不是 add/edit/delete |
