# UI-01 主界面（UGUI）

> 状态：**已搭建（无挂机逻辑）** · 更新：2026-09-01

## 职责

- 提供与 [02-界面](../../设计/02-界面.md) / [主界面-概念.html](../../设计/概念图/主界面-概念.html) 一致的 **PC 主界面布局**
- 左栏技能导航、中栏地点横幅 + 动作卡 + 进度、右栏详情、底栏物品
- 运行时：切换技能选中态、切换动作卡（占位数据）

## 入口

| 类型 | 路径 |
|------|------|
| 生成菜单 | Unity 菜单 **UniverIdle → 创建主界面（当前场景）** |
| 场景根节点 | `UniverIdle_MainUI` |
| 运行时控制器 | `MainUIController` on `App` |

## 文件清单

```
Assets/Editor/MainUISetup.cs       # 一键生成 UI 层级
Assets/Scripts/UI/UITheme.cs       # 配色常量
Assets/Scripts/UI/MainUIController.cs
Assets/Scripts/UI/SkillNavItemView.cs
Assets/Scripts/UI/ActionCardView.cs
Assets/Res/fonts/unifont-15.asset   # 主界面 TMP 默认字体
```

## 布局参数

| 项 | 值 |
|----|-----|
| Canvas | Screen Space Overlay，全屏拉伸，参考 1920×1080 |
| 根面板 `App` | 铺满画布 |
| 顶栏 / 底栏 | 56px / 80px |
| 左栏 / 右栏 | 180px / 240px |

## 左栏技能（当前占位 8 项）

打猎、**伐木**、溪钓、野拾、掘矿、炼药、锻造、讨伐

默认选中：**溪钓**（`MainUIController.activeSkillIndex = 2`）

## 依赖

- TextMeshPro（`com.unity.textmeshpro`）
- uGUI（`com.unity.ugui`）

## 扩展指南

| 要做的事 | 改哪里 |
|----------|--------|
| 加技能项 | `MainUISetup.AddSkillNav` 数据数组；后续改 ScriptableObject |
| 动作卡随技能切换 | `MainUIController.SelectSkill` 内刷新卡片列表（未做） |
| 接挂机逻辑 | 新建 `GAME-技能动作` 模块，由 Controller 订阅 |
| 伐木动作数据 | `GAME-伐木` 模块 + 配置表 |

## 已知限制

- 动作卡、详情文案为 **溪钓静态占位**，换技能只改横幅标题（`LocationName`）
- 顶栏按钮、背包格无逻辑

## 变更记录

| 日期 | 变更 |
|------|------|
| 2026-09-01 | 初版：Editor 生成 UI；修复 `CreateTMP` 传 `Image` 编译错误 |
| 2026-09-01 | 左栏增加 **伐木**（村外） |
