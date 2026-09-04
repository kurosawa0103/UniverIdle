# SAVE-01 本地存档

> 状态：**已实现** · 更新：2026-09-04

## 职责

- 把玩家进度写入本地 `save.dat`（文件内仍是 JSON 文本），下次启动读回
- 开局无档时发默认陷阱并立刻建档
- **不**存进行中的挂机；**不做**离线结算（UTC 离线收益仍待做）

## 入口

| 类型 | 路径 / 菜单 |
|------|-------------|
| 存档文件 | `{Application.persistentDataPath}/save.dat` |
| IO | `GameSave` |
| 会话 | `GameSession`（Awake 加载；脏标记后定时 / Pause / Quit / Destroy 写入） |
| GM 重置 | **UniverIdle → GM...** →「重置存档」 |

## 文件清单

```
Assets/Scripts/Game/GameSave.cs
Assets/Scripts/Game/GameSession.cs
Assets/Scripts/Game/PlayerState.cs   （ToSaveFile / LoadFrom / ResetToNewPlayer）
Assets/Editor/Gm/GmSaveWindow.cs
```

## 自动存档

| 时机 | 行为 |
|------|------|
| 默认间隔 | **10 秒**（`GameSession.DefaultAutoSaveIntervalSeconds` / Inspector `autoSaveIntervalSeconds`） |
| 触发条件 | 物品 / 金币 / 熟练度变更后标脏；到点且脏才写盘 |
| 立刻写盘 | 开局建档、GM 重置、Pause、Quit、`OnDestroy` |

## 存档内容

| 字段 | 说明 |
|------|------|
| `gold` | 金币 |
| `unlockedPageCount` / `unlockedSlotCount` | 背包页与格 |
| `items[]` | `id` + `count` |
| `works[]` | 工作总等级与 XP |
| `actionMasteries[]` | `actionId` + 该动作熟练度等级与 XP（v2；旧 `scenes[]` 不再读取） |

加载时页/格会按当前 `inventory.json` 夹紧。

## 开局默认

无存档时：`small_trap ×8`、`large_trap ×3`（与旧硬编码一致），然后写入存档。

## GM

窗口只做一件事：**重置存档**。

- 未 Play：删文件
- Play 中：`GameSession.ResetToNewGame()`（停挂机 → 新号 → 删旧档 → 写新档 → 通知 UI）

## 依赖

- `PlayerState` 事件只标脏；定时器与生命周期负责写盘
- UI 不直接读写文件

## 扩展指南

| 要做的事 | 改哪里 |
|----------|--------|
| 改间隔 | `GameSession.autoSaveIntervalSeconds`（≤0 关闭定时） |
| 加字段 | `GameSaveFile` + `PlayerState.ToSaveFile/LoadFrom`，升 `version` |
| 离线收益 | 存 `lastQuitUtc`，进游戏再结算（尚未做） |
| 多存档槽 | 改 `GameSave.FileName` / 路径策略 |

## 已知限制

- 挂机中最多延迟约一个间隔才落盘（Pause/退出仍立刻存）
- 损坏 JSON 会警告并当无档开新号（不会自动备份）
- 不存当前动作；重置或重进后挂机停止
