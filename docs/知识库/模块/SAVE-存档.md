# SAVE-01 本地存档

> 状态：**已实现** · 更新：2026-09-03

## 职责

- 把玩家进度写入本地 `save.dat`（文件内仍是 JSON 文本），下次启动读回
- 开局无档时发默认陷阱并立刻建档
- **不**存进行中的挂机；**不做**离线结算（UTC 离线收益仍待做）

## 入口

| 类型 | 路径 / 菜单 |
|------|-------------|
| 存档文件 | `{Application.persistentDataPath}/save.dat` |
| IO | `GameSave` |
| 会话 | `GameSession`（Awake 加载；变更 / Pause / Quit / Destroy 写入） |
| GM 重置 | **UniverIdle → GM...** →「重置存档」 |

## 文件清单

```
Assets/Scripts/Game/GameSave.cs
Assets/Scripts/Game/GameSession.cs
Assets/Scripts/Game/PlayerState.cs   （ToSaveFile / LoadFrom / ResetToNewPlayer）
Assets/Editor/Gm/GmSaveWindow.cs
```

## 存档内容

| 字段 | 说明 |
|------|------|
| `gold` | 金币 |
| `unlockedPageCount` / `unlockedSlotCount` | 背包页与格 |
| `items[]` | `id` + `count` |
| `works[]` | 工作等级与 XP |
| `scenes[]` | `workId` + `sceneId` + 等级与 XP |

加载时页/格会按当前 `inventory.json` 夹紧。

## 开局默认

无存档时：`small_trap ×8`、`large_trap ×3`（与旧硬编码一致），然后写入存档。

## GM

窗口只做一件事：**重置存档**。

- 未 Play：删文件
- Play 中：`GameSession.ResetToNewGame()`（停挂机 → 新号 → 删旧档 → 写新档 → 通知 UI）

## 依赖

- `PlayerState` 事件触发自动存盘
- UI 不直接读写文件

## 扩展指南

| 要做的事 | 改哪里 |
|----------|--------|
| 加字段 | `GameSaveFile` + `PlayerState.ToSaveFile/LoadFrom`，升 `version` |
| 离线收益 | 存 `lastQuitUtc`，进游戏再结算（尚未做） |
| 多存档槽 | 改 `GameSave.FileName` / 路径策略 |

## 已知限制

- 每次物品/金币/熟练度变化都整文件写入（挂机节奏下可接受）
- 损坏 JSON 会警告并当无档开新号（不会自动备份）
- 不存当前动作；重置或重进后挂机停止

## 变更记录

| 日期 | 变更 |
|------|------|
| 2026-09-03 | 去掉 `save.json` 迁移；只认 `save.dat` |
| 2026-09-03 | 文件名改为 `save.dat`；兼容读入旧 `save.json` |
| 2026-09-03 | 初版 JSON 存档 + GM 重置 |
