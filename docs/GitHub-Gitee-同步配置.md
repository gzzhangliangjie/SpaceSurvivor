# SpaceSurvivor GitHub ⇄ Gitee 双推配置

- 主仓库：`https://github.com/gzzhangliangjie/SpaceSurvivor`（默认分支 `main`）
- 镜像仓库：`https://gitee.com/gzzhangliangjie/SpaceSurvivor`
- 当前同步方式：**本地双推脚本 `scripts/push-all.sh`，先 Gitee 再 GitHub**
- GitHub Actions 自动同步方案已弃用（原因见文末）

## 一次性准备：Gitee 私人令牌

1. 打开 https://gitee.com/profile/personal_access_tokens
2. 「生成新令牌」，勾选 `projects` 权限即可
3. 令牌只显示一次，复制保存

## 本地凭据：`.env`

```bash
cp .env.example .env      # 填入真实的用户名和令牌
```

```ini
GITEE_USER=gzzhangliangjie
GITEE_TOKEN=你的私人令牌
```

- `.env` 已加入 `.gitignore`，**不会入库**；可用 `git check-ignore -v .env` 验证
- 换机器时手动拷一次 `.env`，或重新建一份
- 令牌明文只存在本机，脚本运行时通过临时 credential helper 注入，不写进 remote URL、不落盘、不进 git 配置

## 日常用法

```bash
bash scripts/push-all.sh          # 推当前分支，先 Gitee 后 GitHub
bash scripts/push-all.sh main     # 指定分支
```

脚本行为：

1. 从仓库根目录 `.env` 加载 `GITEE_USER` / `GITEE_TOKEN`（也支持用同名环境变量覆盖）
2. **先推 `gitee`，再推 `origin`（GitHub）**
3. 任一边失败都会继续尝试另一边，最后以非 0 退出——看到红字先确认是哪一边，另一边未必失败
4. 缺少 `.env` 或变量为空时，跳过 Gitee 并在结果里标 `✗`，不会静默成功

## 关键约束

- **Gitee 是覆盖式镜像，不是对等副本。** 只在 Gitee 上存在的提交，会在下次镜像推送（`--mirror`）时被重置掉。所以不要在 Gitee 网页上直接改文件或提交。
- **本仓库不能是浅克隆。** Gitee 服务端拒绝浅克隆的推送：`! [remote rejected] main -> main (shallow update not allowed)`。若 `ls .git/shallow` 存在，先 `git fetch --unshallow origin` 再推。
- 仓库单文件最大 41.8 MB（`Assets/DOTSSurvivors/Art/Audio/Music`），Gitee 免费版限制是单文件 100 MB / 仓库 500 MB；当前整仓约 154 MB，还有余量，但再塞大音频要留意。
- Issue / PR / Star / Release 不在 git 同步范围内。

## 踩坑记录

| 现象 | 原因 | 处理 |
|---|---|---|
| Actions 同步 57 分钟无进展、Gitee ref 为 0 | GitHub runner → Gitee 跨境 HTTPS 连接挂起（git 无应用层超时，Actions job 超时 6h） | 弃用 Actions，改本地推送 |
| `remote rejected ... (shallow update not allowed)` | 本地是浅克隆（`--depth 1`） | `git fetch --unshallow origin`（本仓库只需补 1 个初始提交） |
| 双 push 时 GitHub 被推两次 | 用了两次 `set-url --add --push`，追加而非替换 | 第一条用 `set-url --push` 重写 |

## 为什么弃用了 GitHub Actions 方案

原方案是 `.github/workflows/sync-to-gitee.yml`：每次 push 时 GitHub runner 裸克隆整个仓库，再 `git push --mirror` 到 Gitee。

实测结果：首次同步跑到 **57 分钟仍无任何进展**，Gitee 侧 ref 数为 0，同一时刻本机 `git ls-remote` 却是秒回（说明 Gitee 服务正常）。判断为跨境连接挂起。核心矛盾是首次要跨境传 154 MB 全量包（音乐、字体、PDF 占大头），改用本地推送后走国内链路，几分钟完成。

若以后又想恢复自动化（比如会在 GitHub 网页改文件、合并 PR），务必同时做两件事：

1. 用本地推送先把 Gitee 的基座灌满，避免首次全量传输；
2. 给 job 加 `timeout-minutes: 20`，让挂起变成可见的失败，而不是干等 6 小时。
