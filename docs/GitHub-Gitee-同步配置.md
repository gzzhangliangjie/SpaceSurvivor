# SpaceSurvivor GitHub → Gitee 自动同步配置

仓库：`https://github.com/gzzhangliangjie/SpaceSurvivor`（默认分支 `main`）
方案：GitHub Actions 单向同步（GitHub 为唯一源头）+ 本地双 push 兜底。

## 第一步：在 Gitee 建空仓库（手动，1 分钟）

1. 打开 https://gitee.com/projects/new
2. 仓库名填 `SpaceSurvivor`（和 GitHub 保持一致，workflow 里会用到同名仓库）
3. **不要勾选**「使用 Readme 初始化」——必须是空仓库
4. 开源/私有按需选择，创建即可

## 第二步：生成 Gitee 私人令牌

1. 打开 https://gitee.com/profile/personal_access_tokens
2. 「生成新令牌」，勾选 `projects` 权限即可
3. **令牌只显示一次**，复制保存好

## 第三步：给 GitHub 仓库配置 Secrets

打开 `https://github.com/gzzhangliangjie/SpaceSurvivor/settings/secrets/actions`，新建两个 secret：

| Name | 值 |
|---|---|
| `GITEE_USER` | 你的 Gitee 用户名（登录名） |
| `GITEE_TOKEN` | 第二步生成的私人令牌 |

## 第四步：提交 workflow 并推送

workflow 文件已放在仓库的 `.github/workflows/sync-to-gitee.yml`，本文档位于 `docs/GitHub-Gitee-同步配置.md`。提交推送即可：

```bash
git add .github/workflows/sync-to-gitee.yml docs/GitHub-Gitee-同步配置.md
git commit -m "ci: add sync-to-gitee workflow"
git push
```

推上去之后，这次 push 本身就会触发第一次同步。也可以去 Actions 页面点 `Sync to Gitee` → `Run workflow` 手动跑一次验证。

## 第五步：配置本地双 push 兜底

在你的 SpaceSurvivor 本地仓库里执行（原 GitHub 推送地址是什么就用什么）：

```bash
# 先看当前推送地址
git remote -v

# 追加两个 push 地址（fetch 地址保持不变）
git remote set-url --add --push origin https://github.com/gzzhangliangjie/SpaceSurvivor.git
git remote set-url --add --push origin https://gitee.com/<Gitee用户名>/SpaceSurvivor.git

# 验证：origin 的 push 地址应显示两行
git remote -v
```

之后 `git push` 会同时推两边。Gitee 的 HTTPS 推送需要账号密码或令牌认证，配一次凭据管理器即可。

注意：本机直连 github.com 曾出现证书问题；如果本地推 GitHub 失败，直接推 Gitee 那条不受影响，GitHub 侧下次用能通的渠道推即可，Actions 会把 Gitee 拉齐。

## 行为说明

- 每次对 GitHub 的 push（任意分支、任意 tag）都会触发一次全量镜像同步，几秒到几分钟完成
- 删除 GitHub 上的分支/tag，Gitee 上也会同步删除（每次全新裸克隆 + `--mirror`）
- Gitee 是只读镜像：**不要在 Gitee 上直接提交**，否则会被下次同步覆盖
- Issue / PR / Star / Release 不在同步范围内（git 层面同步不了这些）

## 常见问题

| 现象 | 处理 |
|---|---|
| Actions 报 403 推 Gitee | 检查 `GITEE_TOKEN` 是否勾了 `projects` 权限、用户名是否为登录名 |
| 同步后 Gitee 多出分支 | 正常，之前手动推过的残留，mirror 会清理 |
| 仓库超过 500MB / 单文件超 100MB | Gitee 免费版会拒绝，需先清理或用 LFS（Gitee LFS 免费容量很小） |
| Actions 排队没跑 | 手动 `Run workflow` 兜底；本地双 push 本来就不依赖 Actions |
