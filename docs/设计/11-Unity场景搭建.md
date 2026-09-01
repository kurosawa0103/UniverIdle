# 底层与场景搭建

> 最后更新：2026-09-01

## Unity 目录

```
Assets/
├── Editor/
│   └── MainUISetup.cs          # 菜单一键生成主界面
├── Scripts/UI/
│   ├── UITheme.cs              # 配色（对齐概念 HTML）
│   ├── MainUIController.cs     # 主界面交互
│   ├── SkillNavItemView.cs     # 左栏技能项
│   └── ActionCardView.cs       # 动作卡片
├── UI/
│   ├── Art/主界面-概念图.png    # 地点横幅占位图
│   └── Fonts/                  # 运行 Setup 后生成中文字体 TMP
└── Scenes/
    └── SampleScene.unity       # 在此场景生成 UI
```

## 在场景里生成界面

1. 用 Unity 2022.3 打开项目 `D:\UniverIdle`
2. 打开 `Assets/Scenes/SampleScene.unity`
3. 菜单栏：**UniverIdle → 创建主界面（当前场景）**
4. 保存场景（Ctrl+S）
5. 点击 Play 预览

会创建根节点 `UniverIdle_MainUI`，布局与 `docs/设计/概念图/主界面-概念.html` 一致：

| 区域 | 尺寸/说明 |
|------|-----------|
| 画布参考 | 1920×1080 ScaleWithScreenSize |
| 应用面板 | 1200×680 居中 |
| 顶栏 | 52px |
| 左技能栏 | 172px × 7 项 |
| 右详情 | 228px |
| 底物品栏 | 76px |

## 运行时

- `MainUIController`：点击技能切换选中态；点击动作卡更新详情与进度条（占位数据）
- 挂机逻辑、存档等 **尚未接入** — 下一步接 `GameLoop` 层

## 重新生成

再次执行菜单会 **删除** 场景中旧的 `UniverIdle_MainUI` 并重建（方便迭代排版）。

## 关联

- [主界面与交互](../设计/09-主界面与交互.md)
- [UI视觉风格](../设计/10-UI视觉风格.md)
