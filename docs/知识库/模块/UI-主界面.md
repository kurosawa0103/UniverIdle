# UI-01 主界面（UGUI）

> 状态：**已接拾荒挂机** · 更新：2026-09-01

## 职责

- 提供与 [02-界面](../../设计/02-界面.md) / [主界面-概念.html](../../设计/概念图/主界面-概念.html) 一致的 **PC 主界面布局**
- 左栏工作导航、中栏地点横幅 + 动作卡 + 进度、右栏详情、底栏物品
- 运行时：切换工作、选动作挂机、进度条、背包刷新

## 入口

| 类型 | 路径 |
|------|------|
| 生成菜单 | Unity 菜单 **UniverIdle → 创建主界面（当前场景）** |
| 场景根节点 | `UniverIdle_MainUI` |
| 运行时控制器 | `MainUIController` + `GameSession` on `App` |

## 文件清单

```
Assets/Editor/MainUISetup*.cs     # 一键生成 UI 层级
Assets/Scripts/UI/MainUIController.cs
Assets/Scripts/UI/InventoryBarView.cs
Assets/Scripts/UI/SkillNavItemView.cs
Assets/Scripts/UI/ActionCardView.cs
Assets/Scripts/UI/UITheme.cs
Assets/Scripts/Game/GameSession.cs
Assets/Scripts/Game/GameContent.cs
Assets/Res/fonts/unifont-15.asset
```

## 布局参数

| 项 | 值 |
|----|-----|
| Canvas | Screen Space Overlay，全屏 |
| 顶栏 / 底栏 | 52px / 76px |
| 左栏 / 右栏 | 172px / 228px |

## 左栏工作（当前）

**拾荒**（萤溪村）、**砍树**（黑松林）、**魔物探索**（坠星野外）— 左栏切换；玩法见 [玩法-拾荒](玩法-拾荒.md)、[玩法-砍树](玩法-砍树.md)、[玩法-魔物探索](玩法-魔物探索.md)。

## 依赖

- TextMeshPro、uGUI
- `UniverIdle.Game`（挂机与内容表）

## 扩展指南

| 要做的事 | 改哪里 |
|----------|--------|
| 加工作项 | `MainUISetup.AddSkillNav` + `GameContent` |
| 加动作卡槽位 | `CreateActionCards` 数量 |
| 接新工作逻辑 | `GameContent` 注册表；`MainUIController` 已通用 |

## 已知限制

- 需重新执行 **创建主界面** 才能更新场景层级（旧场景无 `GameSession` / 动态背包）
- 顶栏图鉴/背包/设置无逻辑

## 变更记录

| 日期 | 变更 |
|------|------|
| 2026-09-01 | 接拾荒挂机；底栏改动态背包；左栏收窄为拾荒 |
