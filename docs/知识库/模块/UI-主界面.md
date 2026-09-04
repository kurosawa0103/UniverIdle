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
| `ScavengeHubView` | `WorkView_scavenge` | `detailPanel` → 拾荒 `Detail`（`ScavengeDetailView`，**无 GetComponent 兜底**） |
| `StandardWorkCenterView` | **拾荒地图节点**（如 `Content/村口`）；挖矿/魔物可挂工作根 | `workId`、`sceneId`、动作卡；**必拖** `runningBarRoot` + fill/文案；挖矿等多地区时 **必拖** `sceneTagsRoot`（**无** `Find("Tags")`） |
| `ActionListWorkCenterView` | `WorkView_woodcutting` | `workId`、动作卡、**必拖** `runningBarRoot` + fill/文案、`detailPanel` → 砍树 `Detail`（**无** Find RunningBar） |
| 进度条 | 各工作 `RunningBar`；复用预制体 `进度条.prefab` | **必绑** `BarFill`→`ItemIcon/ui_progress_fill`（实心）、`BarBg`→`ui_progress_track`；菜单 **UniverIdle → 安装进度条预制体** |
| `ScavengeDetailView` | **仅** `WorkView_scavenge/Detail` | 标题、正文、`Btn_工作` + `workButtonText`、`LootPreviewView`（**无**按钮文案 InChildren 兜底） |
| `WorkActionDetailView` | `WorkView_woodcutting/Detail` 等 | 标题、正文、掉落预览；**无**开始按钮 |
| `SkillNavItemView` | 左栏每项 | `workId`、高亮状态 |
| `ActionCardView` | 动作卡 | 标题、元信息、`thumbArt`（动作图）、`unlockText`、**`button`（`ClickButton`）**、`MasteryIcon` / `MasteryLevel`；Thumb 底板由预制体静态 Image，脚本不绑 `thumb`；Center Wire 用 `ClickButton`，不 `GetComponent` |
| `LootToastView` | **`App` 根下「获得提示区」**（勿挂 `WorkView_*` / Detail） | **必拖** `lineRoot` / `floatLayer` / `linePrefab`（`获得提示.prefab`）/ `floaterPrefab`（`获得提示飘字.prefab`）；`MainUI.lootToast` 只拖此节点；结算可推道具 / 金币 / **经验**（`ItemIconLoader.GetXp()` → `ui_xp`）；经验右侧为本轮挂机累计，停机清零 |
| `LootToastLineView` | `获得提示.prefab` | `icon` / 文案 + **`row` → `Row`**（布局必拖） |
| `InventoryPanelView` | `InventoryOverlay` | 见 [UI-背包](UI-背包.md)；`pageTabs` 必拖（**不**扫 `tabRoot`） |
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
| 加工作项 | 场景左栏加 `SkillNavItemView` + `GameContent` 注册；再跑一键绑定或手拖 |
| 加动作卡 | 复制卡并绑 `ActionCardView`；缺 Mastery 可再跑一键绑定 |
| 接新工作逻辑 | `GameContent` 注册表；`MainUIController` 已通用 |

## 已知限制

- **进度条**：`进度条.prefab` + `ui_progress_fill` / `ui_progress_track`；MainUI 内 `BarFill` 必须有 sprite（`Image.Type.Filled` 无图不滚）；运行时仅 Resources 兜底，**禁止**再依赖 `Texture2D.whiteTexture`；菜单 `UniverIdle/安装进度条预制体` 可重建预制体并写回 MainUI
- **停机**：`ActionRunner.Stop()` 与材料不足停机同一条路径，均发 `OnActionStopped` → Center `OnRunnerActionStopped`
- **砍树列表**：结算只刷详情；卡表靠背包 / 总等级·熟练度变更 `Refresh`，避免每轮双绑
- **获得提示**：全局 overlay，挂 `App`；行·飘字预制体只绑在 `LootToastView` 上；切工作不隐藏；错挂 WorkView 时一键绑定只挪父级、**不改**你调好的锚点/位置（仅新建才用默认占位）；行须绑 `row`；道具/金币/经验行共用 `RefreshGainLine`；经验右侧为**本轮挂机累计**（非 `当前/升级`），`OnActionStopped` → `ResetSessionXp`
- **砍树**：无地图节点；点卡即开停；详情用 `WorkActionDetailView`，与拾荒 `ScavengeDetailView` 分离
- **顶栏金币**：`TopBar/Currency` + `TopBarGoldView`；图鉴/设置按钮无逻辑；背包见 [UI-背包](UI-背包.md)
- 本地存档见 [SAVE-存档](SAVE-存档.md)（默认 10 秒自动存）；离线收益尚未做
