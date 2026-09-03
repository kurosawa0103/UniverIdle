# UI-01 主界面（UGUI）

> 状态：**四工作可切换 + 背包弹层** · 更新：2026-09-03

## 职责

- 提供与 [02-界面](../../设计/02-界面.md) 一致的 **PC 主界面布局**（概念图仍是目标画风；工程以预制体色块为准）
- 左栏工作导航、中栏（拾荒有地图；砍树是动作列表）、右栏详情、顶栏金币与背包弹层
- 运行时：切换工作、选动作挂机、各 Center 自带进度条、背包刷新

## 入口

| 类型 | 路径 |
|------|------|
| 场景根节点 | `UniverIdle_MainUI`（在场景中手配） |
| 运行时控制器 | `MainUIController` + `GameSession` on `App` |

## 文件清单

```
Assets/Scripts/UI/MainUIController.cs
Assets/Scripts/UI/WorkCenterHost.cs
Assets/Scripts/UI/ScavengeHubView.cs
Assets/Scripts/UI/StandardWorkCenterView.cs
Assets/Scripts/UI/ActionListWorkCenterView.cs
Assets/Scripts/UI/SkillNavItemView.cs
Assets/Scripts/UI/ActionCardView.cs
Assets/Scripts/UI/InventoryPanelView.cs
Assets/Scripts/UI/InventoryGridView.cs
Assets/Scripts/UI/InventorySlotView.cs
Assets/GameResources/Prefab/UniverIdle_MainUI.prefab
Assets/Resources/Prefab/背包slot.prefab
Assets/Resources/Prefab/掉落slot.prefab
Assets/Scripts/UI/LootPreviewView.cs
Assets/Scripts/UI/LootDropSlotView.cs
Assets/Scripts/UI/WorkActionDetailView.cs
Assets/Scripts/UI/ScavengeDetailView.cs
Assets/Scripts/UI/TopBarGoldView.cs
Assets/Scripts/UI/UITheme.cs
Assets/Scripts/Game/GameSession.cs
Assets/Scripts/Game/GameContent.cs
Assets/Scripts/Game/SceneProgressRules.cs
```

## 场景手配要点

在 `Demo.unity`（或你的主场景）里直接搭层级并拖引用：

| 组件 | 挂哪里 | 要拖的引用 |
|------|--------|------------|
| `GameSession` | `App` | — |
| `MainUIController` | `App` | `skillItems`、`workCenterHost`、背包按钮/面板、`topBarGold`（可空则找子节点上的组件） |
| `TopBarGoldView` | `TopBar/Currency` | **必拖** `icon` → `Icon`、`amountText` → `Text`（无按名兜底） |
| `WorkCenterHost` | `App/Body/Center` | 各 `WorkView_*` 子物体 |
| `ScavengeHubView` | `WorkView_scavenge`（拾荒工作根） | `detailPanel` → 拾荒 `Detail`（`ScavengeDetailView`） |
| `StandardWorkCenterView` | **拾荒地图节点**（如 `Content/村口`）；挖矿/魔物目前挂在工作根（`sceneId` 空） | `workId`、`sceneId`（村口填 `gate`）、该节点动作卡、**本 Center 的 `RunningBar`** |
| `ActionListWorkCenterView` | `WorkView_woodcutting` 工作根 | `workId=woodcutting`、动作卡、中栏进度条、`detailPanel` → 砍树 `Detail`（`WorkActionDetailView`）；点卡开始/停止 |
| `ScavengeDetailView` | **仅** `WorkView_scavenge/Detail` | 标题、正文、拾荒 `Btn_工作`、`LootPreviewView`；**不管进度条**；不挂到砍树 |
| `WorkActionDetailView` | `WorkView_woodcutting/Detail`（及其它动作列表工作） | 标题、正文、掉落预览、获得提示；**无**开始按钮 |
| `SkillNavItemView` | 左栏每项 | `workId`、高亮状态 |
| `ActionCardView` | 动作卡预制/实例 | 标题、元信息、Thumb |
| `InventoryPanelView` | `InventoryOverlay` | 见 [UI-背包](UI-背包.md) |
| `LootPreviewView` | `Detail/掉落预览` | `slotPrefab` → `Resources/Prefab/掉落slot.prefab`（含 Icon / Unknown） |
| `InventoryGridView` | 背包 Body | `slotPrefab` → `Resources/Prefab/背包slot.prefab` |

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

- **进度条**：由当前 Center（`StandardWorkCenterView` / `ActionListWorkCenterView`）驱动自己的 `RunningBar`，详情不管进度
- **获得提示**：详情上 `lootToast` 常为空，运行时仍会 `new`「获得提示区」
- **砍树**：无地图节点；点卡即开停；详情用 `WorkActionDetailView`，与拾荒 `ScavengeDetailView` 分离
- **顶栏金币**：`TopBar/Currency` + `TopBarGoldView`；图鉴/设置按钮无逻辑；背包见 [UI-背包](UI-背包.md)
- 本地存档见 [SAVE-存档](SAVE-存档.md)（默认 10 秒自动存）；离线收益尚未做
