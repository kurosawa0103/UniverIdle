# UI-01 主界面（UGUI）

> 状态：**四工作可切换 + 背包弹层** · 更新：2026-09-04

## 职责

- 提供与 [02-界面](../../设计/02-界面.md) 一致的 **PC 主界面布局**（概念图仍是目标画风；工程以预制体色块为准）
- 左栏工作导航、中栏（拾荒/砍树为**同一套地图式界面**）、右栏详情、顶栏金币与背包弹层
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
Assets/Scripts/UI/WorkMapHubView.cs
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
Assets/Scripts/UI/WorkRunDetailView.cs
Assets/Scripts/UI/TopBarGoldView.cs
Assets/Scripts/UI/UITheme.cs
Assets/Scripts/Game/GameSession.cs
Assets/Scripts/Game/GameContent.cs
Assets/Scripts/Game/ItemIconLoader.cs
Assets/Scripts/Game/WorkActionRules.cs
Assets/Editor/UI/MainUIBindMenu.cs
```

## 场景手配要点

在 `Demo.unity`（或你的主场景）里直接搭层级并拖引用；也可选中 MainUI 后跑 **一键绑定**（缺「获得提示区 / Mastery」会在编辑器创建）：

| 组件 | 挂哪里 | 要拖的引用 |
|------|--------|------------|
| `GameSession` | `App` | — |
| `MainUIController` | `App` | `skillItems`、`workCenterHost`、`inventoryButton`（`Btn_背包`）、`inventoryPanel`、`topBarGold`、`lootToast`（**无**行·飘字预制体字段；**无运行时挪父级**） |
| `TopBarGoldView` | `TopBar/Currency` | **必拖** `icon` → `Icon`、`amountText` → `Text` |
| `WorkCenterHost` | `App/Body/Center` | 各 `WorkView_*` 子物体 |
| `WorkMapHubView` | `WorkView_scavenge` / `WorkView_woodcutting` 等地图式工作根 | `detailPanel` → 本工作 `Detail`；**必拖** `maps` → 各地图 `StandardWorkCenterView`（**无**运行时扫；一键绑定可补） |
| `StandardWorkCenterView` | **地图节点**（如 `Content/村口`）；挖矿/魔物可挂工作根 | `workId`、`sceneId`、动作卡；**必拖** `runningBarRoot` + fill/文案 |
| `ActionListWorkCenterView` | 纯列表工作（若仍用） | `workId`、动作卡、进度条、`detailPanel` → 无开工按钮的 `WorkActionDetailView` |
| 进度条 | 各工作 `RunningBar`；复用预制体 `进度条.prefab` | **必绑** `BarFill`→`ItemIcon/ui_progress_fill`（实心）、`BarBg`→`ui_progress_track`；子节点名约定 `Label` / `Time`；菜单 **UniverIdle → 安装进度条预制体** |
| `WorkRunDetailView` | 地图式工作的 `Detail`（拾荒/砍树共用脚本） | 标题、正文、`Btn_工作` + `workButtonText`、`LootPreviewView`；按钮文案取工作 `DisplayName` |
| `WorkActionDetailView` | 无开工按钮的详情 | 标题、正文、掉落预览 |
| `SkillNavItemView` | 左栏每项 | `workId`、高亮状态 |
| `ActionCardView` | 动作卡 | 标题、元信息、`thumbArt`、`unlockText`、**`button`**、`MasteryIcon` / `MasteryLevel`；Thumb 底板静态 Image |
| `LootToastView` | **`App` 根下「获得提示区」**（全游戏一份；**勿**挂 `WorkView_*` / Detail） | **必拖** `lineRoot` / `floatLayer` / `linePrefab` / `floaterPrefab` |
| `LootToastLineView` | `获得提示.prefab` | `icon` / 文案 + **`row` → `Row`** |
| `InventoryPanelView` | `InventoryOverlay` | 见 [UI-背包](UI-背包.md) |
| `LootPreviewView` | `Detail/掉落预览` | `slotPrefab` → `掉落slot.prefab` |
| `InventoryGridView` | 背包 Body | `slotPrefab` → `背包slot.prefab` |

布局以预制体/场景为准；Agent **默认只改脚本**，预制体由你改（见 `.cursor/rules/UI-手配预制体.mdc`）。

## 左栏工作（当前）

**拾荒**（萤溪村）、**砍树**（黑松林）、**挖矿**（坠星矿洞）、**魔物探索**（坠星野外）— 左栏切换；玩法见对应模块文档。

## 依赖

- TextMeshPro、uGUI
- `UniverIdle.Game`（挂机与内容表）；图标统一 `ItemIconLoader`（含 `GetGold()`、`GetXp()`、按等级 `GetMastery()`）

## 扩展指南

| 要做的事 | 改哪里 |
|----------|--------|
| 加地图式工作 | 复制 `WorkView_*` + `WorkMapHubView` / 地图 `StandardWorkCenterView` / `WorkRunDetailView`；配表注册；一键绑定补 `maps` |
| 加动作卡 | 复制卡并绑 `ActionCardView`；缺 Mastery 可再跑一键绑定 |
| 接新工作逻辑 | `GameContent` 注册表；`MainUIController` 已通用 |

## 已知限制

- **进度条**：`进度条.prefab` + `ui_progress_fill` / `ui_progress_track`；MainUI 内 `BarFill` 必须有 sprite；运行时仅 Resources 兜底，**禁止**再依赖 `Texture2D.whiteTexture`
- **停机**：`ActionRunner.Stop()` 与材料不足停机同一条路径，均发 `OnActionStopped` → Center `OnRunnerActionStopped`
- **获得提示**：全游戏一份，挂 `App`（勿进 WorkView/Detail）；一键绑定会挪到 App 并删多余副本
- **拾荒/砍树**：同一套地图 Hub + 开工详情脚本（`WorkMapHubView` / `WorkRunDetailView`），仅 `workId` 与配表不同
- **顶栏金币**：`TopBar/Currency` + `TopBarGoldView`；图鉴/设置按钮无逻辑；背包见 [UI-背包](UI-背包.md)
- 本地存档见 [SAVE-存档](SAVE-存档.md)（默认 10 秒自动存）；离线收益尚未做
