# UI-01 主界面（UGUI）

> 状态：**四工作可切换 + 背包弹层** · 更新：2026-09-04

## 职责

- 提供与 [02-界面](../../设计/02-界面.md) 一致的 **PC 主界面布局**（概念图仍是目标画风；工程以预制体色块为准）
- 左栏工作导航、中栏（拾荒有地图；砍树是动作列表）、右栏详情、顶栏金币与背包弹层
- 运行时：切换工作、选动作挂机、各 Center 自带进度条、背包刷新

## 入口

| 类型 | 路径 |
|------|------|
| 场景根节点 | `UniverIdle_MainUI`（在场景中手配） |
| 运行时控制器 | `MainUIController` + `GameSession` on `App` |
| 一键绑定 | 菜单 `UniverIdle/一键绑定主界面引用` → `Assets/Editor/UI/MainUIBindMenu.cs` |

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
Assets/Resources/Prefab/UniverIdle_MainUI.prefab
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
Assets/Scripts/Game/ItemIconLoader.cs
Assets/Scripts/Game/SceneProgressRules.cs
Assets/Editor/UI/MainUIBindMenu.cs
```

## 场景手配要点

在 `Demo.unity`（或你的主场景）里直接搭层级并拖引用；也可选中 MainUI 后跑 **一键绑定**（缺「获得提示区 / Mastery」会在编辑器创建）：

| 组件 | 挂哪里 | 要拖的引用 |
|------|--------|------------|
| `GameSession` | `App` | — |
| `MainUIController` | `App` | `skillItems`、`workCenterHost`、`inventoryButton`（`Btn_背包`）、`inventoryPanel`、`topBarGold`、`lootToast` / 行·飘字预制体（**无运行时兜底**） |
| `TopBarGoldView` | `TopBar/Currency` | **必拖** `icon` → `Icon`、`amountText` → `Text` |
| `WorkCenterHost` | `App/Body/Center` | 各 `WorkView_*` 子物体 |
| `ScavengeHubView` | `WorkView_scavenge` | `detailPanel` → 拾荒 `Detail`（`ScavengeDetailView`，**无 GetComponent 兜底**） |
| `StandardWorkCenterView` | **拾荒地图节点**（如 `Content/村口`）；挖矿/魔物可挂工作根 | `workId`、`sceneId`、动作卡、本 Center `RunningBar` |
| `ActionListWorkCenterView` | `WorkView_woodcutting` | `workId`、动作卡、中栏进度条、`detailPanel` → 砍树 `Detail`（**无兜底**） |
| `ScavengeDetailView` | **仅** `WorkView_scavenge/Detail` | 标题、正文、`Btn_工作` + `workButtonText`、`LootPreviewView`（**无**按钮文案 InChildren 兜底） |
| `WorkActionDetailView` | `WorkView_woodcutting/Detail` 等 | 标题、正文、掉落预览；**无**开始按钮 |
| `SkillNavItemView` | 左栏每项 | `workId`、高亮状态 |
| `ActionCardView` | 动作卡 | 标题、元信息、Thumb、`unlockText`、`MasteryIcon` / `MasteryLevel`（**无**运行时按名扫；缺引用跑一键绑定）；熟练度五角星：1–30 铜、31–70 银、71+ 金 |
| `LootToastView` | **`App` 根下「获得提示区」**（勿挂 `WorkView_*` / Detail） | `lineRoot` / `floatLayer` / 行·飘字预制体；`MainUI.lootToast` 拖此节点；切工作仍显示 |
| `LootToastLineView` | `获得提示.prefab` | `icon` / 文案 + **`row` → `Row`**（布局必拖） |
| `InventoryPanelView` | `InventoryOverlay` | 见 [UI-背包](UI-背包.md)；`pageTabs` 必拖（**不**扫 `tabRoot`） |
| `LootPreviewView` | `Detail/掉落预览` | `slotPrefab` → `掉落slot.prefab` |
| `InventoryGridView` | 背包 Body | `slotPrefab` → `背包slot.prefab` |

布局以预制体/场景为准；Agent **默认只改脚本**，预制体由你改（见 `.cursor/rules/UI-手配预制体.mdc`）。

## 左栏工作（当前）

**拾荒**（萤溪村）、**砍树**（黑松林）、**挖矿**（坠星矿洞）、**魔物探索**（坠星野外）— 左栏切换；玩法见对应模块文档。

## 依赖

- TextMeshPro、uGUI
- `UniverIdle.Game`（挂机与内容表）；图标统一 `ItemIconLoader`（含 `GetGold()`、按等级 `GetMastery()`）

## 扩展指南

| 要做的事 | 改哪里 |
|----------|--------|
| 加工作项 | 场景左栏加 `SkillNavItemView` + `GameContent` 注册；再跑一键绑定或手拖 |
| 加动作卡 | 复制卡并绑 `ActionCardView`；缺 Mastery 可再跑一键绑定 |
| 接新工作逻辑 | `GameContent` 注册表；`MainUIController` 已通用 |

## 已知限制

- **进度条**：由当前 Center 驱动自己的 `RunningBar`，详情不管进度
- **获得提示**：全局 overlay，挂 `App`（与 `MainUIController` 同级子树），**不要**放进拾荒/砍树 Detail；运行时若仍挂在 WorkView 下会自动挪到 App；或跑一键绑定挪位；行预制体须绑 `row`
- **砍树**：无地图节点；点卡即开停；详情用 `WorkActionDetailView`，与拾荒 `ScavengeDetailView` 分离
- **顶栏金币**：`TopBar/Currency` + `TopBarGoldView`；图鉴/设置按钮无逻辑；背包见 [UI-背包](UI-背包.md)
- 本地存档见 [SAVE-存档](SAVE-存档.md)（默认 10 秒自动存）；离线收益尚未做
