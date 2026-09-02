# UI-01 主界面（UGUI）

> 状态：**已接拾荒挂机** · 更新：2026-09-02

## 职责

- 提供与 [02-界面](../../设计/02-界面.md) / [主界面-概念.html](../../设计/概念图/主界面-概念.html) 一致的 **PC 主界面布局**
- 左栏工作导航、中栏地点横幅 + 动作卡 + 进度、右栏详情、背包面板
- 运行时：切换工作、选动作挂机、进度条、背包刷新

## 入口

| 类型 | 路径 |
|------|------|
| 场景根节点 | `UniverIdle_MainUI`（在场景中手配） |
| 运行时控制器 | `MainUIController` + `GameSession` on `App` |

## 文件清单

```
Assets/Scripts/UI/MainUIController.cs
Assets/Scripts/UI/MainUIInputBootstrap.cs
Assets/Scripts/UI/WorkCenterHost.cs
Assets/Scripts/UI/StandardWorkCenterView.cs
Assets/Scripts/UI/SkillNavItemView.cs
Assets/Scripts/UI/ActionCardView.cs
Assets/Scripts/UI/InventoryPanelView.cs
Assets/Scripts/UI/InventoryGridView.cs
Assets/Scripts/UI/LootPreviewView.cs
Assets/Scripts/UI/LootDropSlotView.cs
Assets/Scripts/UI/ScavengeDetailView.cs
Assets/Scripts/UI/UITheme.cs
Assets/Scripts/Game/GameSession.cs
Assets/Scripts/Game/GameContent.cs
```

## 场景手配要点

在 `Demo.unity`（或你的主场景）里直接搭层级并拖引用：

| 组件 | 挂哪里 | 要拖的引用 |
|------|--------|------------|
| `GameSession` | `App` | — |
| `MainUIController` | `App` | `skillItems`、`workCenterHost`、背包按钮/面板 |
| `WorkCenterHost` | `App/Body/Center` | 各 `WorkView_*` 子物体 |
| `StandardWorkCenterView` | 各 `WorkView_*` | Banner、ActionCards、RunningBar 等 |
| `ScavengeDetailView` | `WorkView_scavenge/Detail` | 标题、正文、`Btn_工作`、`RunningBar`、`LootPreviewView` |
| `SkillNavItemView` | 左栏每项 | `workId`、高亮状态 |
| `ActionCardView` | 动作卡预制/实例 | 标题、元信息、Thumb |
| `InventoryPanelView` | 背包面板 | Grid、关闭按钮 |
| `LootPreviewView` | `WorkView_scavenge/Detail/掉落预览` | `slotPrefab` → `GameResources/Prefab/掉落slot.prefab` |

布局（Grid Cell、Banner 高度、栏宽等）**以预制体/场景为准**，在 Inspector / RectTransform 里手调；Agent **默认只改脚本**，预制体由你改（见 `.cursor/rules/UI-手配预制体.mdc`）。

## 左栏工作（当前）

**拾荒**（萤溪村）、**砍树**（黑松林）、**挖矿**（坠星矿洞）、**魔物探索**（坠星野外）— 左栏切换；玩法见 [玩法-拾荒](玩法-拾荒.md)、[玩法-砍树](玩法-砍树.md)、[玩法-挖矿](玩法-挖矿.md)、[玩法-魔物探索](玩法-魔物探索.md)。

## 依赖

- TextMeshPro、uGUI
- `UniverIdle.Game`（挂机与内容表）

## 扩展指南

| 要做的事 | 改哪里 |
|----------|--------|
| 加工作项 | 场景左栏加 `SkillNavItemView` + `GameContent` 注册 |
| 加动作卡 | 在 `ActionCards` 下复制卡片并绑 `ActionCardView` |
| 接新工作逻辑 | `GameContent` 注册表；`MainUIController` 已通用 |

## 已知限制

- **右侧详情**：仅拾荒 `WorkView_scavenge` 含 `Detail`（含进度条 `RunningBar`）；由 `ScavengeDetailView` 驱动
- 顶栏图鉴/背包/设置部分按钮无逻辑

## 变更记录

| 日期 | 变更 |
|------|------|
| 2026-09-02 | 拾荒进度条迁入 `Detail/RunningBar`，由 `ScavengeDetailView` 驱动 |
| 2026-09-02 | 右侧详情从 `MainUIController` 迁至 `WorkView_scavenge/ScavengeDetailView` |
| 2026-09-02 | 约定 UI 以预制体手配为准；掉落预览 + `掉落slot` 预制体 |
| 2026-09-02 | 冗余清理：删生成器接线 API、UITheme 死色、GetSceneGroup、场景分组缓存等 |
| 2026-09-02 | 移除 Editor 一键重建 / 布局调参；改场景手配 |
| 2026-09-01 | 接拾荒挂机；底栏改动态背包；左栏收窄为拾荒 |
