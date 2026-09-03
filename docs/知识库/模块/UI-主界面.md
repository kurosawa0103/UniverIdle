# UI-01 主界面（UGUI）

> 状态：**四工作可切换 + 背包弹层** · 更新：2026-09-03

## 职责

- 提供与 [02-界面](../../设计/02-界面.md) 一致的 **PC 主界面布局**（概念图仍是目标画风；工程以预制体色块为准）
- 左栏工作导航、中栏（拾荒有地图；砍树是动作列表）、右栏详情、顶栏背包弹层
- 运行时：切换工作、选动作挂机、各 Center 自带进度条、背包刷新

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
Assets/Scripts/UI/WorkCenterHubView.cs
Assets/Scripts/UI/StandardWorkCenterView.cs
Assets/Scripts/UI/ActionListWorkCenterView.cs
Assets/Scripts/UI/SkillNavItemView.cs
Assets/Scripts/UI/ActionCardView.cs
Assets/Scripts/UI/InventoryPanelView.cs
Assets/Scripts/UI/InventoryGridView.cs
Assets/GameResources/Prefab/UniverIdle_MainUI.prefab
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
| `WorkCenterHubView` | `WorkView_scavenge`（工作根） | `detailPanel` → `Detail` |
| `StandardWorkCenterView` | **拾荒地图节点**（如 `Content/村口`）；挖矿/魔物目前挂在工作根（`sceneId` 空） | `workId`、`sceneId`（村口填 `gate`）、该节点动作卡、**本 Center 的 `RunningBar`** |
| `ActionListWorkCenterView` | `WorkView_woodcutting` 工作根 | `workId=woodcutting`、动作卡、中栏进度条；点卡开始/停止 |
| `ScavengeDetailView` | `WorkView_scavenge/Detail`（砍树详情也可复用 toast/预览） | 标题、正文、拾荒 `Btn_工作`、`LootPreviewView`；**不管进度条** |
| `SkillNavItemView` | 左栏每项 | `workId`、高亮状态 |
| `ActionCardView` | 动作卡预制/实例 | 标题、元信息、Thumb |
| `InventoryPanelView` | `InventoryOverlay` | 见 [UI-背包](UI-背包.md) |
| `LootPreviewView` | `Detail/掉落预览` | `slotPrefab` → `GameResources/Prefab/掉落slot.prefab` |

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
| 加动作卡 | 在对应地图节点下复制卡片并绑 `ActionCardView` |
| 接新工作逻辑 | `GameContent` 注册表；`MainUIController` 已通用 |

## 已知限制

- **进度条**：由当前 Center（`StandardWorkCenterView` / `ActionListWorkCenterView`）驱动自己的 `RunningBar`，详情不再 Share/Hide 进度
- **获得提示**：`ScavengeDetailView` 的 `lootToast` 预制体常为空，运行时仍会 `new`「获得提示区」
- **砍树**：无地图 Tags；点卡即开停；需手配满表内动作卡（现 5 棵树）
- 顶栏图鉴/设置按钮无逻辑；背包见 [UI-背包](UI-背包.md)
- 本地存档见 [SAVE-存档](SAVE-存档.md)；离线收益尚未做

## 变更记录

| 日期 | 变更 |
|------|------|
| 2026-09-03 | 本地 JSON 存档；菜单 UniverIdle/GM 可重置 |
| 2026-09-03 | 背包页签手配进预制体；砍运行时造 Tabs；详情不再接管进度条 |
| 2026-09-03 | 砍树改 `ActionListWorkCenterView`：工作根直接摆动作卡，不走拾荒地图 |
| 2026-09-02 | 拾荒进度条曾迁入 `Detail/RunningBar`（已改回 Center 自管） |
| 2026-09-02 | 右侧详情从 `MainUIController` 迁至 `WorkView_scavenge/ScavengeDetailView` |
| 2026-09-02 | 约定 UI 以预制体手配为准；掉落预览 + `掉落slot` 预制体 |
| 2026-09-02 | 冗余清理：删生成器接线 API、UITheme 死色、GetSceneGroup、场景分组缓存等 |
| 2026-09-02 | 移除 Editor 一键重建 / 布局调参；改场景手配 |
| 2026-09-01 | 接拾荒挂机；底栏改动态背包；左栏收窄为拾荒 |
