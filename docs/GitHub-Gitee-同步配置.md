# SpaceSurvivor GitHub → Gitee 同步说明

- 源仓库：`https://github.com/gzzhangliangjie/SpaceSurvivor`（默认分支 `main`，唯一源头）
- 镜像仓库：`https://gitee.com/gzzhangliangjie/SpaceSurvivor`（只读镜像）
- 当前同步方式：**本地 `git push` 到 Gitee**（GitHub Actions 方案已弃用，原因见文末）

## 一次性准备：Gitee 私人令牌

1. 打开 https://gitee.com/profile/personal_access_tokens
2. 「生成新令牌」，勾选 `projects` 权限即可
3. 令牌只显示一次，复制保存

首次推送会提示输入 Gitee 账号和令牌，Windows 凭据管理器记住后会一直复用。

## 日常用法

### 方式 A：独立 remote（推荐，最不容易出错）

本地仓库已配置好独立的 `gitee` remote，`origin` 保持只推 GitHub：

```bash
# 全量镜像：分支、tag 全部与 GitHub 对齐，Gitee 上多余引用会被删除
git push gitee --mirror

# 只推当前分支（日常够用，不会动 Gitee 上的其他分支）
git push gitee main
```

首次必须用 `--mirror` 把完整历史灌上去（约 154 MB），之后都是增量，几秒到几十秒。

### 方式 B：origin 双 push（一条命令推两边）

注意 `set-url --add --push` 是「追加」不是「替换」。本仓库 `origin` 已有一条 push 地址，所以第一条要用不带 `--add` 的 `set-url --push` 重写，否则 GitHub 会被推两遍：

```bash
git remote set-url --push origin https://gzzhangliangjie@github.com/gzzhangliangjie/SpaceSurvivor.git
git remote set-url --add --push origin https://gzzhangliangjie@gitee.com/gzzhangliangjie/SpaceSurvivor.git
git remote -v   # 应恰好两行 push：GitHub + Gitee
```

副作用：

- 双 push 是**串行**的，Gitee 那条失败会让 `git push` 整体返回非 0，但 GitHub 那条其实已经成功——别看到红字就以为两边都没上去。
- 以后每条命令都会同时推两边，想临时只推 GitHub 得用 `git push <github-url> main`。

## 关键约束

- **Gitee 是覆盖式镜像，不是对等副本。** 只在 Gitee 上存在的提交，会在下一次镜像推送时被 GitHub 的状态强行覆盖（`--mirror` 会重置 ref）。所以不要在 Gitee 网页上直接改文件或提交。
- 仓库里最大单文件 41.8 MB（`Assets/DOTSSurvivors/Art/Audio/Music`），Gitee 免费版限制是单文件 100 MB / 仓库 500 MB，目前都还有余量，但再往里塞大音频要留意。
- Issue / PR / Star / Release 不在 git 同步范围内。

## 为什么弃用了 GitHub Actions 方案

原方案是 `.github/workflows/sync-to-gitee.yml`：每次 push 时 GitHub runner 裸克隆整个仓库，再 `git push --mirror` 到 Gitee。

实测结果：首次同步跑到 **57 分钟仍无任何进展**，Gitee 侧 ref 数为 0，同一时刻本机 `git ls-remote` 却是秒回（说明 Gitee 服务正常）。判断为 GitHub runner → Gitee 的跨境 HTTPS 连接被挂起：git 不设应用层超时，连接挂住后既不报错也不结束，而 Actions 的 job 总超时是 6 小时。

核心矛盾是**首次要跨境传 154 MB 全量包**（音乐、字体、PDF 占大头）。改用本地推送后，这一步走的是国内到 Gitee 的链路，几分钟完成。

如果以后又想要自动化（比如会在 GitHub 网页上改文件、合并 PR），可以恢复 workflow，但务必同时做两件事：

1. 用本地推送先把 Gitee 的基座灌满，避免首次全量传输；
2. 给 job 加 `timeout-minutes: 20`，让挂起变成可见的失败，而不是干等 6 小时。
