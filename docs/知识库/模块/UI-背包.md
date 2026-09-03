# UI-02 背包

> 状态：**已实现（无存档）** · 更新：2026-09-03

## 职责

- 顶栏打开背包弹层：分页格子、金币解锁下一页/下一格
- 物品进包受格子上限约束；满包时挂机掉落跳过并 toast

## 入口

| 类型 | 路径 |
|------|------|
| 预制体 | `Assets/GameResources/Prefab/UniverIdle_MainUI.prefab` → `InventoryOverlay` |
| 面板脚本 | `InventoryPanelView`（挂在 Overlay 上） |
| 格子脚本 | `InventoryGridView`（Body 内） |
| 容量表 | `Assets/StreamingAssets/Game/inventory.json` |
| 运行时 | `GameContent.Inventory`、`PlayerState` |

## 文件清单

```
Assets/Scripts/UI/InventoryPanelView.cs
Assets/Scripts/UI/InventoryGridView.cs
Assets/Scripts/Game/Data/InventoryBagDefinition.cs
Assets/Scripts/Game/Data/GameDataFile.cs   （InventoryBagDataFile）
Assets/StreamingAssets/Game/inventory.json
```

## 容量（当前表）

手改 JSON 即可（**没有对应 Excel**；缺文件时用 `InventoryBagDefinition.CreateDefault()`，数值应与 JSON 一致）。

| 项 | 值 |
|----|----|
| 页数 × 每页格 | 4 × 20 |
| 开局免费格 | 10（第 1 页） |
| 解锁第 2/3/4 页 | 50 / 150 / 400 金 |
| 解锁下一格 | `15 + 8 × (已解锁格 − 免费格)` |

- 开局：1 页已开、10 格可用
- 未解锁页仍可点页签查看；点该页可解锁格（「解锁本页 N金」）才扣金币开页
- 已开页内点「解锁 N金」开下一格；格满后新物品 `TryAddItem` 失败

## 场景手配要点

Agent **默认不改 prefab**。在 `InventoryOverlay/Panel`：

| 节点 | 组件 / 引用 |
|------|-------------|
| Overlay 根 | `InventoryPanelView`：`overlayRoot`、`grid`、`closeButton`、`backdropButton`、`tabRoot`、`pageTabs`（Tab_1～4 的 Button）、`pageLabelText`（`Text_页码`）、`goldText`（`Text_金币`） |
| `Panel/Tabs` | `Tab_1`～`Tab_4`、金币与格数 TMP |
| Body | `InventoryGridView`：`slotContainer`、`font` |

格子由 `InventorySlotView.Create` **运行时生成**（不手配 20 个 slot；与掉落预览的 `掉落slot.prefab` 不是同一套）。

代码**不**按节点名补造页签；漏拖引用则页签/关闭键无效。

## 依赖

- `MainUIController`：`Btn_背包` → `Toggle`；金币/物品变化时 `Refresh`
- `ActionRunner`：背包满则本跳掉落跳过 + toast「背包已满…」

## 扩展指南

| 要做的事 | 改哪里 |
|----------|--------|
| 改页数/价格 | `inventory.json`（同步改 `CreateDefault` 以免无文件时漂移） |
| 改页签外观 | 预制体 `Tabs` |
| 改单格外观 | `InventoryGridView` 内 `InventorySlotView.Create` |

## 已知限制

- 无存档：每次启动 `new PlayerState()`，解锁进度不保留
- 物品按名称排序铺进已解锁格，没有拖拽整理
- 预制体若仍序列化已删除的 `emptyLabel`，打开组件后会被 Unity 丢掉

## 变更记录

| 日期 | 变更 |
|------|------|
| 2026-09-03 | 分页 + 金币解锁；页签切页；砍运行时造 Tabs / 详情进度条双通道 |
