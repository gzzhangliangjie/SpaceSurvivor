# AGENTS.md — 开发规范

本文件是面向 AI Agent 与开发者的项目基础规范。修改代码前请先阅读本文件。

## 1. 项目概述

- **项目**：DOTS Survivors — 太空主题生存类游戏
- **引擎**：Unity 6（6000.0.30f1，见 ProjectSettings/ProjectVersion.txt）
- **核心包**：Entities 1.0+、Burst 1.8+、URP、Input System
- **语言**：C#（大量使用 DOTS/ECS 架构）

## 2. 目录结构

```
Assets/DOTSSurvivors/
├── Scripts/            # 所有运行时代码（按模块分目录）
│   ├── Player/         # 玩家逻辑
│   ├── PlayerAttacks/  # 武器与攻击系统
│   ├── EnemyLogic/     # 敌人 AI 与生成
│   ├── Transformations/# 实体行为组件（Authoring）
│   ├── StatModifiers/  # 属性修改系统
│   ├── Items/          # 物品掉落
│   ├── UI/             # UI 桥接（事件驱动）
│   ├── Input/          # 输入处理
│   ├── VFX/ SFX/       # 特效与音效
│   └── General/        # 通用工具
├── Config/             # 运行时配置
├── ScriptableObjects/  # SO 配置资产
├── Prefabs/            # 预制体
├── Scenes/             # 场景
└── Editor/             # 编辑器专用代码
```

## 3. C# 代码规范

### 3.1 命名

| 类型 | 规则 | 示例 |
|------|------|------|
| 类/结构体/枚举 | PascalCase | `PlayerAttackSystem` |
| 接口 | `I` 前缀 | `IStatModifier` |
| 私有字段 | `_camelCase` | `_currentHealth` |
| 公共字段/属性 | PascalCase | `MoveSpeed` |
| 方法 | PascalCase，动词开头 | `ApplyDamage()` |
| 常量 | PascalCase | `MaxEnemyCount` |
| 文件名 | 与类名一致，一个文件一个主要类 | `BoomerangMovementAuthoring.cs` |

### 3.2 通用要求

- 缩进：4 空格；大括号：K&R / Allman 均可，但同一文件内保持一致（跟随现有文件风格）。
- 优先复用已有模块，禁止复制粘贴相似系统后再另起一套。
- 异常处理：DOTS 代码中**禁止抛出/捕获异常**（Burst 不支持），用条件检查 + Debug 日志代替。
- 每次修改后确保无编译错误、无多余 using。

## 4. DOTS/ECS 专项规范（重点）

### 4.1 结构约定

- **System**：`ISystem`（Burst 编译）优先，仅在需要托管资源（GameObject、字符串、事件）时才用 `SystemBase`。
  - 命名：`XxxSystem`，放在对应模块目录。
  - `[BurstCompile]` 必须标注在结构体和 `OnUpdate` 上。
- **Component**：`IComponentData`（纯数据），需要逻辑时用 `IComponentData` + `ISystem` 或接口实现；禁止在组件中放托管引用（类、string、事件）。
- **Authoring**：`XxxAuthoring : MonoBehaviour` + `XxxBaker : Baker<XxxAuthoring>`，用于把 Inspector 数据烘焙为组件。
- **Tag 组件**：空结构体实现 `IComponentData`，命名以 `Tag` 结尾或语义明确。

### 4.2 性能红线

- **零 GC 分配**：主游戏循环中禁止 `new` 托管对象、LINQ、boxing、字符串拼接。
- 容器一律使用 `NativeArray` / `NativeList` / `NativeHashMap` 等，必须显式 `Dispose()` 或用 `Allocator.TempJob` + 正确的所有权。
- Job 中只访问已声明的 RW/RO 组件，避免竞态；谨慎使用 `[NativeDisableParallelForRestriction]`，必须写注释说明为什么安全。
- 实体查询/过滤通过 `EntityQuery` 与组件标签完成，不要每帧手动遍历所有实体。
- 需要立即结构变更（创建/销毁实体）时使用 `ECB`（`EntityCommandBuffer`），并行 Job 中使用 `ParallelWriter`。

### 4.3 UI 与事件桥接

- ECS 世界与 UI/GameObject 世界之间**只通过事件桥**通信（发布-订阅），禁止在 System 里直接操作 UI 组件。
- UI 更新必须数据驱动：仅在值变化时触发，不做每帧轮询刷新。

## 5. ScriptableObject 配置

- 数据配置统一走 SO：`CharacterProperties`、`EnemySpawnEvents`、`UpgradeProperties`、`StageProperties` 等。
- 新增配置类型放 `Assets/DOTSSurvivors/ScriptableObjects/`，代码类放对应 Scripts 模块。
- 配置资产命名：`类型_用途`（如 `Upgrade_Jetpack`）。
- 修改 SO 字段需向后兼容：优先新增字段并给默认值，避免删除/改名导致资产数据丢失。

## 6. 资源与 Git 规范

- **.meta 文件**：每个新增的资产/目录必须带 .meta；禁止删除或手工修改已有 .meta（会破坏 GUID 引用）。
- **资源命名**：PascalCase，`类型_名称`（如 `Icon_Laser.png`、`AstronautBlue-Icon.png`，跟随现有风格）。
- **不要提交**：`Library/`、`Logs/`、`UserSettings/`、`obj/`、临时截图（如 `_playmode_shot.png`）。
- 提交信息：简洁祈使句，说明做了什么，如 `Fix rocket homing overshoot near target`。
- 大改动拆成多个小提交，每个提交保持可编译。

## 7. 修改流程（Agent 工作流）

1. **先看后写**：修改前阅读目标模块的现有代码，遵循该模块的既有风格与模式。
2. **最小改动**：只改完成任务必需的部分，不顺手重构、不重命名公共 API。
3. **编译验证**：改动后确认代码能编译（可借助 Unity 编辑器或 csproj 检查引用是否正确）。
4. **模式匹配**：新增武器/行为/敌人时，优先模仿现有同类实现（如新移动方式参考 `Transformations/` 下现有 Authoring + System 的配对写法）。
5. **不动的部分**：`ProjectSettings/`、URP 全局配置、输入动作资产除非任务明确要求，否则不要修改。

## 8. 运行与测试

- 主场景位于 `Assets/DOTSSurvivors/Scenes/`，用 Unity 打开项目后直接 Play。
- 编辑器扩展代码放 `Assets/DOTSSurvivors/Editor/` 或 `Assets/Editor/`，编译目标仅限 Editor 平台。
- 修改游戏逻辑后，重点回归验证：玩家攻击、敌人生成、升级属性生效、UI 数值同步。
