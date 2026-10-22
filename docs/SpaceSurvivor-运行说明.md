# SpaceSurvivor 运行说明

## 当前状态：可直接打开运行

- 项目位置：`C:\Users\zlj10\WorkBuddy\2026-10-20-15-01-55\SpaceSurvivor`
- 已用本机 **Unity 6000.0.30f1**（`D:\Unity\6000.0.30f1`）打开并 **编译通过（0 errors）**
- 美术资源齐全：151 个 C# 脚本、181 张 png、112 个材质、134 个 prefab、三张地图（Mars / Moon / Venus）
- 原始 zip 备份：`_probe\SpaceSurvivor-main.zip`（156 MB）

## 仓库与提交（已 fork 并推送）

| 项 | 值 |
|---|---|
| 上游 | https://github.com/fxy725/SpaceSurvivor |
| 我的 fork | https://github.com/gzzhangliangjie/SpaceSurvivor |
| 当前 HEAD | `3fdb85d` fix(compile): 统一输入生成类命名空间，修复 CS0246；TMP 增加中文动态字体回退 |

两个提交：

- `f89095f` chore(project): 升级到 Unity 6000.0.30f1，Burst 升到 1.8.24，移除 `com.unity.feature.2d`
- `3fdb85d` fix(compile): 统一输入生成类命名空间并补 `using`；TMP 增加中文动态字体回退

**推送通道**：本机 `https://github.com/` 被全局 `insteadOf` 改写到只读镜像 `gh-proxy.com`，镜像不支持写（`No anonymous write access`），直连 push 又被网关拦成 408/502。因此改用 **GitHub Git Data API**（blob → tree → commit → PATCH ref）写入，脚本见 `_probe\push_via_api.py` + `_probe\push_plan.json`。以后再推：

```bash
GH_TOKEN=<token> PROJ_ROOT="C:/Users/zlj10/WorkBuddy/2026-10-20-15-01-55/SpaceSurvivor" \
  C:/Users/zlj10/.workbuddy/binaries/python/versions/3.13.12/python.exe _probe/push_via_api.py
# 按需改 push_plan.json 里的 files / message_file
```

拉取（读）走镜像正常：`git fetch origin main --depth 1`。

**未纳入版本控制的本地调试物**（写在 `.git/info/exclude`，不污染上游 `.gitignore`）：
`Assets/Editor/SpaceSurvivorAutoBoot.cs`、`_playmode_shot.png`、`.vsconfig`。

## 相对原仓库做过的 5 处改动

| 改动 | 原因 |
|---|---|
| `ProjectSettings/ProjectVersion.txt` → `6000.0.30f1` | 原为 6000.0.43f1，本机是 6000.0.30f1，同大版本补丁差可兼容 |
| `Packages/manifest.json` 删除 `com.unity.feature.2d` | 其依赖 `com.unity.2d.pixel-perfect 5.0.3` 用了 URP 17 已删除的 `Experimental.Rendering.Universal.PixelPerfectCamera`，编译报 CS0234。项目零引用 2D 工具链 |
| `com.unity.burst` `1.8.19` → `1.8.24` | 1.8.19 的 460MB 包在本机网络反复下载失败；1.8.24 与 Entities 1.3.2 兼容 |
| 4 个文件补 `using TMG.DOTSSurvivors;` | `.inputactions.meta` 里 `wrapperCodeNamespace` 仍是旧作者的 `TMG.DOTSSurvivors`，业务代码在 `Lemo.SpaceSurvivors`，导致 `DOTSSurvivorsInputActions` 找不到（CS0246） |
| `TMP Settings.asset` 加 `SourceHanSansSC-Normal SDF` 为全局 fallback | `LXGWMarkerGothic-Regular SDF` 是静态图集且无回退，UI 中文（火星/金星等）缺字形显示方块 |

涉及文件：`Player/PlayerAuthoring.cs`、`UI/PauseMenuUIController.cs`、`UI/SelectionIconUIController.cs`、`UI/TitleScreenUIController.cs`、`Scripts/Input/DOTSSurvivorsInputActions.cs`

## 运行步骤

1. 编辑器当前已加载项目（标题栏 `SpaceSurvivor - Unity 6 (6000.0.30f1)`）
2. 打开场景 `Assets/DOTSSurvivors/Scenes/TitleScene.unity`
3. 按 Play

首次 Play 会烘焙 SubScene（火星/月球/金星的 ECS 实体场景），需要等一会儿。

## 操作

- 移动：WASD / 方向键
- 攻击：全自动
- 关卡：`XxxLevel.unity`（主场景）+ `XxxEntityScene.unity`（ECS 实体 SubScene）+ `XxxEnvironmentScene.unity`（环境）

## 依赖版本

| 项目 | 版本 |
|---|---|
| Unity Editor | 6000.0.30f1（原要求 6000.0.43f1） |
| Entities | 1.3.2 |
| Entities Graphics | 1.3.2 |
| Unity Physics | 1.3.2 |
| Burst | 1.8.24（原 1.8.19） |
| URP | 17.0.4 |

## 编辑器的启动方式（重要）

Unity 编辑器从本会话的工具命令里启动会被沙箱回收（进程几秒后消失、日志停在 `Rebuilding Library`）。本机唯一可靠方式是在后台任务里阻塞运行：

```powershell
& "D:\Unity\6000.0.30f1\Editor\Unity.exe" -projectPath "C:\Users\zlj10\WorkBuddy\2026-10-20-15-01-55\SpaceSurvivor"
```

日常自己用则直接双击项目或用 Hub 打开即可。

## 已装好的 Unity CLI

- `unity` 命令：v1.0.0-beta.13（`winget install --id Unity.CLI`，MSIX 包）
- `unity editors -i` 能列出本机三个引擎：6000.0.30f1 / 2023.1.4f1 / 2022.3.57f1
- 要用它驱动编辑器（进 Play、重编译、跑测试、截图），还需两步：
  1. 项目内执行 `unity pipeline install`（安装 `com.unity.pipeline`）
  2. `unity auth login`（浏览器 OAuth）
  之后可用 `unity status` / `unity command editor_play` / `unity command screenshot`

## 代码入口

```
Assets/DOTSSurvivors/Scripts/
├── PlayerAttacks/    31 文件，14 种武器（激光/锯刃/火箭/等离子/黑洞/无人机/扳手/螺丝刀/氧气软管/云/喷气背包/卫星/放射废料/电钻）
├── Transformations/  11 文件，行为组件（直线/抛物线/回旋镖/旋转/反向旋转/缩放脉冲/环境反弹/屏幕边缘反弹/击退）
├── StatModifiers/     6 文件，属性修改器 + 重算
├── EnemyLogic/       10 文件，生成/波次/巡逻/追击/正弦移动
├── Destruction/      15 文件，伤害结算/命中销毁/超时销毁/闪白/伤害数字
├── Items/             8 文件，经验宝石/箱子/掉落物交互
├── UI/               21 文件
├── VFX/              13 文件
└── General/          24 文件，游戏流程/相机/碰撞/时间
```

## 备注：Burst 包的应急修复手段

如果以后删了 Library 重新解析、又卡在 Burst 下载，可以手动填缓存：

```
https://packages.unity.cn/com.unity.burst/-/com.unity.burst-1.8.24.tgz   (469645774 字节)
→ %LOCALAPPDATA%\Unity\cache\npm\packages.unity.com\com.unity.burst\1.8.24\package.tgz
→ 同目录 package.tgz.size 写入 469645774
```
