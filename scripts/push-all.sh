#!/usr/bin/env bash
# 双推脚本：先推 Gitee，再推 GitHub。用法：bash scripts/push-all.sh [分支名]
# 凭据从仓库根目录的 .env 读取（该文件已被 .gitignore 忽略，不会入库）。
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT" || exit 1

if [ -f .env ]; then
    set -a
    # shellcheck disable=SC1091
    . ./.env
    set +a
fi

BRANCH="${1:-$(git rev-parse --abbrev-ref HEAD)}"
GITEE_USER="${GITEE_USER:-}"
GITEE_TOKEN="${GITEE_TOKEN:-}"
GITEE_REMOTE="${GITEE_REMOTE:-gitee}"
GITHUB_REMOTE="${GITHUB_REMOTE:-origin}"

echo "==> 仓库：$REPO_ROOT"
echo "==> 分支：$BRANCH"
echo

FAILED=0

# ---- 1) Gitee ----
echo "==> [1/2] 推送 Gitee（$GITEE_REMOTE）"
if [ -z "$GITEE_USER" ] || [ -z "$GITEE_TOKEN" ]; then
    echo "    ✗ 缺少 GITEE_USER / GITEE_TOKEN，请在仓库根目录的 .env 里配置（参考 .env.example）"
    FAILED=1
else
    # 令牌通过临时 credential helper 注入：不写进 remote URL、不落盘、不出现在进程参数里
    if GITEE_USER="$GITEE_USER" GITEE_TOKEN="$GITEE_TOKEN" \
        git -c credential.helper= \
            -c credential.helper='!f() { echo "username=${GITEE_USER}"; echo "password=${GITEE_TOKEN}"; }; f' \
            push "$GITEE_REMOTE" "$BRANCH"; then
        echo "    ✓ Gitee 已完成"
    else
        echo "    ✗ Gitee 推送失败"
        FAILED=1
    fi
fi

echo

# ---- 2) GitHub ----
echo "==> [2/2] 推送 GitHub（$GITHUB_REMOTE）"
if git push "$GITHUB_REMOTE" "$BRANCH"; then
    echo "    ✓ GitHub 已完成"
else
    echo "    ✗ GitHub 推送失败"
    FAILED=1
fi

echo
if [ "$FAILED" -ne 0 ]; then
    echo "==> 有推送失败，请看上面带 ✗ 的行"
    exit 1
fi
echo "==> 两边都推送成功"
